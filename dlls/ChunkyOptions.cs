using ShellProgressBar;
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
        internal bool Verbose { get; set; } = true;

        internal int BufferSize = 4 * 1024 * 1024;

        internal ProgressBarOptions PBarOptions = new() 
        {
            ForegroundColor = ConsoleColor.Gray,
            ProgressCharacter = '#',
            BackgroundCharacter = ' ',
            DisplayTimeInRealTime = false,
            CollapseWhenFinished = false,
            ShowEstimatedDuration = false,
            ProgressBarOnBottom = true,
        };
        internal ProgressBarOptions PBarChildOptions = new()
        {
            ForegroundColor = ConsoleColor.Yellow,
            ProgressCharacter = '-',
            BackgroundCharacter = ' ',
            CollapseWhenFinished = false,
            DenseProgressBar = true
        };
    }
}
