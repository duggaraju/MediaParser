using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Media.ISO.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public sealed class BoxContentGenerator : IIncrementalGenerator
{
    private const string BoxAttributeMetadataName = "Media.ISO.BoxAttribute";
    private const string FullBoxAttributeMetadataName = "Media.ISO.FullBoxAttribute";
    private const string ContainerAttributeMetadataName = "Media.ISO.ContainerAttribute";
    private const string VersionDependentSizeAttributeMetadataName = "Media.ISO.VersionDependentSizeAttribute";
    private const string FlagOptionalAttributeMetadataName = "Media.ISO.FlagOptionalAttribute";
    private const string ReservedAttributeMetadataName = "Media.ISO.ReservedAttribute";
    private const string ReaderMetadataName = "Media.ISO.BoxReader";
    private const string WriterMetadataName = "Media.ISO.BoxWriter";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        RegisterTargets(context, CreateTargets(context, BoxAttributeMetadataName, isContainer: false, requiresFullBoxBase: false));
        RegisterTargets(context, CreateTargets(context, FullBoxAttributeMetadataName, isContainer: false, requiresFullBoxBase: true));
        RegisterTargets(context, CreateTargets(context, ContainerAttributeMetadataName, isContainer: true, requiresFullBoxBase: false));
    }

    private static IncrementalValuesProvider<GenerationTarget> CreateTargets(
        IncrementalGeneratorInitializationContext context,
        string metadataName,
        bool isContainer,
        bool requiresFullBoxBase)
    {
        return context.SyntaxProvider.ForAttributeWithMetadataName(
                metadataName,
                static (node, _) => node is ClassDeclarationSyntax,
                (attributeContext, cancellationToken) => GetGenerationTarget(attributeContext, cancellationToken, isContainer, requiresFullBoxBase))
            .Where(static target => target is not null)
            .Select(static (target, _) => target!);
    }

    private static void RegisterTargets(
        IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<GenerationTarget> targets)
    {
        context.RegisterSourceOutput(targets, static (productionContext, target) =>
        {
            foreach (var diagnostic in target.Diagnostics)
            {
                productionContext.ReportDiagnostic(diagnostic);
            }

            if (!target.ShouldGenerate && !target.RequiresFullBoxBase)
            {
                return;
            }

            var sourceText = GenerateSource(target);
            productionContext.AddSource($"{target.Symbol.Name}.BoxContent.g.cs", sourceText);
        });
    }

    private const string ContainerBoxMetadataName = "Media.ISO.Boxes.ContainerBox";
    private const string FullContainerBoxMetadataName = "Media.ISO.Boxes.FullContainerBox";
    private const string FullBoxMetadataName = "Media.ISO.Boxes.FullBox";

    private static GenerationTarget? GetGenerationTarget(
        GeneratorAttributeSyntaxContext context,
        System.Threading.CancellationToken cancellationToken,
        bool isContainer,
        bool requiresFullBoxBase)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        if (!IsPartial(typeSymbol))
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.TypeMustBePartial, typeSymbol.Locations.FirstOrDefault(), typeSymbol.Name));
            return new GenerationTarget(typeSymbol, ImmutableArray<PropertyModel>.Empty, shouldGenerate: false, diagnostics.ToImmutable(), isContainer, requiresFullBoxBase, generateParse: false, generateWrite: false, generateContentSize: false);
        }

        var inheritsContainer = InheritsFrom(typeSymbol, ContainerBoxMetadataName);
        var inheritsFullContainer = InheritsFrom(typeSymbol, FullContainerBoxMetadataName);
        var inheritsFullBox = InheritsFrom(typeSymbol, FullBoxMetadataName);
        var needsFullBoxBase = requiresFullBoxBase && !inheritsFullBox;
        var isFullBox = inheritsFullBox || needsFullBoxBase;

        if (!isContainer && (inheritsContainer || inheritsFullContainer))
        {
            return new GenerationTarget(typeSymbol, ImmutableArray<PropertyModel>.Empty, shouldGenerate: false, diagnostics.ToImmutable(), isContainer, needsFullBoxBase, generateParse: false, generateWrite: false, generateContentSize: false);
        }

        if (isContainer)
        {
            var shouldGenerateContainer = diagnostics.Count == 0 && !inheritsContainer;
            return new GenerationTarget(typeSymbol, ImmutableArray<PropertyModel>.Empty, shouldGenerateContainer, diagnostics.ToImmutable(), isContainer, needsFullBoxBase, generateParse: false, generateWrite: false, generateContentSize: false);
        }

        if (!isFullBox)
        {
            return new GenerationTarget(typeSymbol, ImmutableArray<PropertyModel>.Empty, shouldGenerate: false, diagnostics.ToImmutable(), isContainer, needsFullBoxBase, generateParse: false, generateWrite: false, generateContentSize: false);
        }

        var hasParseOverride = HasOverride(typeSymbol, "ParseBoxContent");
        var hasWriteOverride = HasOverride(typeSymbol, "WriteBoxContent");
        var hasContentSizeOverride = HasContentSizeOverride(typeSymbol);

        var generateParse = !hasParseOverride;
        var generateWrite = !hasWriteOverride;
        var generateContentSize = !hasContentSizeOverride;

        if (!generateParse && !generateWrite && !generateContentSize)
        {
            return new GenerationTarget(typeSymbol, ImmutableArray<PropertyModel>.Empty, shouldGenerate: false, diagnostics.ToImmutable(), isContainer, needsFullBoxBase, generateParse, generateWrite, generateContentSize);
        }

        var autoPropertySet = CollectAutoProperties(typeSymbol);
        var syntaxTreeOrder = CreateSyntaxTreeOrder(typeSymbol);
        var properties = ImmutableArray.CreateBuilder<PropertyModel>();

        foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!SymbolEqualityComparer.Default.Equals(property.ContainingType, typeSymbol))
            {
                continue;
            }

            var hasReservedAttribute = property.GetAttributes().Any(attribute => AttributeMatches(attribute, ReservedAttributeMetadataName));

            if (property.IsStatic || property.GetMethod is null)
            {
                continue;
            }

            // Allow properties without setters if they are arrays/collections (we modify elements, not the property itself)
            var isArrayOrCollection = property.Type is IArrayTypeSymbol ||
                (property.Type is INamedTypeSymbol namedType &&
                 (namedType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IList_T ||
                  namedType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Collections.Generic.List<T>"));

            if (!hasReservedAttribute && property.SetMethod is null && !isArrayOrCollection)
            {
                continue;
            }

            if (!hasReservedAttribute && !isArrayOrCollection && !autoPropertySet.Contains(property, SymbolEqualityComparer.Default))
            {
                continue;
            }

            var syntaxReference = property.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxReference is null)
            {
                continue;
            }

            if (syntaxReference.GetSyntax(cancellationToken) is not PropertyDeclarationSyntax syntax)
            {
                continue;
            }

            if (!PropertyAccessorFactory.TryCreate(property, isFullBox, out var accessor, out var accessorDiagnostic))
            {
                diagnostics.Add(accessorDiagnostic ?? Diagnostic.Create(
                    DiagnosticDescriptors.PropertyTypeUnsupported,
                    property.Locations.FirstOrDefault(),
                    property.Name,
                    typeSymbol.Name,
                    property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                continue;
            }

            var treeOrdinal = syntax.SyntaxTree is not null && syntaxTreeOrder.TryGetValue(syntax.SyntaxTree, out var ordinal)
                ? ordinal
                : int.MaxValue;
            var orderValue = ((long)treeOrdinal << 32) | (uint)syntax.SpanStart;

            properties.Add(new PropertyModel(property, accessor, orderValue));
        }

        var orderedProperties = properties.Count > 0
            ? properties.ToImmutable().Sort(static (left, right) => left.Order.CompareTo(right.Order))
            : ImmutableArray<PropertyModel>.Empty;

        if (!orderedProperties.IsDefaultOrEmpty)
        {
            for (var i = 0; i < orderedProperties.Length; i++)
            {
                if (orderedProperties[i].Accessor.RequiresRemainingBytes && i != orderedProperties.Length - 1)
                {
                    diagnostics.Add(Diagnostic.Create(
                        DiagnosticDescriptors.RemainingBytesPropertyMustBeLast,
                        orderedProperties[i].Symbol.Locations.FirstOrDefault(),
                        orderedProperties[i].Symbol.Name,
                        typeSymbol.Name));
                }
            }
        }

        var hasAnyGeneration = generateParse || generateWrite || generateContentSize;
        var shouldGenerate = diagnostics.Count == 0 && !orderedProperties.IsDefaultOrEmpty && orderedProperties.Length > 0 && hasAnyGeneration;

        return new GenerationTarget(typeSymbol, orderedProperties, shouldGenerate, diagnostics.ToImmutable(), isContainer, needsFullBoxBase, generateParse, generateWrite, generateContentSize);
    }

    private static bool InheritsFrom(INamedTypeSymbol typeSymbol, string metadataName)
    {
        var fullyQualified = string.Concat("global::", metadataName);
        for (var current = typeSymbol.BaseType; current is not null; current = current.BaseType)
        {
            if (string.Equals(current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), fullyQualified, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private static ImmutableHashSet<IPropertySymbol> CollectAutoProperties(INamedTypeSymbol typeSymbol)
    {
        var builder = ImmutableHashSet.CreateBuilder<IPropertySymbol>(SymbolEqualityComparer.Default);
        foreach (var field in typeSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (field.AssociatedSymbol is IPropertySymbol property)
            {
                builder.Add(property);
            }
        }
        return builder.ToImmutable();
    }

    private static Dictionary<SyntaxTree, int> CreateSyntaxTreeOrder(INamedTypeSymbol typeSymbol)
    {
        var treeOrder = new Dictionary<SyntaxTree, int>();
        var index = 0;
        foreach (var syntaxReference in typeSymbol.DeclaringSyntaxReferences)
        {
            var tree = syntaxReference.SyntaxTree;
            if (tree is null || treeOrder.ContainsKey(tree))
            {
                continue;
            }

            treeOrder[tree] = index++;
        }

        return treeOrder;
    }

    private static bool IsPartial(INamedTypeSymbol typeSymbol)
    {
        foreach (var syntaxReference in typeSymbol.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax() is ClassDeclarationSyntax classDeclaration &&
                classDeclaration.Modifiers.Any(static modifier => modifier.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)))
            {
                return true;
            }
        }
        return false;
    }

    private static bool HasOverride(INamedTypeSymbol typeSymbol, string memberName)
    {
        foreach (var member in typeSymbol.GetMembers(memberName))
        {
            if (member is IMethodSymbol methodSymbol && methodSymbol.IsOverride)
            {
                return true;
            }
        }
        return false;
    }

    private static bool HasContentSizeOverride(INamedTypeSymbol typeSymbol)
    {
        foreach (var member in typeSymbol.GetMembers("ContentSize"))
        {
            if (member is IPropertySymbol propertySymbol && propertySymbol.IsOverride)
            {
                return true;
            }
        }
        return false;
    }

    private static string GenerateSource(GenerationTarget target)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();

        var namespaceName = target.Symbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : target.Symbol.ContainingNamespace.ToDisplayString();

        var indentation = string.Empty;
        if (!string.IsNullOrEmpty(namespaceName))
        {
            builder.Append("namespace ").Append(namespaceName).AppendLine();
            builder.AppendLine("{");
            indentation = "    ";
        }

        builder.Append(indentation)
            .Append(GetAccessibilityKeyword(target.Symbol.DeclaredAccessibility))
            .Append(" partial class ")
            .Append(target.Symbol.Name);

        if (target.IsContainer)
        {
            builder.Append(" : global::").Append(ContainerBoxMetadataName);
        }
        else if (target.RequiresFullBoxBase)
        {
            builder.Append(" : global::").Append(FullBoxMetadataName);
        }

        builder.AppendLine()
            .Append(indentation)
            .AppendLine("{");

        if (target.IsContainer)
        {
            builder.Append(indentation).AppendLine("}");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                builder.AppendLine("}");
            }
            return builder.ToString();
        }

        if (!target.ShouldGenerate || target.Properties.IsDefaultOrEmpty || target.Properties.Length == 0)
        {
            builder.Append(indentation).AppendLine("}");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                builder.AppendLine("}");
            }
            return builder.ToString();
        }

        var memberIndent = indentation + "    ";
        var emittedSection = false;

        if (target.GenerateParse)
        {
            AppendParseMethod(builder, memberIndent, target.Properties);
            emittedSection = true;
        }

        if (target.GenerateWrite)
        {
            if (emittedSection)
            {
                builder.AppendLine();
            }

            AppendWriteMethod(builder, memberIndent, target.Properties);
            emittedSection = true;
        }

        if (target.GenerateContentSize)
        {
            if (emittedSection)
            {
                builder.AppendLine();
            }

            AppendContentSize(builder, memberIndent, target.Properties);
            emittedSection = true;
        }

        if (emittedSection)
        {
            builder.AppendLine();
        }

        builder.Append(indentation).AppendLine("}");
        if (!string.IsNullOrEmpty(namespaceName))
        {
            builder.AppendLine("}");
        }

        return builder.ToString();
    }

    private static void AppendParseMethod(StringBuilder builder, string indent, IReadOnlyList<PropertyModel> properties)
    {
        builder.Append(indent)
            .Append("protected override void ParseBoxContent(global::")
            .Append(ReaderMetadataName)
            .AppendLine(" reader)");
        builder.Append(indent).AppendLine("{");
        builder.Append(indent).AppendLine("    var __remaining = (int)global::System.Math.Max(0L, Size - HeaderSize);");
        for (var __index = 0; __index < properties.Count; __index++)
        {
            var property = properties[__index];
            var __indent = indent + "    ";
            if (property.Accessor is StringPropertyAccessor __stringAccessor)
            {
                var __isLast = __index == properties.Count - 1;
                __stringAccessor.AppendRead(builder, __indent, property.Name, __isLast);
            }
            else
            {
                property.Accessor.AppendRead(builder, __indent, property.Name);
            }

            builder.Append(indent)
                .Append("    __remaining -= ")
                .Append(property.Accessor.GetSizeExpression(property.Name))
                .AppendLine(";");
        }
        builder.Append(indent).AppendLine("}");
    }

    private static void AppendWriteMethod(StringBuilder builder, string indent, IReadOnlyList<PropertyModel> properties)
    {
        builder.Append(indent)
            .Append("protected override void WriteBoxContent(global::")
            .Append(WriterMetadataName)
            .AppendLine(" writer)");
        builder.Append(indent).AppendLine("{");
        foreach (var property in properties)
        {
            property.Accessor.AppendWrite(builder, indent + "    ", property.Name);
        }
        builder.Append(indent).AppendLine("}");
    }

    private static void AppendContentSize(StringBuilder builder, string indent, IReadOnlyList<PropertyModel> properties)
    {
        builder.Append(indent).AppendLine("protected override int ContentSize =>");
        for (int i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            var sizeExpression = property.Accessor.GetSizeExpression(property.Name);
            builder.Append(indent)
                .Append("    ");
            if (i > 0)
            {
                builder.Append("+ ");
            }
            builder.Append(sizeExpression);
            builder.AppendLine(i == properties.Count - 1 ? ";" : string.Empty);
        }
    }

    private static string GetAccessibilityKeyword(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public => "public",
        Accessibility.Internal => "internal",
        Accessibility.Protected => "protected",
        Accessibility.Private => "private",
        Accessibility.ProtectedAndInternal => "private protected",
        Accessibility.ProtectedOrInternal => "protected internal",
        _ => "internal"
    };

    private sealed class GenerationTarget
    {
        public GenerationTarget(
            INamedTypeSymbol symbol,
            ImmutableArray<PropertyModel> properties,
            bool shouldGenerate,
            ImmutableArray<Diagnostic> diagnostics,
            bool isContainer,
            bool requiresFullBoxBase,
            bool generateParse,
            bool generateWrite,
            bool generateContentSize)
        {
            Symbol = symbol;
            Properties = properties;
            ShouldGenerate = shouldGenerate;
            Diagnostics = diagnostics;
            IsContainer = isContainer;
            RequiresFullBoxBase = requiresFullBoxBase;
            GenerateParse = generateParse;
            GenerateWrite = generateWrite;
            GenerateContentSize = generateContentSize;
        }

        public INamedTypeSymbol Symbol { get; }

        public ImmutableArray<PropertyModel> Properties { get; }

        public bool ShouldGenerate { get; }

        public ImmutableArray<Diagnostic> Diagnostics { get; }

        public bool IsContainer { get; }

        public bool RequiresFullBoxBase { get; }

        public bool GenerateParse { get; }

        public bool GenerateWrite { get; }

        public bool GenerateContentSize { get; }
    }

    private sealed class PropertyModel
    {
        public PropertyModel(IPropertySymbol symbol, PropertyAccessor accessor, long order)
        {
            Symbol = symbol;
            Accessor = accessor;
            Order = order;
        }

        public IPropertySymbol Symbol { get; }

        public string Name => Symbol.Name;

        public PropertyAccessor Accessor { get; }

        public long Order { get; }
    }

    private abstract class PropertyAccessor
    {
        public abstract void AppendRead(StringBuilder builder, string indent, string propertyName);

        public abstract void AppendWrite(StringBuilder builder, string indent, string propertyName);

        public abstract string GetSizeExpression(string propertyName);

        public virtual bool RequiresRemainingBytes => false;
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

        public override string GetSizeExpression(string propertyName) => "(" + propertyName + "?.Length ?? 0)";
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

        public PrimitiveArrayPropertyAccessor(string readerMethod, string writerMethod, string sizeOfExpression)
        {
            _readerMethod = readerMethod;
            _writerMethod = writerMethod;
            _sizeOfExpression = sizeOfExpression;
        }

        public override void AppendRead(StringBuilder builder, string indent, string propertyName)
        {
            builder.Append(indent)
                .Append("for (var __i = 0; __i < ")
                .Append(propertyName)
                .AppendLine(".Length; __i++)");
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
            builder.Append(indent)
                .Append("foreach (var __item in ")
                .Append(propertyName)
                .AppendLine(")");
            builder.Append(indent).AppendLine("{");
            builder.Append(indent)
                .Append("    writer.")
                .Append(_writerMethod)
                .AppendLine("(__item);");
            builder.Append(indent).AppendLine("}");
        }

        public override string GetSizeExpression(string propertyName) =>
            $"({propertyName}.Length * {_sizeOfExpression})";
    }

    private sealed class StructArrayPropertyAccessor : PropertyAccessor
    {
        private readonly string _elementTypeName;

        public StructArrayPropertyAccessor(string elementTypeName)
        {
            _elementTypeName = elementTypeName;
        }

        public override void AppendRead(StringBuilder builder, string indent, string propertyName)
        {
            builder.Append(indent)
                .Append("for (var __i = 0; __i < ")
                .Append(propertyName)
                .AppendLine(".Length; __i++)");
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
                .AppendLine("[__i].Read(reader);");
            builder.Append(indent).AppendLine("}");
        }

        public override void AppendWrite(StringBuilder builder, string indent, string propertyName)
        {
            builder.Append(indent)
                .Append("foreach (var __item in ")
                .Append(propertyName)
                .AppendLine(")");
            builder.Append(indent).AppendLine("{");
            builder.Append(indent)
                .AppendLine("    __item.Write(writer);");
            builder.Append(indent).AppendLine("}");
        }

        public override string GetSizeExpression(string propertyName)
        {
            return $"global::System.Linq.Enumerable.Sum({propertyName}, __x => __x.ComputeSize())";
        }
    }

    private sealed class ListPropertyAccessor : PropertyAccessor
    {
        private readonly PropertyAccessor _inner;
        private readonly bool _isStructOrClass;

        public ListPropertyAccessor(PropertyAccessor inner, bool isStructOrClass)
        {
            _inner = inner;
            _isStructOrClass = isStructOrClass;
        }

        public override void AppendRead(StringBuilder builder, string indent, string propertyName)
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
                    .AppendLine("    __item.Read(reader);");
                builder.Append(indent)
                    .Append("    ")
                    .Append(propertyName)
                    .AppendLine("[__i] = __item;");
            }
            else
            {
                // For primitive types in lists, we need to generate the read directly
                // This is handled by the inner accessor
                _inner.AppendRead(builder, indent + "    ", $"{propertyName}[__i]");
            }
            builder.Append(indent).AppendLine("}");
        }

        public override void AppendWrite(StringBuilder builder, string indent, string propertyName)
        {
            builder.Append(indent)
                .Append("foreach (var __item in ")
                .Append(propertyName)
                .AppendLine(")");
            builder.Append(indent).AppendLine("{");
            if (_isStructOrClass)
            {
                builder.Append(indent)
                    .AppendLine("    __item.Write(writer);");
            }
            else
            {
                _inner.AppendWrite(builder, indent + "    ", "__item");
            }
            builder.Append(indent).AppendLine("}");
        }

        public override string GetSizeExpression(string propertyName)
        {
            if (_isStructOrClass)
            {
                return $"global::System.Linq.Enumerable.Sum({propertyName}, __x => __x.ComputeSize())";
            }
            else
            {
                return $"({propertyName}.Count * {_inner.GetSizeExpression("__dummy").Replace("__dummy", "1").Replace("sizeof", "sizeof")})";
            }
        }
    }

    private static class PropertyAccessorFactory
    {
        public static bool TryCreate(IPropertySymbol property, bool inheritsFullBox, out PropertyAccessor accessor, out Diagnostic? diagnostic)
        {
            diagnostic = null;
            var flagAttribute = property.GetAttributes().FirstOrDefault(attribute => AttributeMatches(attribute, FlagOptionalAttributeMetadataName));
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

            var reservedAttribute = property.GetAttributes().FirstOrDefault(attribute => AttributeMatches(attribute, ReservedAttributeMetadataName));
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

            var versionAttribute = property.GetAttributes().FirstOrDefault(attribute => AttributeMatches(attribute, VersionDependentSizeAttributeMetadataName));
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
                if (arrayType.ElementType.SpecialType == SpecialType.System_Byte)
                {
                    accessor = ApplyFlag(new ByteArrayPropertyAccessor());
                    return true;
                }

                // Handle arrays of primitive types
                if (TryGetPrimitiveArrayAccessor(arrayType.ElementType, out var primitiveArrayAccessor))
                {
                    accessor = ApplyFlag(primitiveArrayAccessor);
                    return true;
                }

                // Handle arrays of structs/classes
                if (arrayType.ElementType.TypeKind == TypeKind.Struct || arrayType.ElementType.TypeKind == TypeKind.Class)
                {
                    var elementTypeName = arrayType.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    accessor = ApplyFlag(new StructArrayPropertyAccessor(elementTypeName));
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

            // Handle IList<T> and List<T>
            if (property.Type is INamedTypeSymbol namedType)
            {
                var isList = false;
                ITypeSymbol? elementType = null;

                // Check if it's IList<T> or List<T>
                if (namedType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IList_T ||
                    (namedType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Collections.Generic.List<T>"))
                {
                    isList = true;
                    elementType = namedType.TypeArguments.FirstOrDefault();
                }

                if (isList && elementType is not null)
                {
                    // Handle lists of primitive types
                    if (TryGetPrimitiveArrayAccessor(elementType, out var primitiveListAccessor))
                    {
                        accessor = ApplyFlag(new ListPropertyAccessor(primitiveListAccessor, isStructOrClass: false));
                        return true;
                    }

                    // Handle lists of structs/classes
                    if (elementType.TypeKind == TypeKind.Struct || elementType.TypeKind == TypeKind.Class)
                    {
                        var elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        accessor = ApplyFlag(new ListPropertyAccessor(new StructArrayPropertyAccessor(elementTypeName), isStructOrClass: true));
                        return true;
                    }
                }
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

        private static bool TryGetPrimitiveArrayAccessor(ITypeSymbol elementType, out PropertyAccessor accessor)
        {
            PropertyAccessor? candidate = elementType.SpecialType switch
            {
                SpecialType.System_Int16 => new PrimitiveArrayPropertyAccessor("ReadInt16()", "WriteInt16", "sizeof(short)"),
                SpecialType.System_UInt16 => new PrimitiveArrayPropertyAccessor("ReadUInt16()", "WriteUInt16", "sizeof(ushort)"),
                SpecialType.System_Int32 => new PrimitiveArrayPropertyAccessor("ReadInt32()", "WriteInt32", "sizeof(int)"),
                SpecialType.System_UInt32 => new PrimitiveArrayPropertyAccessor("ReadUInt32()", "WriteUInt32", "sizeof(uint)"),
                SpecialType.System_Int64 => new PrimitiveArrayPropertyAccessor("ReadInt64()", "WriteInt64", "sizeof(long)"),
                SpecialType.System_UInt64 => new PrimitiveArrayPropertyAccessor("ReadUInt64()", "WriteUInt64", "sizeof(ulong)"),
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
    }

    private static bool AttributeMatches(AttributeData attribute, string metadataName)
    {
        var attributeName = attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return string.Equals(attributeName, $"global::{metadataName}", StringComparison.Ordinal);
    }

    private static class DiagnosticDescriptors
    {
        private const string Category = "BoxContentGenerator";

        public static readonly DiagnosticDescriptor TypeMustBePartial = new(
            id: "MP4GEN001",
            title: "Box type must be partial",
            messageFormat: "Class '{0}' must be declared as partial to enable box content generation",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor PropertyTypeUnsupported = new(
            id: "MP4GEN002",
            title: "Unsupported property type",
            messageFormat: "Property '{0}' on '{1}' uses unsupported type '{2}' for box content generation",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor VersionAttributeInvalidUsage = new(
            id: "MP4GEN003",
            title: "Version-dependent size attribute requires FullBox",
            messageFormat: "Property '{0}' on '{1}' must inherit from FullBox to use VersionDependentSizeAttribute",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor VersionAttributeInvalidType = new(
            id: "MP4GEN004",
            title: "Version-dependent size attribute invalid type",
            messageFormat: "Property '{0}' uses type '{1}' but VersionDependentSizeAttribute supports only 64-bit integer properties",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RemainingBytesPropertyMustBeLast = new(
            id: "MP4GEN005",
            title: "Array properties consuming remaining bytes must be last",
            messageFormat: "Property '{0}' on '{1}' must be the final property because it consumes the remaining box bytes",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor FlagAttributeInvalidUsage = new(
            id: "MP4GEN006",
            title: "Flag optional attribute requires FullBox",
            messageFormat: "Property '{0}' on '{1}' must inherit from FullBox to use FlagOptionalAttribute",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor FlagAttributeInvalidMask = new(
            id: "MP4GEN007",
            title: "Flag optional attribute invalid mask",
            messageFormat: "Property '{0}' on '{1}' must specify a constant uint mask value for FlagOptionalAttribute",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ReservedAttributeInvalidType = new(
            id: "MP4GEN008",
            title: "Reserved attribute invalid type",
            messageFormat: "Property '{0}' on '{1}' uses unsupported type '{2}' for ReservedAttribute",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ReservedAttributeInvalidSize = new(
            id: "MP4GEN009",
            title: "Reserved attribute invalid size",
            messageFormat: "Property '{0}' on '{1}' must specify a positive byte count for ReservedAttribute",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }
}
