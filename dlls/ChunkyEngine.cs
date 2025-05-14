using ShellProgressBar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Chunky.dlls
{
    internal class ChunkyEngine
    {
        private readonly ChunkyOptions _options;

        public ChunkyEngine(ChunkyOptions options)
        {
            _options = options;
        }

        public bool Split(string sourceFile, string destinationFolder, long blockSizeBytes)
        {
            byte[] buffer = new byte[_options.BufferSize];

            byte[] magicBytes = Encoding.ASCII.GetBytes(_options.Identifier);
            if (magicBytes.Length != 8)
                throw new Exception("Magic must be exactly 8 bytes.");

            int index = 0;
            List<string> files = new();

            using ProgressBar summaryBar = new(100, $"Progress", _options.PBarOptions);

            try
            {
                using FileStream sourceStream = new(sourceFile, FileMode.Open, FileAccess.Read);

                Threshold summaryThreshold = new(sourceStream.Length, 5);

                while (sourceStream.Position < sourceStream.Length)
                {
                    using ChildProgressBar? chunkBar = _options.Verbose ? summaryBar.Spawn(100, $"", _options.PBarChildOptions) : null;

                    string pathToFile;
                    do
                    {
                        pathToFile = Path.Combine(destinationFolder, Guid.NewGuid().ToString());
                    } while (File.Exists(pathToFile));
                    files.Add(pathToFile);

                    long bytesLeft = sourceStream.Length - sourceStream.Position;
                    long chunkSize = Math.Min(blockSizeBytes, bytesLeft);

                    Threshold chunkThreshold = new(Math.Min(blockSizeBytes, bytesLeft), 5);
                    using (FileStream chunk = new(pathToFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        chunk.Write(BitConverter.GetBytes(index++));
                        chunk.Write(magicBytes);

                        long copied = 0;
                        while (copied < chunkSize)
                        {
                            int read = (int)Math.Min(_options.BufferSize, chunkSize - copied);
                            sourceStream.ReadExactly(buffer, 0, read);

                            if (read == 0) break;
                            chunk.Write(buffer, 0, read);
                            copied += read;

                            if (_options.Verbose && chunkThreshold.TryAdvance(read, out int chunkPercent))
                            {
                                string fileName = Path.GetFileName(pathToFile);
                                string shortName = fileName.Length > 20 ? fileName.Substring(0, 17) + "..." : fileName.PadRight(20);
                                chunkBar?.Tick(chunkPercent, $"Processing chunk {shortName}");
                            }
                            if (summaryThreshold.TryAdvance(read, out int summaryPercent))
                                summaryBar.Tick(summaryPercent, "Progress");
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                foreach (var file in files)
                    File.Delete(file);

                ConsoleEx.Error($"Split failed: {ex.Message}");
                return false;
            }
        }
        public bool Join(string sourceFolder, string destinationFile)
        {
            byte[] magicBytes = Encoding.ASCII.GetBytes(_options.Identifier);

            byte[] buffer = new byte[_options.BufferSize];

            var files = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories)
                .Where(path =>
                {
                    using (FileStream fs = new(path, FileMode.Open, FileAccess.Read))
                    {
                        byte[] indexBytes = new byte[4];
                        byte[] magicTest = new byte[8];

                        if (fs.Length < 12) return false;
                        fs.ReadExactly(indexBytes, 0, 4);
                        fs.ReadExactly(magicTest, 0, 8);

                        return magicTest.SequenceEqual(magicBytes);
                    }
                })
                .OrderBy(path =>
                {
                    using (FileStream fs = new(path, FileMode.Open, FileAccess.Read))
                    {
                        byte[] indexBytes = new byte[4];
                        fs.ReadExactly(indexBytes, 0, 4);
                        return BitConverter.ToInt32(indexBytes);
                    }
                }).Select(x => new FileInfo(x));

            ConsoleEx.Info($"Found {files.Count()} chunk(s)");

            if (files.Count() == 0)
            {
                ConsoleEx.Error("No valid chunks found.");
                return false;
            }

            try
            {
                using ProgressBar summaryBar = new(100, $"Progress", _options.PBarOptions);
                long length = files.Sum(x => x.Length);
                Threshold summaryThreshold = new(length, 5);

                using FileStream destination = new(destinationFile, FileMode.CreateNew, FileAccess.Write);

                foreach (var file in files)
                {
                    ChildProgressBar? chunkBar = _options.Verbose ? summaryBar.Spawn(100, "Progress", _options.PBarChildOptions) : null;
                    Threshold chunkThreshold = new(file.Length, 5);

                    using (FileStream fs = new(file.FullName, FileMode.Open, FileAccess.Read))
                    {
                        fs.Position = 12;

                        int read;
                        while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            destination.Write(buffer, 0, read);

                            if (_options.Verbose && chunkThreshold.TryAdvance(read, out int chunkPercent))
                            {
                                chunkBar?.Tick(chunkPercent, $"Appending chunk: {file.Name}");
                            }
                            if (summaryThreshold.TryAdvance(read, out int summaryPercent))
                            {
                                summaryBar.Tick(summaryPercent, "Progress");
                            }
                        }
                    }

                    if (_options.DeleteChunksAfterJoin)
                        file.Delete();
                }

                return true;
            }
            catch (Exception ex)
            {
                ConsoleEx.Error($"Join failed: {ex.Message}");
                return false;
            }
        }
    }
}