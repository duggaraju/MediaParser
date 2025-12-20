using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Media.ISO.SourceGenerators;

public sealed partial class BoxContentGenerator
{
    private abstract class PropertyAccessor
    {
        public abstract void AppendRead(StringBuilder builder, string indent, string propertyName);

        public abstract void AppendWrite(StringBuilder builder, string indent, string propertyName);

        public abstract string GetSizeExpression(string propertyName);

        public virtual bool RequiresRemainingBytes => false;

        public virtual CollectionLengthStrategy LengthStrategy => CollectionLengthStrategy.None;

        public virtual string? GetCollectionCountExpression(string propertyName) => null;
    }

    private sealed class SimplePropertyAccessor : PropertyAccessor
    {
        private readonly string _readerInvocation;
        private readonly string _writerInvocationFormat;
        private readonly string _sizeExpressionFormat;

        public SimplePropertyAccessor(string readerInvocation, string writerInvocationFormat, string sizeExpressionFormat)
        {
            _readerInvocation = readerInvocation;
            _writerInvocationFormat = writerInvocationFormat;
            _sizeExpressionFormat = sizeExpressionFormat;
        }

        public override void AppendRead(StringBuilder builder, string indent, string propertyName)
        {
            builder.Append(indent)
                .Append(propertyName)
                .Append(" = reader.")
                .Append(_readerInvocation)
                .AppendLine(";");
        }

        public override void AppendWrite(StringBuilder builder, string indent, string propertyName)
        {
            builder.Append(indent)
                .AppendFormat(CultureInfo.InvariantCulture, _writerInvocationFormat, propertyName)
                .AppendLine();
        }

        public override string GetSizeExpression(string propertyName) => string.Format(CultureInfo.InvariantCulture, _sizeExpressionFormat, propertyName);
    }

    private sealed class StringPropertyAccessor : PropertyAccessor
    {
        public void AppendRead(StringBuilder builder, string indent, string propertyName, bool useRemainingLength)
        {
            builder.Append(indent)
                .Append(propertyName)
                .Append(" = reader.ReadString(");
            if (useRemainingLength)
            {
                builder.Append("__remaining");
            }

            builder.Append(");")
                .AppendLine();
        }

        public override void AppendRead(StringBuilder builder, string indent, string propertyName)
        {
            AppendRead(builder, indent, propertyName, useRemainingLength: false);
        }

        public override void AppendWrite(StringBuilder builder, string indent, string propertyName)
        {
            builder.Append(indent)
                .Append("writer.WriteString(")
                .Append(propertyName)
                .Append(" ?? global::System.String.Empty);")
                .AppendLine();
        }

        public override string GetSizeExpression(string propertyName) => $"(({propertyName}?.Length ?? 0) + 1)";
    }

    private sealed class VersionSizedPropertyAccessor : PropertyAccessor
    {
        private readonly bool _isSigned;
        private readonly int _versionThreshold;

        public VersionSizedPropertyAccessor(bool isSigned, int versionThreshold)
        {
            _isSigned = isSigned;
            _versionThreshold = versionThreshold;
        }

        public override void AppendRead(StringBuilder builder, string indent, string propertyName)
        {
            builder.Append(indent)
                .Append("if (Version >= ")
                .Append(_versionThreshold)
                .AppendLine(")");
            builder.Append(indent).AppendLine("{");
            builder.Append(indent)
                .Append("    ")
                .Append(propertyName)
                .Append(" = reader.")
                .Append(_isSigned ? "ReadInt64()" : "ReadUInt64()")
                .AppendLine(";");
            builder.Append(indent).AppendLine("}");
            builder.Append(indent).AppendLine("else");
            builder.Append(indent).AppendLine("{");
            builder.Append(indent)
                .Append("    ")
                .Append(propertyName)
                .Append(" = reader.")
                .Append(_isSigned ? "ReadInt32()" : "ReadUInt32()")
                .AppendLine(";");
            builder.Append(indent).AppendLine("}");
        }

        public override void AppendWrite(StringBuilder builder, string indent, string propertyName)
        {
            builder.Append(indent)
                .Append("if (Version >= ")
                .Append(_versionThreshold)
                .AppendLine(")");
            builder.Append(indent).AppendLine("{");
            builder.Append(indent)
                .Append("    writer.")
                .Append(_isSigned ? "WriteInt64" : "WriteUInt64")
                .Append("(")
                .Append(propertyName)
                .AppendLine(");");
            builder.Append(indent).AppendLine("}");
            builder.Append(indent).AppendLine("else");
            builder.Append(indent).AppendLine("{");
            builder.Append(indent)
                .Append("    writer.")
                .Append(_isSigned ? "WriteInt32" : "WriteUInt32")
                .Append("((")
                .Append(_isSigned ? "int" : "uint")
                .Append(")")
                .Append(propertyName)
                .AppendLine(");");
            builder.Append(indent).AppendLine("}");
        }

