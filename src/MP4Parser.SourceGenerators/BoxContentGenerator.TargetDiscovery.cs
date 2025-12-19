using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Media.ISO.SourceGenerators;

public sealed partial class BoxContentGenerator
{
    private static GenerationTarget? GetGenerationTarget(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken,
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
            return new GenerationTarget(typeSymbol, ImmutableArray<PropertyModel>.Empty, shouldGenerate: false, diagnostics.ToImmutable(), isContainer, requiresFullBoxBase, isFullBox: false, generateParse: false, generateWrite: false, generateContentSize: false);
        }

        var inheritsContainer = InheritsFrom(typeSymbol, ContainerBoxMetadataName);
        var inheritsFullContainer = InheritsFrom(typeSymbol, FullContainerBoxMetadataName);
        var inheritsFullBox = InheritsFrom(typeSymbol, FullBoxMetadataName);
        var inheritsBox = InheritsFrom(typeSymbol, BoxMetadataName);
        var needsFullBoxBase = requiresFullBoxBase && !inheritsFullBox;
        var isFullBox = inheritsFullBox || needsFullBoxBase;
        var supportsBoxGeneration = isFullBox || inheritsBox;

        if (!isContainer && (inheritsContainer || inheritsFullContainer))
        {
            return new GenerationTarget(typeSymbol, ImmutableArray<PropertyModel>.Empty, shouldGenerate: false, diagnostics.ToImmutable(), isContainer, needsFullBoxBase, isFullBox, generateParse: false, generateWrite: false, generateContentSize: false);
        }

        if (isContainer)
        {
            var shouldGenerateContainer = diagnostics.Count == 0 && !inheritsContainer;
            return new GenerationTarget(typeSymbol, ImmutableArray<PropertyModel>.Empty, shouldGenerateContainer, diagnostics.ToImmutable(), isContainer, needsFullBoxBase, isFullBox, generateParse: false, generateWrite: false, generateContentSize: false);
        }

        if (!supportsBoxGeneration)
        {
            return new GenerationTarget(typeSymbol, ImmutableArray<PropertyModel>.Empty, shouldGenerate: false, diagnostics.ToImmutable(), isContainer, needsFullBoxBase, isFullBox, generateParse: false, generateWrite: false, generateContentSize: false);
        }

        var hasParseOverride = HasOverride(typeSymbol, "ParseBody");
        var hasWriteOverride = HasOverride(typeSymbol, "WriteBoxBody");
        var hasContentSizeOverride = isFullBox
            ? HasContentSizeOverride(typeSymbol)
            : HasComputeBodySizeOverride(typeSymbol);

        if (isFullBox)
        {
            hasParseOverride |= HasOverride(typeSymbol, "ParseBoxContent");
            hasWriteOverride |= HasOverride(typeSymbol, "WriteBoxContent");
        }

        var generateParse = !hasParseOverride;
        var generateWrite = !hasWriteOverride;
        var generateContentSize = !hasContentSizeOverride;

        if (!generateParse && !generateWrite && !generateContentSize)
        {
            return new GenerationTarget(typeSymbol, ImmutableArray<PropertyModel>.Empty, shouldGenerate: false, diagnostics.ToImmutable(), isContainer, needsFullBoxBase, isFullBox, generateParse, generateWrite, generateContentSize);
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

            var attributes = property.GetAttributes();
            var hasReservedAttribute = attributes.Any(attribute => AttributeMatches(attribute, ReservedAttributeMetadataName));
            var hasFlagAttribute = attributes.Any(attribute =>
                AttributeMatches(attribute, FlagOptionalAttributeMetadataName) ||
                AttributeMatches(attribute, FlagDependentAttributeMetadataName));

            if (property.IsStatic || property.GetMethod is null)
            {
                continue;
            }

            var isArrayOrCollection = property.Type is IArrayTypeSymbol ||
                (property.Type is INamedTypeSymbol namedType &&
                 (namedType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IList_T ||
                  namedType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Collections.Generic.List<T>"));

            if (!hasReservedAttribute && property.SetMethod is null && !isArrayOrCollection)
            {
                continue;
            }

            if (!hasReservedAttribute && !isArrayOrCollection && !hasFlagAttribute &&
                !autoPropertySet.Contains(property, SymbolEqualityComparer.Default))
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

            ValidateCollectionSizeReferences(typeSymbol, orderedProperties, diagnostics);
        }

        var hasAnyGeneration = generateParse || generateWrite || generateContentSize;
        var shouldGenerate = diagnostics.Count == 0 && !orderedProperties.IsDefaultOrEmpty && orderedProperties.Length > 0 && hasAnyGeneration;

        return new GenerationTarget(typeSymbol, orderedProperties, shouldGenerate, diagnostics.ToImmutable(), isContainer, needsFullBoxBase, isFullBox, generateParse, generateWrite, generateContentSize);
    }

    private static void ValidateCollectionSizeReferences(
        INamedTypeSymbol typeSymbol,
        ImmutableArray<PropertyModel> orderedProperties,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var propertyIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < orderedProperties.Length; i++)
        {
            propertyIndex[orderedProperties[i].Name] = i;
        }

        for (var i = 0; i < orderedProperties.Length; i++)
        {
            var property = orderedProperties[i];
            var strategy = property.Accessor.LengthStrategy;
            if (strategy.Kind != CollectionLengthKind.FromProperty)
            {
                continue;
            }

            if (string.IsNullOrEmpty(strategy.LengthPropertyName) ||
                !propertyIndex.TryGetValue(strategy.LengthPropertyName!, out var referencedIndex))
            {
                diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.CollectionSizeMissingProperty,
                    property.Symbol.Locations.FirstOrDefault(),
                    property.Name,
                    strategy.LengthPropertyName ?? string.Empty,
                    typeSymbol.Name));
                continue;
            }

