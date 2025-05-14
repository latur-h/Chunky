using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Chunky.dlls
{
    internal class ChunkyOptions
    {
        internal string Identifier = "chunkyoo";
        internal bool DeleteChunksAfterJoin { get; set; } = true;
        public bool Verbose { get; set; } = true;

        internal int BufferSize = 4 * 1024 * 1024;
    }
}