        public override string GetSizeExpression(string propertyName) => string.Format(CultureInfo.InvariantCulture,
            "(Version >= {0} ? sizeof({1}) : sizeof({2}))",
            _versionThreshold,
            _isSigned ? "long" : "ulong",
            _isSigned ? "int" : "uint");
    }

    private sealed class ByteArrayPropertyAccessor : PropertyAccessor
    {
        public override bool RequiresRemainingBytes => true;

        public override void AppendRead(StringBuilder builder, string indent, string propertyName)
        {
            builder.Append(indent).AppendLine("if (__remaining <= 0)");
            builder.Append(indent).AppendLine("{");
            builder.Append(indent)
                .Append("    ")
                .Append(propertyName)
                .Append(" = global::System.Array.Empty<byte>();")
                .AppendLine();
            builder.Append(indent).AppendLine("}");
            builder.Append(indent).AppendLine("else");
            builder.Append(indent).AppendLine("{");
            builder.Append(indent)
                .Append("    ")
                .Append(propertyName)
                .Append(" = new byte[__remaining];")
                .AppendLine();
            builder.Append(indent)
                .Append("    reader.Read(")
                .Append(propertyName)
                .AppendLine(");");
            builder.Append(indent).AppendLine("}");
        }

        public override void AppendWrite(StringBuilder builder, string indent, string propertyName)
        {
            builder.Append(indent)
                .Append("writer.Write(")
                .Append(propertyName)
                .Append(" ?? global::System.Array.Empty<byte>());")
                .AppendLine();
        }

        public override string GetSizeExpression(string propertyName) => GetArrayCountExpression(propertyName);
    }

    private sealed class FlagOptionalPropertyAccessor : PropertyAccessor
    {
        private readonly PropertyAccessor _inner;
        private readonly string _maskLiteral;

        public FlagOptionalPropertyAccessor(PropertyAccessor inner, uint flagMask)
        {
            _inner = inner;
            _maskLiteral = string.Format(CultureInfo.InvariantCulture, "0x{0:X}u", flagMask);
        }

        public override void AppendRead(StringBuilder builder, string indent, string propertyName)
        {
            builder.Append(indent)
                .Append("if ((Flags & ")
                .Append(_maskLiteral)
                .AppendLine(") != 0)");
            builder.Append(indent).AppendLine("{");
            _inner.AppendRead(builder, indent + "    ", propertyName);
            builder.Append(indent).AppendLine("}");
        }

        public override void AppendWrite(StringBuilder builder, string indent, string propertyName)
        {
            builder.Append(indent)
                .Append("if ((Flags & ")
                .Append(_maskLiteral)
                .AppendLine(") != 0)");
            builder.Append(indent).AppendLine("{");
            _inner.AppendWrite(builder, indent + "    ", propertyName);
            builder.Append(indent).AppendLine("}");
        }

        public override string GetSizeExpression(string propertyName) =>
            $"((Flags & {_maskLiteral}) != 0 ? ({_inner.GetSizeExpression(propertyName)}) : 0)";

        public override bool RequiresRemainingBytes => _inner.RequiresRemainingBytes;

        public override CollectionLengthStrategy LengthStrategy => _inner.LengthStrategy;
    }

    private sealed class ReservedPropertyAccessor : PropertyAccessor
    {
        private readonly string _byteCountLiteral;

        public ReservedPropertyAccessor(int byteCount)
        {
            if (byteCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            }

            _byteCountLiteral = byteCount.ToString(CultureInfo.InvariantCulture);
        }

        public override void AppendRead(StringBuilder builder, string indent, string propertyName)
        {
            builder.Append(indent)
                .Append("reader.SkipBytes(")
                .Append(_byteCountLiteral)
                .AppendLine(");");
        }

        public override void AppendWrite(StringBuilder builder, string indent, string propertyName)
        {
            builder.Append(indent)
                .Append("writer.SkipBytes(")
                .Append(_byteCountLiteral)
                .AppendLine(");");
        }

        public override string GetSizeExpression(string propertyName) => _byteCountLiteral;
    }

    private sealed class PrimitiveArrayPropertyAccessor : PropertyAccessor
    {
        private readonly string _readerMethod;
        private readonly string _writerMethod;
        private readonly string _sizeOfExpression;
        private readonly string _elementTypeName;
        private readonly CollectionLengthStrategy _lengthStrategy;

        public PrimitiveArrayPropertyAccessor(string readerMethod, string writerMethod, string sizeOfExpression, string elementTypeName, CollectionLengthStrategy lengthStrategy)
        {
            _readerMethod = readerMethod;
            _writerMethod = writerMethod;
            _sizeOfExpression = sizeOfExpression;
            _elementTypeName = elementTypeName;
            _lengthStrategy = lengthStrategy;
        }

        public override bool RequiresRemainingBytes => _lengthStrategy.Kind == CollectionLengthKind.RemainingBytes;