            if (referencedIndex >= i)
            {
                diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.CollectionSizePropertyOrder,
                    property.Symbol.Locations.FirstOrDefault(),
                    property.Name,
                    strategy.LengthPropertyName ?? string.Empty,
                    typeSymbol.Name));
            }
        }
    }

    private static CollectionLengthStrategy GetCollectionLengthStrategy(
        IPropertySymbol property,
        bool supportsStrategy,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        var attributes = property.GetAttributes();
        var sizeAttribute = attributes.FirstOrDefault(attribute => AttributeMatches(attribute, CollectionSizeAttributeMetadataName));
        var prefixAttribute = attributes.FirstOrDefault(attribute => AttributeMatches(attribute, CollectionLengthPrefixAttributeMetadataName));

        if ((sizeAttribute is not null || prefixAttribute is not null) && !supportsStrategy)
        {
            diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.CollectionLengthAttributeInvalidPropertyType,
                property.Locations.FirstOrDefault(),
                property.Name,
                property.ContainingType?.Name ?? string.Empty);
            return CollectionLengthStrategy.None;
        }

        if (sizeAttribute is not null && prefixAttribute is not null)
        {
            diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.CollectionLengthAttributeConflict,
                property.Locations.FirstOrDefault(),
                property.Name,
                property.ContainingType?.Name ?? string.Empty);
            return CollectionLengthStrategy.None;
        }

        if (sizeAttribute is not null)
        {
            if (sizeAttribute.ConstructorArguments.Length == 0 ||
                sizeAttribute.ConstructorArguments[0].Value is not string lengthProperty ||
                string.IsNullOrWhiteSpace(lengthProperty))
            {
                diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.CollectionSizeInvalidReference,
                    property.Locations.FirstOrDefault(),
                    property.Name,
                    property.ContainingType?.Name ?? string.Empty);
                return CollectionLengthStrategy.None;
            }

            return CollectionLengthStrategy.FromProperty(lengthProperty);
        }

        if (prefixAttribute is not null)
        {
            var fieldSize = CollectionLengthFieldSize.UInt32;
            if (prefixAttribute.ConstructorArguments.Length > 0)
            {
                var argumentValue = prefixAttribute.ConstructorArguments[0].Value;
                if (argumentValue is ITypeSymbol typeSymbol)
                {
                    if (!TryGetCollectionLengthFieldSize(typeSymbol, out fieldSize))
                    {
                        diagnostic = Diagnostic.Create(
                            DiagnosticDescriptors.CollectionLengthPrefixInvalidFieldType,
                            property.Locations.FirstOrDefault(),
                            property.Name,
                            property.ContainingType?.Name ?? string.Empty);
                        return CollectionLengthStrategy.None;
                    }
                }
            }

            return CollectionLengthStrategy.LengthPrefixed(fieldSize);
        }

        return CollectionLengthStrategy.None;
    }

    private static bool TryGetCollectionLengthFieldSize(ITypeSymbol typeSymbol, out CollectionLengthFieldSize fieldSize)
    {
        fieldSize = typeSymbol.SpecialType switch
        {
            SpecialType.System_Byte => CollectionLengthFieldSize.Byte,
            SpecialType.System_UInt16 => CollectionLengthFieldSize.UInt16,
            SpecialType.System_UInt32 => CollectionLengthFieldSize.UInt32,
            SpecialType.System_UInt64 => CollectionLengthFieldSize.UInt64,
            _ => CollectionLengthFieldSize.UInt32,
        };

        return typeSymbol.SpecialType is SpecialType.System_Byte or
            SpecialType.System_UInt16 or
            SpecialType.System_UInt32 or
            SpecialType.System_UInt64;
    }

    private static bool HasComputeSizeMethod(ITypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers("ComputeSize"))
            {
                if (member is IMethodSymbol method &&
                    !method.IsStatic &&
                    method.Parameters.Length == 0 &&
                    method.ReturnType.SpecialType == SpecialType.System_Int32)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryCreatePrimitiveElementAccessor(ITypeSymbol elementType, out PropertyAccessor accessor)
    {
        accessor = elementType.SpecialType switch
        {
            SpecialType.System_Int16 => new SimplePropertyAccessor("ReadInt16()", "writer.WriteInt16({0});", "sizeof(short)"),
            SpecialType.System_UInt16 => new SimplePropertyAccessor("ReadUInt16()", "writer.WriteUInt16({0});", "sizeof(ushort)"),
            SpecialType.System_Int32 => new SimplePropertyAccessor("ReadInt32()", "writer.WriteInt32({0});", "sizeof(int)"),
            SpecialType.System_UInt32 => new SimplePropertyAccessor("ReadUInt32()", "writer.WriteUInt32({0});", "sizeof(uint)"),
            SpecialType.System_Int64 => new SimplePropertyAccessor("ReadInt64()", "writer.WriteInt64({0});", "sizeof(long)"),
            SpecialType.System_UInt64 => new SimplePropertyAccessor("ReadUInt64()", "writer.WriteUInt64({0});", "sizeof(ulong)"),
            _ => null!
        };

        return accessor is not null;
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

    private static bool HasComputeBodySizeOverride(INamedTypeSymbol typeSymbol)
    {
        foreach (var member in typeSymbol.GetMembers("ComputeBodySize"))
        {
            if (member is IMethodSymbol methodSymbol && methodSymbol.IsOverride)
            {
                return true;
            }
        }

        return false;
    }
}
