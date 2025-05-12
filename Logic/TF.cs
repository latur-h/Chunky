using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Chunky.Logic
{
    internal class TF
    {
        public TF() { }
        public (bool status, string? reason, int amount) To_ManyFiles(string sourceFile, string destinationFolder, long blockSizeBytes)
        {
            const int bufferSize = 4 * 1024 * 1024;
            byte[] buffer = new byte[bufferSize];

            const string magic = "chunky01"; // 8 bytes
            byte[] magicBytes = Encoding.ASCII.GetBytes(magic);
            if (magicBytes.Length != 8)
                throw new Exception("Magic must be exactly 8 bytes.");

            using FileStream sourceStream = new(sourceFile, FileMode.Open, FileAccess.Read);
            List<string> files = [];

            int index = 0;
            try
            {
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
                        int read = sourceStream.Read(buffer, 0, (int)Math.Min(bufferSize, chunkSize - copied));
                        if (read == 0) break;
                        chunk.Write(buffer, 0, read);
                        copied += read;
                    }

                    string pathToFile;
                    do
                    {
                        pathToFile = Path.Combine(destinationFolder, Guid.NewGuid().ToString());
                    } while (File.Exists(pathToFile));

                    ConsoleEx.Info($"Writing file: {Path.GetFileName(pathToFile)}");
                    File.WriteAllBytes(pathToFile, chunk.ToArray());
                    files.Add(pathToFile);
                }
            }
            catch (Exception ex)
            {
                foreach (var file in files)
                    File.Delete(file);

                return (false, ex.Message, 0);
            }

            return (true, null, index);
        }


        public void From_ManyFiles(string sourceFolder, string destinationFile)
        {
            const string magic = "chunky01";
            byte[] magicBytes = Encoding.ASCII.GetBytes(magic);

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

            byte[] buffer = new byte[4 * 1024 * 1024];

            using FileStream destination = new(destinationFile, FileMode.CreateNew, FileAccess.Write);

            foreach (var file in files)
            {
                ConsoleEx.Info($"Reading file: {Path.GetFileName(file)}");

                using (FileStream fs = new(file, FileMode.Open, FileAccess.Read))
                {
                    fs.Position = 12;

                    int read;
                    while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                        destination.Write(buffer, 0, read);
                }

                File.Delete(file);
            }
        }

    }
}