        public override void AppendRead(StringBuilder builder, string indent, string propertyName)
        {
            string lengthExpression;
            if (_lengthStrategy.Kind == CollectionLengthKind.RemainingBytes)
            {
                builder.Append(indent)
                    .Append("var __count = global::System.Math.Max(0, __remaining / ")
                    .Append(_sizeOfExpression)
                    .AppendLine(");");
                builder.Append(indent)
                    .Append(propertyName)
                    .Append(" = new ")
                    .Append(_elementTypeName)
                    .Append("[__count];")
                    .AppendLine();
                lengthExpression = "__count";
            }
            else
            {
                lengthExpression = InitializeArrayForRead(builder, indent, propertyName, _elementTypeName, _lengthStrategy);
            }

            builder.Append(indent)
                .Append("for (var __i = 0; __i < ")
                .Append(lengthExpression)
                .AppendLine("; __i++)");
            builder.Append(indent).AppendLine("{");
            builder.Append(indent)
                .Append("    ")
                .Append(propertyName)
                .Append("[__i] = reader.")
                .Append(_readerMethod)
                .AppendLine(";");
            builder.Append(indent).AppendLine("}");
        }

        public override void AppendWrite(StringBuilder builder, string indent, string propertyName)
        {
            var lengthExpression = PrepareArrayLengthForWrite(builder, indent, propertyName, _lengthStrategy);

            builder.Append(indent)
                .Append("for (var __i = 0; __i < ")
                .Append(lengthExpression)
                .AppendLine("; __i++)");
            builder.Append(indent).AppendLine("{");
            builder.Append(indent)
                .Append("    writer.")
                .Append(_writerMethod)
                .Append("(")
                .Append(propertyName)
                .Append("[__i]);")
                .AppendLine();
            builder.Append(indent).AppendLine("}");
        }

        public override string GetSizeExpression(string propertyName)
        {
            var baseSize = $"({GetArrayCountExpression(propertyName)} * {_sizeOfExpression})";
            if (_lengthStrategy.Kind == CollectionLengthKind.LengthPrefixed)
            {
                return $"{_lengthStrategy.PrefixSizeInBytes} + {baseSize}";
            }

            return baseSize;
        }

        public override CollectionLengthStrategy LengthStrategy => _lengthStrategy;

        public override string? GetCollectionCountExpression(string propertyName) => GetArrayCountExpression(propertyName);
    }

    private sealed class StructArrayPropertyAccessor : PropertyAccessor
    {
        private readonly string _elementTypeName;
        private readonly CollectionLengthStrategy _lengthStrategy;
        private readonly bool _requiresVersionContext;

        public StructArrayPropertyAccessor(string elementTypeName, CollectionLengthStrategy lengthStrategy, bool requiresVersionContext)
        {
            _elementTypeName = elementTypeName;
            _lengthStrategy = lengthStrategy;
            _requiresVersionContext = requiresVersionContext;
        }

        public override void AppendRead(StringBuilder builder, string indent, string propertyName)
        {
            var lengthExpression = InitializeArrayForRead(builder, indent, propertyName, _elementTypeName, _lengthStrategy);

            builder.Append(indent)
                .Append("for (var __i = 0; __i < ")
                .Append(lengthExpression)
                .AppendLine("; __i++)");
            builder.Append(indent).AppendLine("{");
            builder.Append(indent)
                .Append("    ")
                .Append(propertyName)
                .Append("[__i] = new ")
                .Append(_elementTypeName)
                .AppendLine("();");
            builder.Append(indent)
                .Append("    ")
                .Append(propertyName)
                .Append("[__i].Read(reader");
            if (_requiresVersionContext)
            {
                builder.Append(", ").Append(GetVersionAndFlagsExpression());
            }
            builder.AppendLine(");");
            builder.Append(indent).AppendLine("}");
        }

        public override void AppendWrite(StringBuilder builder, string indent, string propertyName)
        {
            var lengthExpression = PrepareArrayLengthForWrite(builder, indent, propertyName, _lengthStrategy);

            builder.Append(indent)
                .Append("for (var __i = 0; __i < ")
                .Append(lengthExpression)
                .AppendLine("; __i++)");
            builder.Append(indent).AppendLine("{");
            builder.Append(indent)
                .Append("    ")
                .Append(propertyName)
                .Append("[__i].Write(writer");
            if (_requiresVersionContext)
            {
                builder.Append(", ").Append(GetVersionAndFlagsExpression());
            }
            builder.AppendLine(");");
            builder.Append(indent).AppendLine("}");
        }

        public override string GetSizeExpression(string propertyName)
        {
            var collectionExpression = $"{propertyName} ?? global::System.Array.Empty<{_elementTypeName}>()";
            var contextExpression = GetVersionAndFlagsExpression();
            var computeSizeInvocation = _requiresVersionContext
                ? $"__x.ComputeSize({contextExpression})"
                : "__x.ComputeSize()";
            var baseExpression = $"global::System.Linq.Enumerable.Sum({collectionExpression}, __x => {computeSizeInvocation})";
            if (_lengthStrategy.Kind == CollectionLengthKind.LengthPrefixed)
            {
                return $"{_lengthStrategy.PrefixSizeInBytes} + {baseExpression}";
            }

            return baseExpression;
        }

