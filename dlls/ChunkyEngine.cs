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

            try
            {
                using FileStream sourceStream = new(sourceFile, FileMode.Open, FileAccess.Read);                

                while (sourceStream.Position < sourceStream.Length)
                {
                    long bytesLeft = sourceStream.Length - sourceStream.Position;
                    long chunkSize = Math.Min(blockSizeBytes, bytesLeft);

                    using MemoryStream chunk = new();
                    chunk.Write(BitConverter.GetBytes(index++));
                    chunk.Write(magicBytes);

                    long copied = 0;
                    while (copied < chunkSize)
                    {
                        int read = sourceStream.Read(buffer, 0, (int)Math.Min(_options.BufferSize, chunkSize - copied));
                        if (read == 0) break;
                        chunk.Write(buffer, 0, read);
                        copied += read;
                    }

                    string pathToFile;
                    do
                    {
                        pathToFile = Path.Combine(destinationFolder, Guid.NewGuid().ToString());
                    } while (File.Exists(pathToFile));

                    File.WriteAllBytes(pathToFile, chunk.ToArray());
                    files.Add(pathToFile);

                    if (_options.Verbose)
                        ConsoleEx.Info($"Written chunk: {Path.GetFileName(pathToFile)}");
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
                });

            ConsoleEx.Info($"Found {files.Count()} chunk(s) matching identifier: {_options.Identifier}");

            if (files.Count() == 0)
            {
                ConsoleEx.Error("No valid chunks found.");
                return false;
            }

            try
            {
                using FileStream destination = new(destinationFile, FileMode.CreateNew, FileAccess.Write);

                foreach (var file in files)
                {
                    ConsoleEx.Info($"Appending chunk: {Path.GetFileName(file)}");

                    using (FileStream fs = new(file, FileMode.Open, FileAccess.Read))
                    {
                        fs.Position = 12;

                        int read;
                        while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                            destination.Write(buffer, 0, read);
                    }

                    if (_options.DeleteChunksAfterJoin)
                        File.Delete(file);
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