using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Media.ISO.SourceGenerators;

public sealed partial class BoxContentGenerator
{
    private sealed class GenerationTarget
    {
        public GenerationTarget(
            INamedTypeSymbol symbol,
            ImmutableArray<PropertyModel> properties,
            bool shouldGenerate,
            ImmutableArray<Diagnostic> diagnostics,
            bool isContainer,
            bool requiresFullBoxBase,
            bool isFullBox,
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
            IsFullBox = isFullBox;
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

        public bool IsFullBox { get; }

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

    private enum CollectionLengthKind
    {
        None,
        FromProperty,
        LengthPrefixed,
        RemainingBytes
    }

    private enum CollectionLengthFieldSize
    {
        Byte = 1,
        UInt16 = 2,
        UInt32 = 4,
        UInt64 = 8
    }

    private readonly struct CollectionLengthStrategy
    {
        public static CollectionLengthStrategy None => default;

        private CollectionLengthStrategy(CollectionLengthKind kind, string? propertyName, CollectionLengthFieldSize fieldSize)
        {
            Kind = kind;
            LengthPropertyName = propertyName;
            FieldSize = fieldSize;
        }

        public CollectionLengthKind Kind { get; }

        public string? LengthPropertyName { get; }

        public CollectionLengthFieldSize FieldSize { get; }

        public static CollectionLengthStrategy FromProperty(string propertyName)
        {
            return new CollectionLengthStrategy(CollectionLengthKind.FromProperty, propertyName, CollectionLengthFieldSize.UInt32);
        }

        public static CollectionLengthStrategy LengthPrefixed(CollectionLengthFieldSize fieldSize)
        {
            return new CollectionLengthStrategy(CollectionLengthKind.LengthPrefixed, null, fieldSize);
        }

        public static CollectionLengthStrategy RemainingBytes()
        {
            return new CollectionLengthStrategy(CollectionLengthKind.RemainingBytes, null, CollectionLengthFieldSize.UInt32);
        }

        public int PrefixSizeInBytes => Kind == CollectionLengthKind.LengthPrefixed ? (int)FieldSize : 0;

        public string GetReaderExpression()
        {
            return FieldSize switch
            {
                CollectionLengthFieldSize.Byte => "(ulong)reader.ReadByte()",
                CollectionLengthFieldSize.UInt16 => "(ulong)reader.ReadUInt16()",
                CollectionLengthFieldSize.UInt32 => "(ulong)reader.ReadUInt32()",
                CollectionLengthFieldSize.UInt64 => "reader.ReadUInt64()",
                _ => "(ulong)0"
            };
        }

        public string GetWriterInvocation(string writerName, string countExpression)
        {
            return FieldSize switch
            {
                CollectionLengthFieldSize.Byte => $"{writerName}.WriteByte((byte)global::System.Math.Min({countExpression}, 0xFF))",
                CollectionLengthFieldSize.UInt16 => $"{writerName}.WriteUInt16((ushort)global::System.Math.Min({countExpression}, 0xFFFF))",
                CollectionLengthFieldSize.UInt32 => $"{writerName}.WriteUInt32((uint){countExpression})",
                CollectionLengthFieldSize.UInt64 => $"{writerName}.WriteUInt64((ulong){countExpression})",
                _ => $"{writerName}.WriteUInt32((uint){countExpression})"
            };
        }
    }
}
