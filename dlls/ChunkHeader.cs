using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chunky.dlls
{
    internal class ChunkHeader
    {
        internal const int Size = 20;
        internal const string Magic = "chunky";

        internal int Index { get; set; }
        internal string Identifier { get; set; } = Magic;
        internal int TotalChunks { get; set; }

        internal byte[] ToBytes()
        {
            using var ms = new MemoryStream(Size);
            ms.Write(BitConverter.GetBytes(Index));

            string safeId = (Identifier ?? ChunkHeader.Magic).PadRight(8).Substring(0, 8);
            byte[] idBytes = Encoding.ASCII.GetBytes(safeId);

            ms.Write(idBytes);
            ms.Write(BitConverter.GetBytes(TotalChunks));

            return ms.ToArray();
        }

        internal static ChunkHeader FromStream(Stream stream)
        {
            byte[] buffer = new byte[Size];
            stream.ReadExactly(buffer, 0, Size);

            int index = BitConverter.ToInt32(buffer, 0);
            string id = Encoding.ASCII.GetString(buffer, 4, 8).Trim();
            int total = BitConverter.ToInt32(buffer, 12);

            return new ChunkHeader
            {
                Index = index,
                Identifier = id,
                TotalChunks = total
            };
        }
    }
}
