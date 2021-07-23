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
            Type = boxType;
        }

        public BoxTypeAttribute(Guid extendedType) : 
            this("uuid")
        {
            ExtendedType = extendedType;
        }
    }
}
