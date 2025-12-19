namespace Media.ISO
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class BoxAttribute : Attribute
    {
        public BoxType Type { get; }

        public Guid? ExtendedType { get; }

        public BoxAttribute(Guid extendedType)
        {
            Type = BoxType.UuidBox;
            ExtendedType = extendedType;
        }

        public BoxAttribute(string boxType)
        {
            if (Guid.TryParse(boxType, out var guid))
            {
                Type = BoxType.UuidBox;
                ExtendedType = guid;
            }
            else
            {
                var value = boxType.GetBoxType();
                if (!Enum.IsDefined(typeof(BoxType), value))
                    throw new ArgumentException($"Undefined box {value:x}", nameof(boxType));
                Type = (BoxType)value;
            }
        }

        public BoxAttribute(BoxType boxType)
        {
            Type = boxType;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class FullBoxAttribute : BoxAttribute
    {
        public FullBoxAttribute(BoxType boxType) : base(boxType)
        {
        }

        public FullBoxAttribute(string boxType) : base(boxType)
        {
        }

        public FullBoxAttribute(Guid extendedType) : base(extendedType)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ContainerAttribute : BoxAttribute
    {
        public ContainerAttribute(BoxType boxType) : base(boxType)
        {
        }

        public ContainerAttribute(string boxType) : base(boxType)
        {
        }

        public ContainerAttribute(Guid extendedType) : base(extendedType)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class VersionDependentSizeAttribute : Attribute
    {
        public VersionDependentSizeAttribute(int versionThreshold = 1)
        {
            VersionThreshold = versionThreshold;
        }

        public int VersionThreshold { get; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class FlagOptionalAttribute : Attribute
    {
        public FlagOptionalAttribute(uint flagMask)
        {
            FlagMask = flagMask;
        }

        public uint FlagMask { get; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class FlagDependentAttribute : Attribute
    {
        public FlagDependentAttribute(uint flagMask)
        {
            FlagMask = flagMask;
        }

        public uint FlagMask { get; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ReservedAttribute : Attribute
    {
        public ReservedAttribute(int byteCount)
        {
            ByteCount = byteCount;
        }

        public int ByteCount { get; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CollectionSizeAttribute : Attribute
    {
        public CollectionSizeAttribute(string lengthPropertyName)
        {
            if (string.IsNullOrWhiteSpace(lengthPropertyName))
            {
                throw new ArgumentException("Length property name cannot be null or empty", nameof(lengthPropertyName));
            }

            LengthPropertyName = lengthPropertyName;
        }

        public string LengthPropertyName { get; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CollectionLengthPrefixAttribute : Attribute
    {
        public CollectionLengthPrefixAttribute()
            : this(typeof(uint))
        {
        }

        public CollectionLengthPrefixAttribute(Type fieldType)
        {
            if (!IsSupportedFieldType(fieldType))
            {
                throw new ArgumentException("Collection length prefixes support only byte, ushort, uint, or ulong.", nameof(fieldType));
            }
            FieldType = fieldType;
        }

        public Type FieldType { get; }

        private static bool IsSupportedFieldType(Type type)
        {
            return type == typeof(byte) ||
                   type == typeof(ushort) ||
                   type == typeof(uint) ||
                   type == typeof(ulong);
        }
    }
}