        public override CollectionLengthStrategy LengthStrategy => _lengthStrategy;

        public override string? GetCollectionCountExpression(string propertyName) => GetArrayCountExpression(propertyName);
    }

    private sealed class ListPropertyAccessor : PropertyAccessor
    {
        private readonly PropertyAccessor? _primitiveAccessor;
        private readonly bool _isStructOrClass;
        private readonly string _elementTypeName;
        private readonly CollectionLengthStrategy _lengthStrategy;
        private readonly bool _requiresVersionContext;
        private readonly string? _primitiveElementSizeExpression;

        public ListPropertyAccessor(PropertyAccessor? primitiveAccessor, bool isStructOrClass, string elementTypeName, CollectionLengthStrategy lengthStrategy, bool requiresVersionContext = false, string? primitiveElementSizeExpression = null)
        {
            _primitiveAccessor = primitiveAccessor;
            _isStructOrClass = isStructOrClass;
            _elementTypeName = elementTypeName;
            _lengthStrategy = lengthStrategy;
            _requiresVersionContext = requiresVersionContext && isStructOrClass;
            _primitiveElementSizeExpression = primitiveElementSizeExpression;
        }

        public override bool RequiresRemainingBytes => _lengthStrategy.Kind == CollectionLengthKind.RemainingBytes || (_primitiveAccessor?.RequiresRemainingBytes ?? false);

        public override void AppendRead(StringBuilder builder, string indent, string propertyName)
        {
            if (_lengthStrategy.Kind == CollectionLengthKind.RemainingBytes)
            {
                builder.Append(indent)
                    .Append("var __count = global::System.Math.Max(0, __remaining / ")
                    .Append(_primitiveElementSizeExpression ?? "1")
                    .AppendLine(");");
                builder.Append(indent)
                    .Append(propertyName)
                    .Append(" = new global::System.Collections.Generic.List<")
                    .Append(_elementTypeName)
                    .Append(">(__count);")
                    .AppendLine();
                builder.Append(indent)
                    .Append("for (var __i = 0; __i < __count; __i++)")
                    .AppendLine();
                builder.Append(indent).AppendLine("{");
                if (_isStructOrClass)
                {
                    builder.Append(indent)
                        .Append("    var __item = new ")
                        .Append(_elementTypeName)
                        .AppendLine("();");
                    builder.Append(indent)
                        .Append("    __item.Read(reader");
                    if (_requiresVersionContext)
                    {
                        builder.Append(", ").Append(GetVersionAndFlagsExpression());
                    }
                    builder.AppendLine(");");
                    builder.Append(indent)
                        .Append("    ")
                        .Append(propertyName)
                        .AppendLine(".Add(__item);");
                }
                else if (_primitiveAccessor is not null)
                {
                    builder.Append(indent)
                        .Append("    ")
                        .Append(_elementTypeName)
                        .AppendLine(" __item = default!;");
                    _primitiveAccessor.AppendRead(builder, indent + "    ", "__item");
                    builder.Append(indent)
                        .Append("    ")
                        .Append(propertyName)
                        .AppendLine(".Add(__item);");
                }

                builder.Append(indent).AppendLine("}");
                return;
            }

            if (_lengthStrategy.Kind == CollectionLengthKind.None)
            {
                builder.Append(indent)
                    .Append("for (var __i = 0; __i < ")
                    .Append(propertyName)
                    .AppendLine(".Count; __i++)");
                builder.Append(indent).AppendLine("{");
                if (_isStructOrClass)
                {
                    builder.Append(indent)
                        .Append("    var __item = ")
                        .Append(propertyName)
                        .AppendLine("[__i];");
                    builder.Append(indent)
                        .Append("    __item.Read(reader");
                    if (_requiresVersionContext)
                    {
                        builder.Append(", ").Append(GetVersionAndFlagsExpression());
                    }
                    builder.AppendLine(");");
                    builder.Append(indent)
                        .Append("    ")
                        .Append(propertyName)
                        .AppendLine("[__i] = __item;");
                }
                else if (_primitiveAccessor is not null)
                {
                    _primitiveAccessor.AppendRead(builder, indent + "    ", $"{propertyName}[__i]");
                }

                builder.Append(indent).AppendLine("}");
                return;
            }

            if (_lengthStrategy.Kind == CollectionLengthKind.FromProperty)
            {
                AppendCountDeclarationFromProperty(builder, indent, _lengthStrategy.LengthPropertyName!);
            }
            else
            {
                AppendCountDeclarationFromPrefix(builder, indent, _lengthStrategy);
            }

            builder.Append(indent)
                .Append(propertyName)
                .Append(" = new global::System.Collections.Generic.List<")
                .Append(_elementTypeName)
                .Append(">(")
                .Append("__count");
            builder.AppendLine(");");
            builder.Append(indent)
                .Append("for (var __i = 0; __i < __count; __i++)")
                .AppendLine();
            builder.Append(indent).AppendLine("{");
            if (_isStructOrClass)
            {
                builder.Append(indent)
                    .Append("    var __item = new ")
                    .Append(_elementTypeName)
                    .AppendLine("();");
                builder.Append(indent)
                    .Append("    __item.Read(reader");
                if (_requiresVersionContext)
                {
                    builder.Append(", ").Append(GetVersionAndFlagsExpression());
                }
                builder.AppendLine(");");
                builder.Append(indent)
                    .Append("    ")
                    .Append(propertyName)
                    .AppendLine(".Add(__item);");
            }
            else if (_primitiveAccessor is not null)
            {
                builder.Append(indent)
                    .Append("    ")
                    .Append(_elementTypeName)
                    .AppendLine(" __item = default!;");
                _primitiveAccessor.AppendRead(builder, indent + "    ", "__item");
                builder.Append(indent)
                    .Append("    ")
                    .Append(propertyName)
                    .AppendLine(".Add(__item);");
            }

            builder.Append(indent).AppendLine("}");
        }

