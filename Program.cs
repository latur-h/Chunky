using System.CommandLine;
using Chunky.dlls;
using System;

namespace Chunky
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
#if DEBUG
            FileInfo fileInfo = new FileInfo(@"C:\Users\Latur\source\repos\Chunky\bin\Debug\net9.0\data\1.zip");
            DirectoryInfo directoryInfo = new DirectoryInfo(@"C:\Users\Latur\source\repos\Chunky\bin\Debug\net9.0\data\dest");

            string blockSize = "1GB";

            Split(fileInfo, directoryInfo, blockSize, true);

            FileInfo destFile = new FileInfo(@"C:\Users\Latur\source\repos\Chunky\bin\Debug\net9.0\data\2.zip");
            //Join(directoryInfo, destFile, true, true);

            return 0;
            #else
            #region Command line
            var rootCommand = new RootCommand("chunky - file splitter and combiner");

            #region Global Options
            var verboseFlag = new Option<bool>("--verbose", "Enable verbose output");

            rootCommand.AddGlobalOption(verboseFlag);
            #endregion

            #region Split
            var splitSource = new Option<FileInfo>(["--source", "-s"], "Source file to split") { IsRequired = true };
            var splitDestination = new Option<DirectoryInfo?>(["--destination", "-d"], "Output folder");
            var splitBlockSize = new Option<string>(["--block-size", "-bs"], "Block size (e.g., 10MB). Limited by 2GB per chunk") { IsRequired = true };            

            var splitCommand = new Command("split", "Split a file into chunks")
            {
                splitSource,
                splitDestination,
                splitBlockSize
            };

            splitCommand.SetHandler(Split, splitSource, splitDestination, splitBlockSize, verboseFlag);
            #endregion

            #region Join
            var joinSource = new Option<DirectoryInfo>(["--source", "-s"], "Folder with chunks") { IsRequired = true };
            var joinDestination = new Option<FileInfo>(["--destination", "-d"], "Output file") { IsRequired = true };
            var splitChunkDeletion = new Option<bool>(["--delete-chunks", "-dc"], "Delete chunks after join");

            var joinCommand = new Command("join", "Combine chunks into one file")
            {
                joinSource,
                joinDestination
            };

            joinCommand.SetHandler(Join, joinSource, joinDestination, verboseFlag, splitChunkDeletion);
            #endregion

            rootCommand.Add(splitCommand);
            rootCommand.Add(joinCommand);

            return await rootCommand.InvokeAsync(args);
            #endregion
            #endif
        }

        private static void Split(FileInfo source, DirectoryInfo? dest, string blockSizeRaw, bool verbose)
        {
            if (!source.Exists)
            {
                ConsoleEx.Error("Source file doesn't exist.");
                return;
            }

            string destinationDir = string.Empty;

            if (dest is null)
                do
                {
                    destinationDir = Path.Combine(Path.GetDirectoryName(source.FullName)!, Guid.NewGuid().ToString());
                } while (Directory.Exists(destinationDir));
            else
            {
                destinationDir = dest.FullName;
            }

            Directory.CreateDirectory(destinationDir);

            var options = new ChunkyOptions
            {
                Verbose = verbose
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

            bool result = engine.Split(source.FullName, destinationDir, blockSizeLength);

            if (result) ConsoleEx.Info("Split successful.");
        }
        private static void Join(DirectoryInfo source, FileInfo dest, bool verbose, bool deleteChunks)
        {
            if (!source.Exists) { ConsoleEx.Error("Source folder does not exist."); return; }
            if (dest.Exists) { ConsoleEx.Error("Destination file already exists."); return; }

            var options = new ChunkyOptions
            {
                Verbose = verbose,
                DeleteChunksAfterJoin = deleteChunks
            };

            var engine = new ChunkyEngine(options);
            bool result = engine.Join(source.FullName, dest.FullName);
            if (result) ConsoleEx.Info("Join completed.");
        }
    }
}