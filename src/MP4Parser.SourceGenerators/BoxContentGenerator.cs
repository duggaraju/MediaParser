using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Media.ISO.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public sealed partial class BoxContentGenerator : IIncrementalGenerator
{
    private const string BoxAttributeMetadataName = "Media.ISO.BoxAttribute";
    private const string FullBoxAttributeMetadataName = "Media.ISO.FullBoxAttribute";
    private const string ContainerAttributeMetadataName = "Media.ISO.ContainerAttribute";
    private const string VersionDependentSizeAttributeMetadataName = "Media.ISO.VersionDependentSizeAttribute";
    private const string FlagOptionalAttributeMetadataName = "Media.ISO.FlagOptionalAttribute";
    private const string FlagDependentAttributeMetadataName = "Media.ISO.FlagDependentAttribute";
    private const string ReservedAttributeMetadataName = "Media.ISO.ReservedAttribute";
    private const string CollectionSizeAttributeMetadataName = "Media.ISO.CollectionSizeAttribute";
    private const string CollectionLengthPrefixAttributeMetadataName = "Media.ISO.CollectionLengthPrefixAttribute";
    private const string ReaderMetadataName = "Media.ISO.BoxReader";
    private const string WriterMetadataName = "Media.ISO.BoxWriter";

    private const string ContainerBoxMetadataName = "Media.ISO.Boxes.ContainerBox";
    private const string FullContainerBoxMetadataName = "Media.ISO.Boxes.FullContainerBox";
    private const string FullBoxMetadataName = "Media.ISO.Boxes.FullBox";
    private const string BoxMetadataName = "Media.ISO.Boxes.Box";

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
}
