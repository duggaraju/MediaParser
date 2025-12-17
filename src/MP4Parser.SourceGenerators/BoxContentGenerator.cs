using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Media.ISO.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public sealed class BoxContentGenerator : IIncrementalGenerator
{
    private const string BoxTypeAttributeName = "Media.ISO.BoxTypeAttribute";
    private const string ReaderMetadataName = "Media.ISO.BoxReader";
    private const string WriterMetadataName = "Media.ISO.BoxWriter";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = context.SyntaxProvider.ForAttributeWithMetadataName(
                BoxTypeAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (attributeContext, cancellationToken) => GetGenerationTarget(attributeContext, cancellationToken))
            .Where(static target => target is not null)
            .Select(static (target, _) => target!);

        context.RegisterSourceOutput(targets, static (productionContext, target) =>
        {
            foreach (var diagnostic in target.Diagnostics)
            {
                productionContext.ReportDiagnostic(diagnostic);
            }

            if (!target.ShouldGenerate)
            {
                return;
            }

            var sourceText = GenerateSource(target);
            productionContext.AddSource($"{target.Symbol.Name}.BoxContent.g.cs", sourceText);
        });
    }

    private const string ContainerBoxMetadataName = "Media.ISO.Boxes.ContainerBox";
    private const string FullContainerBoxMetadataName = "Media.ISO.Boxes.FullContainerBox";

    private static GenerationTarget? GetGenerationTarget(GeneratorAttributeSyntaxContext context, System.Threading.CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        if (!IsPartial(typeSymbol))
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.TypeMustBePartial, typeSymbol.Locations.FirstOrDefault(), typeSymbol.Name));
            return new GenerationTarget(typeSymbol, ImmutableArray<PropertyModel>.Empty, shouldGenerate: false, diagnostics.ToImmutable());
        }

        if (InheritsFrom(typeSymbol, ContainerBoxMetadataName) || InheritsFrom(typeSymbol, FullContainerBoxMetadataName))
        {
            return new GenerationTarget(typeSymbol, ImmutableArray<PropertyModel>.Empty, shouldGenerate: false, diagnostics.ToImmutable());
        }

        if (HasOverride(typeSymbol, "ParseBoxContent") || HasOverride(typeSymbol, "WriteBoxContent") || HasContentSizeOverride(typeSymbol))
        {
            return new GenerationTarget(typeSymbol, ImmutableArray<PropertyModel>.Empty, shouldGenerate: false, diagnostics.ToImmutable());
        }

        var autoPropertySet = CollectAutoProperties(typeSymbol);
        var properties = ImmutableArray.CreateBuilder<PropertyModel>();

        foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!SymbolEqualityComparer.Default.Equals(property.ContainingType, typeSymbol))
            {
                continue;
            }

            if (property.IsStatic || property.SetMethod is null || property.GetMethod is null)
            {
                continue;
            }

            if (!autoPropertySet.Contains(property, SymbolEqualityComparer.Default))
            {
                continue;
            }

            if (property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) is not PropertyDeclarationSyntax syntax)
            {
                continue;
            }

            if (!FieldAccessor.TryCreate(property, out var accessor))
            {
                diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.PropertyTypeUnsupported,
                    property.Locations.FirstOrDefault(),
                    property.Name,
                    typeSymbol.Name,
                    property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                continue;
            }

            properties.Add(new PropertyModel(property.Name, accessor, syntax.SpanStart));
        }

        var orderedProperties = properties.Count > 0
            ? properties.ToImmutable().Sort(static (left, right) => left.Order.CompareTo(right.Order))
            : ImmutableArray<PropertyModel>.Empty;

        var shouldGenerate = diagnostics.Count == 0 && !orderedProperties.IsDefaultOrEmpty && orderedProperties.Length > 0;

        return new GenerationTarget(typeSymbol, orderedProperties, shouldGenerate, diagnostics.ToImmutable());
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
            .Append(target.Symbol.Name)
            .AppendLine()
            .Append(indentation)
            .AppendLine("{");

        var memberIndent = indentation + "    ";
        AppendParseMethod(builder, memberIndent, target.Properties);
        builder.AppendLine();
        AppendWriteMethod(builder, memberIndent, target.Properties);
        builder.AppendLine();
        AppendContentSize(builder, memberIndent, target.Properties);
        builder.AppendLine();

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
        foreach (var property in properties)
        {
            builder.Append(indent)
                .Append("    ")
                .Append(property.Name)
                .Append(" = reader.")
                .Append(property.Accessor.ReaderInvocation)
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
            builder.Append(indent)
                .Append("    ")
                .AppendFormat(CultureInfo.InvariantCulture, property.Accessor.WriterInvocationFormat, property.Name)
                .AppendLine();
        }
        builder.Append(indent).AppendLine("}");
    }

    private static void AppendContentSize(StringBuilder builder, string indent, IReadOnlyList<PropertyModel> properties)
    {
        builder.Append(indent).AppendLine("protected override int ContentSize =>");
        for (int i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            var sizeExpression = string.Format(CultureInfo.InvariantCulture, property.Accessor.SizeExpressionFormat, property.Name);
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
            ImmutableArray<Diagnostic> diagnostics)
        {
            Symbol = symbol;
            Properties = properties;
            ShouldGenerate = shouldGenerate;
            Diagnostics = diagnostics;
        }

        public INamedTypeSymbol Symbol { get; }

        public ImmutableArray<PropertyModel> Properties { get; }

        public bool ShouldGenerate { get; }

        public ImmutableArray<Diagnostic> Diagnostics { get; }
    }

    private sealed class PropertyModel
    {
        public PropertyModel(string name, FieldAccessor accessor, int order)
        {
            Name = name;
            Accessor = accessor;
            Order = order;
        }

        public string Name { get; }

        public FieldAccessor Accessor { get; }

        public int Order { get; }
    }

    private readonly struct FieldAccessor
    {
        public FieldAccessor(string readerInvocation, string writerInvocationFormat, string sizeExpressionFormat)
        {
            ReaderInvocation = readerInvocation;
            WriterInvocationFormat = writerInvocationFormat;
            SizeExpressionFormat = sizeExpressionFormat;
        }

        public string ReaderInvocation { get; }

        public string WriterInvocationFormat { get; }

        public string SizeExpressionFormat { get; }

        public static bool TryCreate(IPropertySymbol property, out FieldAccessor accessor)
        {
            accessor = default;
            if (property.Type.SpecialType != SpecialType.None)
            {
                if (TryFromSpecialType(property.Type.SpecialType, out accessor))
                {
                    return true;
                }
            }

            var typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (typeName == "global::System.Guid")
            {
                accessor = new FieldAccessor("ReadGuid()", "writer.Write({0});", "16");
                return true;
            }

            if (typeName == "global::System.String")
            {
                accessor = new FieldAccessor(
                    "ReadString()",
                    "writer.WriteString({0} ?? global::System.String.Empty);",
                    "(({0}?.Length ?? 0) + 1)");
                return true;
            }

            return false;
        }

        private static bool TryFromSpecialType(SpecialType specialType, out FieldAccessor accessor)
        {
            accessor = specialType switch
            {
                SpecialType.System_Int16 => new FieldAccessor("ReadInt16()", "writer.WriteInt16({0});", "sizeof(short)"),
                SpecialType.System_UInt16 => new FieldAccessor("ReadUInt16()", "writer.WriteUInt16({0});", "sizeof(ushort)"),
                SpecialType.System_Int32 => new FieldAccessor("ReadInt32()", "writer.WriteInt32({0});", "sizeof(int)"),
                SpecialType.System_UInt32 => new FieldAccessor("ReadUInt32()", "writer.WriteUInt32({0});", "sizeof(uint)"),
                SpecialType.System_Int64 => new FieldAccessor("ReadInt64()", "writer.WriteInt64({0});", "sizeof(long)"),
                SpecialType.System_UInt64 => new FieldAccessor("ReadUInt64()", "writer.WriteUInt64({0});", "sizeof(ulong)"),
                SpecialType.System_String => new FieldAccessor(
                    "ReadString()",
                    "writer.WriteString({0} ?? global::System.String.Empty);",
                    "(({0}?.Length ?? 0) + 1)"),
                _ => default
            };
            return accessor.ReaderInvocation is not null;
        }
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
    }
}
