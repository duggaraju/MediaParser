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
            Console.WriteLine($"{prefix}Box={box.Name} Size={box.Size} children={box.Children.Count}");
            foreach (var child in box.Children)
                DumpBox(child, indent + 1);
        }

        static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Option<string>(
                    "--input-file",
                    "file to open")
                {
                    IsRequired = true
                }
            };

            rootCommand.Description = "MP4 Box dump";

            // Note that the parameters of the handler method are matched according to the names of the options
            rootCommand.Handler = CommandHandler.Create<string>(inputFile =>
            {
                var indent = 0;
                using var stream = File.OpenRead(inputFile);
                foreach (var box in BoxFactory.Parse(stream))
                {
                    DumpBox(box, indent);
                }
            });

            // Parse the incoming args and invoke the handler
            await rootCommand.InvokeAsync(args);
        }
    }
}