        public override void AppendWrite(StringBuilder builder, string indent, string propertyName)
        {
            if (_lengthStrategy.Kind == CollectionLengthKind.LengthPrefixed)
            {
                builder.Append(indent)
                    .Append("var __list = ")
                    .Append(propertyName)
                    .Append(" ?? new global::System.Collections.Generic.List<")
                    .Append(_elementTypeName)
                    .AppendLine(">(0);");
                builder.Append(indent)
                    .Append("var __count = __list.Count;")
                    .AppendLine();
                builder.Append(indent)
                    .Append(_lengthStrategy.GetWriterInvocation("writer", "__count"))
                    .AppendLine(";");
                builder.Append(indent)
                    .Append("for (var __i = 0; __i < __count; __i++)")
                    .AppendLine();
                builder.Append(indent).AppendLine("{");
                if (_isStructOrClass)
                {
                    builder.Append(indent)
                        .Append("    __list[__i].Write(writer");
                    if (_requiresVersionContext)
                    {
                        builder.Append(", ").Append(GetVersionAndFlagsExpression());
                    }
                    builder.AppendLine(");");
                }
                else if (_primitiveAccessor is not null)
                {
                    _primitiveAccessor.AppendWrite(builder, indent + "    ", "__list[__i]");
                }

                builder.Append(indent).AppendLine("}");
                return;
            }

            builder.Append(indent)
                .Append("foreach (var __item in ")
                .Append(propertyName)
                .Append(" ?? global::System.Linq.Enumerable.Empty<")
                .Append(_elementTypeName)
                .AppendLine(">())");
            builder.Append(indent).AppendLine("{");
            if (_isStructOrClass)
            {
                builder.Append(indent)
                    .Append("    __item.Write(writer");
                if (_requiresVersionContext)
                {
                    builder.Append(", ").Append(GetVersionAndFlagsExpression());
                }
                builder.AppendLine(");");
            }
            else if (_primitiveAccessor is not null)
            {
                _primitiveAccessor.AppendWrite(builder, indent + "    ", "__item");
            }

            builder.Append(indent).AppendLine("}");
        }

        public override string GetSizeExpression(string propertyName)
        {
            string baseExpression;
            if (_isStructOrClass)
            {
                var collectionExpression = $"{propertyName} ?? global::System.Linq.Enumerable.Empty<{_elementTypeName}>()";
                var contextExpression = GetVersionAndFlagsExpression();
                var computeSizeInvocation = _requiresVersionContext
                    ? $"__x.ComputeSize({contextExpression})"
                    : "__x.ComputeSize()";
                baseExpression = $"global::System.Linq.Enumerable.Sum({collectionExpression}, __x => {computeSizeInvocation})";
            }
            else if (_primitiveAccessor is not null)
            {
                baseExpression = $"({GetListCountExpression(propertyName)} * {_primitiveAccessor.GetSizeExpression(propertyName)})";
            }
            else
            {
                baseExpression = "0";
            }

            if (_lengthStrategy.Kind == CollectionLengthKind.LengthPrefixed)
            {
                return $"{_lengthStrategy.PrefixSizeInBytes} + {baseExpression}";
            }

            return baseExpression;
        }

        public override CollectionLengthStrategy LengthStrategy => _lengthStrategy;

        public override string? GetCollectionCountExpression(string propertyName) => GetListCountExpression(propertyName);
    }

    private static class PropertyAccessorFactory
    {
        public static bool TryCreate(IPropertySymbol property, bool inheritsFullBox, out PropertyAccessor accessor, out Diagnostic? diagnostic)
        {
            diagnostic = null;
            var propertyAttributes = property.GetAttributes();
            var flagAttribute = propertyAttributes.FirstOrDefault(attribute =>
                AttributeMatches(attribute, FlagOptionalAttributeMetadataName) ||
                AttributeMatches(attribute, FlagDependentAttributeMetadataName));
            uint? flagMask = null;

            if (flagAttribute is not null)
            {
                if (!inheritsFullBox)
                {
                    diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.FlagAttributeInvalidUsage,
                        property.Locations.FirstOrDefault(),
                        property.Name,
                        property.ContainingType?.Name ?? string.Empty);
                    accessor = null!;
                    return false;
                }

                if (!TryGetFlagMask(flagAttribute, out var mask))
                {
                    diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.FlagAttributeInvalidMask,
                        property.Locations.FirstOrDefault(),
                        property.Name,
                        property.ContainingType?.Name ?? string.Empty);
                    accessor = null!;
                    return false;
                }

