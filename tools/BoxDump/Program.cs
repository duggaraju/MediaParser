using Media.ISO;
using Media.ISO.Boxes;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace CommandLine
{
    class Program
    {
        static void DumpBox(Box box, int indent = 0)
        {
            var prefix = new String(' ', indent * 2);
            var extendedType = box.ExtendedType != null ? $"Guid={box.ExtendedType}" : string.Empty;
            Console.WriteLine($"{prefix}Box={box.Name} Size={box.Size} {extendedType} children={box.Children.Count}");
            if (box is TrackFragmentExtendedHeaderBox tfxd)
            {
                Console.WriteLine($"  {prefix}Time={tfxd.Time} Duration={tfxd.Duration}");
            }
            else if (box is TrackFragmentDecodeTimeBox tfdt)
            {
                Console.WriteLine($"  {prefix}Time={tfdt.BaseMediaDecodeTime}");
            }
            else
            {
                foreach (var child in box.Children)
                    DumpBox(child, indent + 1);
            }
        }

        static void Skip(Stream stream, int bytes)
        {
            if (stream.CanSeek)
            {
                stream.Position += bytes;
            }
            else
            {
                Span<byte> span = stackalloc byte[Math.Min(bytes, 16 * 1024)];
                while (bytes != 0)
                {
                    bytes -= stream.Read(span);
                }
            }
        }

        static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Option<string>(
                    aliases: new[] { "-i", "--input-file" },
                    description: "file to open")
                {
                    IsRequired = true
                },
                new Option<bool>(
                    aliases: new[] { "-t", "--top-level"},
                    getDefaultValue: () => false,
                    description: "only print top level boxes")
            };

            rootCommand.Description = "MP4 Box dump";

            // Note that the parameters of the handler method are matched according to the names of the options
            rootCommand.Handler = CommandHandler.Create<string, bool>((inputFile, top) =>
            {
                var indent = 0;
                using var stream = File.OpenRead(inputFile);
                if (top)
                {
                    PrintTopLevelBoxes(stream);
                }
                else
                {
                    PrintBoxTree(indent, stream);
                }
            });

            // Parse the incoming args and invoke the handler
            await rootCommand.InvokeAsync(args);
        }

        private static void PrintBoxTree(int indent, FileStream stream)
        {
            foreach (var box in BoxFactory.Parse(stream))
            {
                DumpBox(box, indent);
            }
        }

        private static void PrintTopLevelBoxes(FileStream stream)
        {
            Span<byte> boxHeader = stackalloc byte[8];
            while (true)
            {
                var bytes = stream.Read(boxHeader);
                if (bytes == 0)
                    break;
                var box = BoxFactory.Parse(boxHeader);
                DumpBox(box);
                Skip(stream, (int)box.Size - 8);
            }
        }
    }
}
