using System;
using System.Text;

namespace Media.ISO.Boxes
{
    [BoxType(BoxType.HandlerTypeBox)]
    public class HandlerTypeBox : Box
    {
        public HandlerTypeBox() : base(BoxType.HandlerTypeBox)
        {
        }

        public int VersionFlags { get; set; }
        
        public int ComponentType { get; set; }
        
        public int ComponentSubtype { get; set; }
        
        public int Manufacturer { get; set; }
        
        public int Reserved { get; set; }

        public int ReservedMask { get; set; }

        public string ComponentTypeName { get; set; } = string.Empty;

        protected override int BoxContentSize => 25 + ComponentTypeName.Length;

        protected override void ParseBoxContent(BoxReader reader)
        {
            VersionFlags = reader.ReadInt32();
            ComponentType = reader.ReadInt32();
            ComponentSubtype = reader.ReadInt32();
            Manufacturer = reader.ReadInt32();
            Reserved = reader.ReadInt32();
            ReservedMask = reader.ReadInt32();
            ComponentTypeName = reader.ReadString();
        }
    }
}