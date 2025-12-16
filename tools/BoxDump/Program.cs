using Media.ISO;
using Media.ISO.Boxes;
using System;
using System.Buffers;
using System.CommandLine;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CommandLine
{
    class Program
    {

        static void DumpBoxHeader(BoxHeader boxHeader)
        {
            var extendedType = boxHeader.ExtendedType != null ? $"Guid={boxHeader.ExtendedType}" : string.Empty;
            Console.WriteLine($"Box={boxHeader.Type} {boxHeader.Type.GetBoxName()} Size={boxHeader.Size} {extendedType}");
        }

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
            Console.WriteLine("Skipping {0} bytes...", bytes);
            if (stream.CanSeek)
            {
                stream.Position += bytes;
            }
            else
            {
                var size = Math.Min(bytes, 16 * 1024);
                using var buffer = MemoryPool<byte>.Shared.Rent(size);

                while (bytes > 0)
                {
                    var toRead = Math.Min(bytes, buffer.Memory.Length);
                    var read = stream.Read(buffer.Memory.Span.Slice(0, toRead));
                    if (read == 0) break;
                    bytes -= read;
                }
            }
        }

        static async Task<int> Main(string[] args)
        {
            var fileOption = new Option<string>("-i", "--input-file")
            {
                Required = true,
                Description = "Input MP4 file path or URL"
            };

            var typeOption = new Option<bool>("-t", "--top-level")
            {
                Required = false,
                DefaultValueFactory = _ => false,
                Description = "only print top level boxes"
            };

            var rootCommand = new RootCommand("MP4 Box dump");
            rootCommand.Options.Add(typeOption);
            rootCommand.Options.Add(fileOption);

            // Note that the parameters of the handler method are matched according to the names of the options
            rootCommand.SetAction(async result =>
            {
                var inputFile = result.GetRequiredValue<string>(fileOption);
                if (!Uri.TryCreate(inputFile, UriKind.Absolute, out var uri))
                {
                    inputFile = Path.GetFullPath(inputFile);
                    uri = new Uri(inputFile);
                }
                var top = result.GetValue<bool>(typeOption);
                Console.WriteLine("Processing file: {0}", uri);
                using var stream = await OpenUri(uri);
                if (top)
                {
                    await PrintTopLevelBoxes(stream);
                }
                else
                {
                    await PrintBoxTree(stream);
                }
            });

            // Parse the incoming args and invoke the handler
            var result = rootCommand.Parse(args);
            return await result.InvokeAsync();
        }

        private static async Task<Stream> OpenUri(Uri uri)
        {
            if (uri.IsFile)
            {
                return File.OpenRead(uri.LocalPath);
            }
            else
            {
                var client = new HttpClient();
                var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                return await response.Content.ReadAsStreamAsync();
            }
        }

        private static async Task PrintBoxTree(Stream stream, int indent = 0)
        {
            if (stream.CanSeek)
            {
                foreach (var box in BoxFactory.ParseBoxes(stream))
                {
                    DumpBox(box, indent);
                }
            }
            else
            {
                await PrintTopLevelBoxes(stream);
            }
        }

        private static async Task PrintTopLevelBoxes(Stream stream)
        {
            var boxHeader = new byte[8];
            while (true)
            {
                var bytes = await stream.ReadAtLeastAsync(boxHeader, 8, throwOnEndOfStream: false);
                if (bytes == 0)
                    break;
                var box = BoxFactory.Parse(boxHeader);
                DumpBox(box);
                Skip(stream, (int)box.Size - 8);
            }
        }
    }
}
