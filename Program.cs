using System.CommandLine;
using Chunky.dlls;
using System;

namespace Chunky
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("chunky - file splitter and combiner");

            #region Global Options
            var verboseFlag = new Option<bool>("--verbose", "Enable verbose output");

            rootCommand.AddGlobalOption(verboseFlag);
            #endregion

            #region Split
            var splitSource = new Option<FileInfo>(["--source", "-s"], "Source file to split") { IsRequired = true };
            var splitDestination = new Option<DirectoryInfo?>(["--destination", "-d"], "Output folder");
            var splitBlockSize = new Option<string>(["--block-size", "-bs"], "Block size (e.g., 10MB)") { IsRequired = true };

            var splitCommand = new Command("split", "Split a file into chunks")
            {
                splitSource,
                splitDestination,
                splitBlockSize
            };

            splitCommand.SetHandler((FileInfo source, DirectoryInfo? dest, string blockSizeRaw, bool verbose) =>
            {
                if (!source.Exists)
                {
                    ConsoleEx.Error("Source file doesn't exist.");
                    return;
                }

                string destinationDir = dest?.FullName ?? Path.Combine(Path.GetDirectoryName(source.FullName)!, Guid.NewGuid().ToString());
                Directory.CreateDirectory(destinationDir);

                var options = new ChunkyOptions
                {
                    Verbose = verbose,
                    DeleteChunksAfterJoin = true
                };

                var engine = new ChunkyEngine(options);

                long blockSizeLength = 0;
                try
                {
                    blockSizeLength = HelperUtility.ParseBlockSize(blockSizeRaw);
                }
                catch (Exception ex)
                {
                    ConsoleEx.Error($"{ex.Message}");
                    return;
                }

                if (blockSizeLength > int.MaxValue)
                {
                    ConsoleEx.Error($"Block size cannot exceed 2GB because of .NET limitations, set a smaller one.");
                    return;
                }

                bool result = engine.Split(source.FullName, destinationDir, blockSizeLength);

                if (result) ConsoleEx.Info("Split successful.");
            }, splitSource, splitDestination, splitBlockSize, verboseFlag);
            #endregion

            #region Join
            var joinSource = new Option<DirectoryInfo>(["--source", "-s"], "Folder with chunks") { IsRequired = true };
            var joinDestination = new Option<FileInfo>(["--destination", "-d"], "Output file") { IsRequired = true };

            var joinCommand = new Command("join", "Combine chunks into one file")
            {
                joinSource,
                joinDestination
            };

            joinCommand.SetHandler((DirectoryInfo source, FileInfo dest, bool verbose) =>
            {
                if (!source.Exists) { ConsoleEx.Error("Source folder does not exist."); return; }
                if (dest.Exists) { ConsoleEx.Error("Destination file already exists."); return; }

                var options = new ChunkyOptions
                {
                    Verbose = verbose,
                    DeleteChunksAfterJoin = true
                };

                var engine = new ChunkyEngine(options);
                bool result = engine.Join(source.FullName, dest.FullName);
                if (result) ConsoleEx.Info("Join completed.");
            }, joinSource, joinDestination, verboseFlag);
            #endregion

            rootCommand.Add(splitCommand);
            rootCommand.Add(joinCommand);

            return await rootCommand.InvokeAsync(args);
        }
    }
}