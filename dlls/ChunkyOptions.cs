using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Chunky.dlls
{
    internal class ChunkyOptions
    {
        internal string Identifier { get; set; } = ChunkHeader.Magic;
        internal bool EnableEncryption { get; set; } = false;
        internal RSA? EncryptionPublicKey { get; set; }
        internal RSA? DecryptionPrivateKey { get; set; }
        internal bool DeleteChunksAfterJoin { get; set; } = true;
        public bool Verbose { get; set; } = true;
        internal int BufferSize = 4 * 1024 * 1024;
    }
}
