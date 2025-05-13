using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chunky.dlls
{
    public static class HelperUtility
    {
        public static long ParseBlockSize(string raw)
        {
            string unit = string.Join("", raw.Where(char.IsLetter).ToArray()).ToLower();
            long size = long.Parse(new string(raw.Where(char.IsDigit).ToArray()));

            return unit switch
            {
                "bytes" => size,
                "kb" => size << 10,
                "mb" => size << 20,
                "gb" => size << 30,
                _ => throw new ArgumentException("Invalid block size unit")
            };
        }
    }
}
