using System;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Media.ISO.SourceGenerators;

public sealed partial class BoxContentGenerator
{
    private static void AppendCountDeclarationFromProperty(StringBuilder builder, string indent, string lengthPropertyName)
    {
        builder.Append(indent)
            .Append("var __count = (int)global::System.Math.Max(0L, global::System.Math.Min((long)")
            .Append(lengthPropertyName)
            .Append(", int.MaxValue));")
            .AppendLine();
    }

    private static void AppendCountDeclarationFromPrefix(StringBuilder builder, string indent, CollectionLengthStrategy strategy)
    {
        var readExpression = strategy.GetReaderExpression();
        builder.Append(indent)
            .Append("var __count = (int)global::System.Math.Min(")
            .Append(readExpression)
            .Append(", (ulong)int.MaxValue);")
            .AppendLine();
    }

    private static string InitializeArrayForRead(StringBuilder builder, string indent, string propertyName, string elementTypeName, CollectionLengthStrategy strategy)
    {
        switch (strategy.Kind)
        {
            case CollectionLengthKind.FromProperty:
                AppendCountDeclarationFromProperty(builder, indent, strategy.LengthPropertyName!);
                builder.Append(indent)
                    .Append(propertyName)
                    .Append(" = new ")
                    .Append(elementTypeName)
                    .Append("[__count];")
                    .AppendLine();
                return "__count";
            case CollectionLengthKind.LengthPrefixed:
                AppendCountDeclarationFromPrefix(builder, indent, strategy);
                builder.Append(indent)
                    .Append(propertyName)
                    .Append(" = new ")
                    .Append(elementTypeName)
                    .Append("[__count];")
                    .AppendLine();
                return "__count";
            default:
                return propertyName + ".Length";
        }
    }

    private static string PrepareArrayLengthForWrite(StringBuilder builder, string indent, string propertyName, CollectionLengthStrategy strategy)
    {
        if (strategy.Kind != CollectionLengthKind.LengthPrefixed)
        {
            return propertyName + ".Length";
        }

        builder.Append(indent)
            .Append("var __count = (int)global::System.Math.Max(0, ")
            .Append(GetArrayCountExpression(propertyName))
            .Append(");")
            .AppendLine();
        builder.Append(indent)
            .Append(strategy.GetWriterInvocation("writer", "__count"))
            .AppendLine(";");
        return "__count";
    }

    private static string GetArrayCountExpression(string propertyName) => $"({propertyName}?.Length ?? 0)";

    private static string GetListCountExpression(string propertyName) => $"({propertyName}?.Count ?? 0)";

    private static bool AttributeMatches(AttributeData attribute, string metadataName)
    {
        var attributeName = attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return string.Equals(attributeName, $"global::{metadataName}", StringComparison.Ordinal);
    }
}
