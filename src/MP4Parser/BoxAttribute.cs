using System;

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
}