                flagMask = mask;
            }

            PropertyAccessor ApplyFlag(PropertyAccessor createdAccessor)
            {
                if (flagMask.HasValue)
                {
                    createdAccessor = new FlagOptionalPropertyAccessor(createdAccessor, flagMask.Value);
                }

                return createdAccessor;
            }

            var reservedAttribute = propertyAttributes.FirstOrDefault(attribute => AttributeMatches(attribute, ReservedAttributeMetadataName));
            if (reservedAttribute is not null)
            {
                if (!IsSupportedReservedPropertyType(property.Type))
                {
                    diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.ReservedAttributeInvalidType,
                        property.Locations.FirstOrDefault(),
                        property.Name,
                        property.ContainingType?.Name ?? string.Empty,
                        property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                    accessor = null!;
                    return false;
                }

                if (!TryGetReservedByteCount(reservedAttribute, out var reservedBytes))
                {
                    diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.ReservedAttributeInvalidSize,
                        property.Locations.FirstOrDefault(),
                        property.Name,
                        property.ContainingType?.Name ?? string.Empty);
                    accessor = null!;
                    return false;
                }

                accessor = ApplyFlag(new ReservedPropertyAccessor(reservedBytes));
                return true;
            }

            var versionAttribute = propertyAttributes.FirstOrDefault(attribute => AttributeMatches(attribute, VersionDependentSizeAttributeMetadataName));
            if (versionAttribute is not null)
            {
                if (!inheritsFullBox)
                {
                    diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.VersionAttributeInvalidUsage,
                        property.Locations.FirstOrDefault(),
                        property.Name,
                        property.ContainingType?.Name ?? string.Empty);
                    accessor = null!;
                    return false;
                }

                var threshold = 1;
                if (versionAttribute.ConstructorArguments.Length > 0 && versionAttribute.ConstructorArguments[0].Value is int value)
                {
                    threshold = value;
                }

                switch (property.Type.SpecialType)
                {
                    case SpecialType.System_UInt64:
                        accessor = ApplyFlag(new VersionSizedPropertyAccessor(isSigned: false, versionThreshold: threshold));
                        return true;
                    case SpecialType.System_Int64:
                        accessor = ApplyFlag(new VersionSizedPropertyAccessor(isSigned: true, versionThreshold: threshold));
                        return true;
                    default:
                        diagnostic = Diagnostic.Create(
                            DiagnosticDescriptors.VersionAttributeInvalidType,
                            property.Locations.FirstOrDefault(),
                            property.Name,
                            property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        accessor = null!;
                        return false;
                }
            }

            if (property.Type is IArrayTypeSymbol arrayType)
            {
                var isByteArray = arrayType.ElementType.SpecialType == SpecialType.System_Byte;
                var lengthStrategy = GetCollectionLengthStrategy(property, !isByteArray, out diagnostic);
                if (diagnostic is not null)
                {
                    accessor = null!;
                    return false;
                }

                if (lengthStrategy.Kind == CollectionLengthKind.RemainingBytes && !IsFixedSizePrimitive(arrayType.ElementType))
                {
                    diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.CollectionLengthToEndUnsupportedType,
                        property.Locations.FirstOrDefault(),
                        property.Name,
                        property.ContainingType?.Name ?? string.Empty,
                        arrayType.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                    accessor = null!;
                    return false;
                }

                if (isByteArray)
                {
                    accessor = ApplyFlag(new ByteArrayPropertyAccessor());
                    return true;
                }

                if (TryGetPrimitiveArrayAccessor(arrayType.ElementType, lengthStrategy, out var primitiveArrayAccessor))
                {
                    accessor = ApplyFlag(primitiveArrayAccessor);
                    return true;
                }

                if (arrayType.ElementType.TypeKind == TypeKind.Struct || arrayType.ElementType.TypeKind == TypeKind.Class)
                {
                    var requiresVersionContext = TypeHasVersionDependentSizeAttribute(arrayType.ElementType);
                    if (requiresVersionContext && !inheritsFullBox)
                    {
                        diagnostic = Diagnostic.Create(
                            DiagnosticDescriptors.VersionAttributeInvalidUsage,
                            property.Locations.FirstOrDefault(),
                            property.Name,
                            property.ContainingType?.Name ?? string.Empty);
                        accessor = null!;
                        return false;
                    }

                    if (!HasComputeSizeMethod(arrayType.ElementType, requiresVersionContext))
                    {
                        diagnostic = Diagnostic.Create(
                            DiagnosticDescriptors.CollectionElementMissingComputeSize,
                            property.Locations.FirstOrDefault(),
                            property.Name,
                            arrayType.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            property.ContainingType?.Name ?? string.Empty);
                        accessor = null!;
                        return false;
                    }

                    var elementTypeName = arrayType.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    accessor = ApplyFlag(new StructArrayPropertyAccessor(elementTypeName, lengthStrategy, requiresVersionContext));
                    return true;
                }

                accessor = null!;
                diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.PropertyTypeUnsupported,
                    property.Locations.FirstOrDefault(),
                    property.Name,
                    property.ContainingType?.Name ?? string.Empty,
                    property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                return false;
            }

            if (property.Type is INamedTypeSymbol namedType)
            {
                var isList = false;
                ITypeSymbol? elementType = null;

                if (namedType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IList_T ||
                    namedType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Collections.Generic.List<T>")
                {
                    isList = true;
                    elementType = namedType.TypeArguments.FirstOrDefault();
                }

                if (isList && elementType is not null)
                {
                    var lengthStrategy = GetCollectionLengthStrategy(property, supportsStrategy: true, out diagnostic);
                    if (diagnostic is not null)
                    {
                        accessor = null!;
                        return false;
                    }

                    if (TryCreatePrimitiveElementAccessor(elementType, out var primitiveListAccessor, out var elementSizeExpression))
                    {
                        var elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        accessor = ApplyFlag(new ListPropertyAccessor(primitiveListAccessor, isStructOrClass: false, elementTypeName, lengthStrategy, primitiveElementSizeExpression: elementSizeExpression));
                        return true;
                    }

                    if (lengthStrategy.Kind == CollectionLengthKind.RemainingBytes)
                    {
                        diagnostic = Diagnostic.Create(
                            DiagnosticDescriptors.CollectionLengthToEndUnsupportedType,
                            property.Locations.FirstOrDefault(),
                            property.Name,
                            property.ContainingType?.Name ?? string.Empty,
                            elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        accessor = null!;
                        return false;
                    }

                    if (elementType.TypeKind == TypeKind.Struct || elementType.TypeKind == TypeKind.Class)
                    {
                        if (lengthStrategy.Kind == CollectionLengthKind.RemainingBytes)
                        {
                            diagnostic = Diagnostic.Create(
                                DiagnosticDescriptors.CollectionLengthToEndUnsupportedType,
                                property.Locations.FirstOrDefault(),
                                property.Name,
                                property.ContainingType?.Name ?? string.Empty,
                                elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                            accessor = null!;
                            return false;
                        }

                        var requiresVersionContext = TypeHasVersionDependentSizeAttribute(elementType);
                        if (requiresVersionContext && !inheritsFullBox)
                        {
                            diagnostic = Diagnostic.Create(
                                DiagnosticDescriptors.VersionAttributeInvalidUsage,
                                property.Locations.FirstOrDefault(),
                                property.Name,
                                property.ContainingType?.Name ?? string.Empty);
                            accessor = null!;
                            return false;
                        }

                        if (!HasComputeSizeMethod(elementType, requiresVersionContext))
                        {
                            diagnostic = Diagnostic.Create(
                                DiagnosticDescriptors.CollectionElementMissingComputeSize,
                                property.Locations.FirstOrDefault(),
                                property.Name,
                                elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                property.ContainingType?.Name ?? string.Empty);
                            accessor = null!;
                            return false;
                        }

                        var elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        accessor = ApplyFlag(new ListPropertyAccessor(null, isStructOrClass: true, elementTypeName, lengthStrategy, requiresVersionContext));
                        return true;
                    }
                }
            }

            _ = GetCollectionLengthStrategy(property, supportsStrategy: false, out diagnostic);
            if (diagnostic is not null)
            {
                accessor = null!;
                return false;
            }

            if (TryCreateSimpleAccessor(property, out var simpleAccessor))
            {
                accessor = ApplyFlag(simpleAccessor);
                return true;
            }

            diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.PropertyTypeUnsupported,
                property.Locations.FirstOrDefault(),
                property.Name,
                property.ContainingType?.Name ?? string.Empty,
                property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            accessor = null!;
            return false;
        }

        private static bool TryGetPrimitiveArrayAccessor(ITypeSymbol elementType, CollectionLengthStrategy lengthStrategy, out PropertyAccessor accessor)
        {
            var elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            PropertyAccessor? candidate = elementType.SpecialType switch
            {
                SpecialType.System_Int16 => new PrimitiveArrayPropertyAccessor("ReadInt16()", "WriteInt16", "sizeof(short)", elementTypeName, lengthStrategy),
                SpecialType.System_UInt16 => new PrimitiveArrayPropertyAccessor("ReadUInt16()", "WriteUInt16", "sizeof(ushort)", elementTypeName, lengthStrategy),
                SpecialType.System_Int32 => new PrimitiveArrayPropertyAccessor("ReadInt32()", "WriteInt32", "sizeof(int)", elementTypeName, lengthStrategy),
                SpecialType.System_UInt32 => new PrimitiveArrayPropertyAccessor("ReadUInt32()", "WriteUInt32", "sizeof(uint)", elementTypeName, lengthStrategy),
                SpecialType.System_Int64 => new PrimitiveArrayPropertyAccessor("ReadInt64()", "WriteInt64", "sizeof(long)", elementTypeName, lengthStrategy),
                SpecialType.System_UInt64 => new PrimitiveArrayPropertyAccessor("ReadUInt64()", "WriteUInt64", "sizeof(ulong)", elementTypeName, lengthStrategy),
                _ => null
            };

            if (candidate is not null)
            {
                accessor = candidate;
                return true;
            }

            accessor = null!;
            return false;
        }

        private static bool TryCreateSimpleAccessor(IPropertySymbol property, out PropertyAccessor accessor)
        {
            PropertyAccessor? candidate = property.Type.SpecialType switch
            {
                SpecialType.System_Int16 => new SimplePropertyAccessor("ReadInt16()", "writer.WriteInt16({0});", "sizeof(short)"),
                SpecialType.System_UInt16 => new SimplePropertyAccessor("ReadUInt16()", "writer.WriteUInt16({0});", "sizeof(ushort)"),
                SpecialType.System_Int32 => new SimplePropertyAccessor("ReadInt32()", "writer.WriteInt32({0});", "sizeof(int)"),
                SpecialType.System_UInt32 => new SimplePropertyAccessor("ReadUInt32()", "writer.WriteUInt32({0});", "sizeof(uint)"),
                SpecialType.System_Int64 => new SimplePropertyAccessor("ReadInt64()", "writer.WriteInt64({0});", "sizeof(long)"),
                SpecialType.System_UInt64 => new SimplePropertyAccessor("ReadUInt64()", "writer.WriteUInt64({0});", "sizeof(ulong)"),
                SpecialType.System_String => new StringPropertyAccessor(),
                _ => null
            };

            if (candidate is not null)
            {
                accessor = candidate;
                return true;
            }

            if (string.Equals(property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "global::System.Guid", StringComparison.Ordinal))
            {
                accessor = new SimplePropertyAccessor("ReadGuid()", "writer.Write({0});", "16");
                return true;
            }

            accessor = null!;
            return false;
        }

        private static bool IsSupportedReservedPropertyType(ITypeSymbol propertyType)
        {
            return propertyType.SpecialType switch
            {
                SpecialType.System_Int32 => true,
                SpecialType.System_UInt32 => true,
                SpecialType.System_Int16 => true,
                SpecialType.System_UInt16 => true,
                SpecialType.System_SByte => true,
                SpecialType.System_Byte => true,
                _ => false,
            };
        }

        private static bool TryGetReservedByteCount(AttributeData attribute, out int byteCount)
        {
            byteCount = 0;
            if (attribute.ConstructorArguments.Length == 0)
            {
                return false;
            }

            var value = attribute.ConstructorArguments[0].Value;
            if (value is null)
            {
                return false;
            }

            switch (value)
            {
                case int intValue when intValue > 0:
                    byteCount = intValue;
                    return true;
                case uint uintValue when uintValue > 0 && uintValue <= int.MaxValue:
                    byteCount = (int)uintValue;
                    return true;
                case short shortValue when shortValue > 0:
                    byteCount = shortValue;
                    return true;
                case ushort ushortValue when ushortValue > 0:
                    byteCount = ushortValue;
                    return true;
                case byte byteValue when byteValue > 0:
                    byteCount = byteValue;
                    return true;
                case long longValue when longValue > 0 && longValue <= int.MaxValue:
                    byteCount = (int)longValue;
                    return true;
                case ulong ulongValue when ulongValue > 0 && ulongValue <= int.MaxValue:
                    byteCount = (int)ulongValue;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetFlagMask(AttributeData attribute, out uint mask)
        {
            mask = 0;
            if (attribute.ConstructorArguments.Length == 0)
            {
                return false;
            }

            var value = attribute.ConstructorArguments[0].Value;
            if (value is null)
            {
                return false;
            }

            switch (value)
            {
                case uint uintValue:
                    mask = uintValue;
                    return true;
                case int intValue when intValue >= 0:
                    mask = unchecked((uint)intValue);
                    return true;
                case ushort ushortValue:
                    mask = ushortValue;
                    return true;
                case byte byteValue:
                    mask = byteValue;
                    return true;
                case long longValue when longValue >= 0 && longValue <= uint.MaxValue:
                    mask = unchecked((uint)longValue);
                    return true;
                case ulong ulongValue when ulongValue <= uint.MaxValue:
                    mask = (uint)ulongValue;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TypeHasVersionDependentSizeAttribute(ITypeSymbol typeSymbol)
        {
            return typeSymbol.GetAttributes().Any(attribute => AttributeMatches(attribute, VersionDependentSizeAttributeMetadataName));
        }

        private static bool IsFixedSizePrimitive(ITypeSymbol typeSymbol)
        {
            return typeSymbol.SpecialType is SpecialType.System_Int16 or
                SpecialType.System_UInt16 or
                SpecialType.System_Int32 or
                SpecialType.System_UInt32 or
                SpecialType.System_Int64 or
                SpecialType.System_UInt64;
        }
    }
}
