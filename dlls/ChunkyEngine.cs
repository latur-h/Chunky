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
            int index = 0;
            List<string> files = new();

            try
            {
                using FileStream sourceStream = new(sourceFile, FileMode.Open, FileAccess.Read);
                int totalChunks = (int)Math.Ceiling((double)sourceStream.Length / blockSizeBytes);

                while (sourceStream.Position < sourceStream.Length)
                {
                    long chunkSize = Math.Min(blockSizeBytes, sourceStream.Length - sourceStream.Position);
                    byte[] data;
                    using (var ms = new MemoryStream())
                    {
                        long copied = 0;
                        while (copied < chunkSize)
                        {
                            int read = sourceStream.Read(buffer, 0, (int)Math.Min(_options.BufferSize, chunkSize - copied));
                            if (read == 0) break;
                            ms.Write(buffer, 0, read);
                            copied += read;
                        }
                        data = ms.ToArray();
                    }

                    var header = new ChunkHeader
                    {
                        Index = index++,
                        Identifier = _options.Identifier,
                        TotalChunks = totalChunks
                    };

                    if (_options.Verbose)
                        ConsoleEx.Info($"Header -> index={header.Index}, id={header.Identifier}, total={header.TotalChunks}");

                    string pathToFile;
                    do
                    {
                        pathToFile = Path.Combine(destinationFolder, Guid.NewGuid().ToString());
                    } while (File.Exists(pathToFile));

                    using var fileStream = new FileStream(pathToFile, FileMode.CreateNew);
                    var headerBytes = header.ToBytes();
                    fileStream.Write(headerBytes, 0, headerBytes.Length);
                    fileStream.Write(data, 0, data.Length);

                    if (_options.Verbose)
                        ConsoleEx.Info($"Written chunk: {Path.GetFileName(pathToFile)}");

                    files.Add(pathToFile);
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
            byte[] buffer = new byte[_options.BufferSize];
            RSA? rsa = _options.DecryptionPrivateKey;

            var files = Directory.GetFiles(sourceFolder, "*", SearchOption.TopDirectoryOnly)                
                .Select(path => new { Path = path, Header = ReadHeader(path) })
                .Where(x => x.Header != null && x.Header.Identifier.Trim() == _options.Identifier)
                .OrderBy(x => x.Header!.Index)
                .ToList();

            ConsoleEx.Info($"Found {files.Count} chunk(s) matching identifier: {_options.Identifier}");

            if (files.Count == 0)
            {
                ConsoleEx.Error("No valid chunks found.");
                return false;
            }

            try
            {
                using var output = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None);

                foreach (var file in files)
                {
                    if (_options.Verbose)
                        ConsoleEx.Info($"Appending chunk: {Path.GetFileName(file.Path)}");

                    using (FileStream fs = new FileStream(file.Path, FileMode.Open, FileAccess.Read))
                    {
                        fs.Seek(ChunkHeader.Size, SeekOrigin.Begin);

                        int read;
                        while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            byte[] raw = buffer[..read];
                            output.Write(raw, 0, raw.Length);
                        }
                    }

                    if (_options.DeleteChunksAfterJoin)
                        File.Delete(file.Path);
                }

                output.Flush(true);
                return true;
            }
            catch (Exception ex)
            {
                ConsoleEx.Error($"Join failed: {ex.Message}");
                return false;
            }
        }
        private ChunkHeader? ReadHeader(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                return fs.Length >= ChunkHeader.Size ? ChunkHeader.FromStream(fs) : null;
            }
            catch
            {
                return null;
            }
        }
    }
}