using System;
using System.IO;
using System.Text;

namespace Media.ISO.Boxes
{
    [BoxType(BoxType.HandlerTypeBox)]
    public class HandlerTypeBox : Box
    {
        private bool _isComponentTypeNamePascalString;
        
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

            var remainingLength = (int)Size - HeaderSize - 24;

            // This part tries to handle string with unknown encoding
            // It may be Pascal string (length as 0th byte), or it may be C string (zero terminated)
            Span<byte> componentTypeNameSpan = stackalloc byte[remainingLength];
            int offset = 0;

            do
            {
                var readBytes = reader.BaseStream.Read(componentTypeNameSpan[offset..]);
                offset += readBytes;
            } while (offset < remainingLength);

            if (componentTypeNameSpan[^1] == 0)
            {
                // it is C string
                _isComponentTypeNamePascalString = false;
                ComponentTypeName = Encoding.ASCII.GetString(componentTypeNameSpan[..^1]);
            }
            else if (componentTypeNameSpan[0] == remainingLength - 1)
            {
                // it is Pascal string
                _isComponentTypeNamePascalString = true;
                ComponentTypeName = Encoding.ASCII.GetString(componentTypeNameSpan[1..]);
            }
            else
            {
                throw new InvalidDataException("Component type name field is neither C not Pascal string");
            }
        }

        protected override void WriteBoxContent(BoxWriter writer)
        {
            writer.WriteInt32(VersionFlags);
            writer.WriteInt32(ComponentType);
            writer.WriteInt32(ComponentSubtype);
            writer.WriteInt32(Manufacturer);
            writer.WriteInt32(Reserved);
            writer.WriteInt32(ReservedMask);

            Span<byte> componentTypeName = stackalloc byte[ComponentTypeName.Length];
            Encoding.ASCII.GetBytes(ComponentTypeName.AsSpan(), componentTypeName);
            
            if (_isComponentTypeNamePascalString)
            {
                writer.BaseStream.WriteByte((byte)ComponentTypeName.Length);
                writer.BaseStream.Write(componentTypeName);
            }
            else
            {
                writer.BaseStream.Write(componentTypeName);
                writer.BaseStream.WriteByte(0);
            }
        }
    }
}