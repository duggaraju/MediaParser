using Microsoft.CodeAnalysis;

namespace Media.ISO.SourceGenerators;

public sealed partial class BoxContentGenerator
{
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
            title: "Flag-dependent attribute requires FullBox",
            messageFormat: "Property '{0}' on '{1}' must inherit from FullBox to use FlagDependentAttribute or FlagOptionalAttribute",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor FlagAttributeInvalidMask = new(
            id: "MP4GEN007",
            title: "Flag-dependent attribute invalid mask",
            messageFormat: "Property '{0}' on '{1}' must specify a constant uint mask value for FlagDependentAttribute or FlagOptionalAttribute",
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

        public static readonly DiagnosticDescriptor CollectionSizeMissingProperty = new(
            id: "MP4GEN010",
            title: "Collection size reference missing",
            messageFormat: "Property '{0}' on '{2}' references length property '{1}' which does not exist",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CollectionSizePropertyOrder = new(
            id: "MP4GEN011",
            title: "Collection size reference ordering",
            messageFormat: "Property '{0}' on '{2}' references length property '{1}' which must be declared before the collection",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CollectionLengthAttributeInvalidPropertyType = new(
            id: "MP4GEN012",
            title: "Collection length attribute invalid usage",
            messageFormat: "Property '{0}' on '{1}' uses collection length attributes but is not a supported collection type",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CollectionLengthAttributeConflict = new(
            id: "MP4GEN013",
            title: "Conflicting collection length attributes",
            messageFormat: "Property '{0}' on '{1}' cannot specify both CollectionSizeAttribute and CollectionLengthPrefixAttribute",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CollectionSizeInvalidReference = new(
            id: "MP4GEN014",
            title: "Collection size attribute missing reference",
            messageFormat: "Property '{0}' on '{1}' must specify a non-empty property name for CollectionSizeAttribute",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CollectionLengthPrefixInvalidFieldType = new(
            id: "MP4GEN015",
            title: "Collection length prefix invalid type",
            messageFormat: "Property '{0}' on '{1}' specifies an unsupported collection length prefix type",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CollectionElementMissingComputeSize = new(
            id: "MP4GEN016",
            title: "Collection element missing ComputeSize",
            messageFormat: "Property '{0}' on '{2}' uses element type '{1}' which must declare an instance ComputeSize() method returning int",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }
}
