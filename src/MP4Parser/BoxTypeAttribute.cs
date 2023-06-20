using System;

namespace Media.ISO
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class BoxTypeAttribute : Attribute
    {
        public string Type { get; set; }

        public Guid? ExtendedType { get; set; }

        public BoxTypeAttribute(string boxType)
        {
            if (Guid.TryParse(boxType, out var guid))
            {
                Type = "uuid";
                ExtendedType = guid;
            }
            else
            {
                Type = boxType;
            }
        }

        public BoxTypeAttribute(Guid extendedType) : 
            this("uuid")
        {
            ExtendedType = extendedType;
        }
    }
}
