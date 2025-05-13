using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Chunky.dlls
{
    public static class HelperUtility
    {
        public static long ParseBlockSize(string raw)
        {
            Regex sizeRegex = new(@"(?'size'\d+(\.\d+)?)(?'unit'bytes|kb|mb|gb)", RegexOptions.IgnoreCase);

            Match match = sizeRegex.Match(raw);

            if (!match.Success)
                throw new FormatException("Invalid size format");

            double size = double.Parse(match.Groups["size"].Value, CultureInfo.InvariantCulture);
            string unit = match.Groups["unit"].Value;

            long multiplier = unit switch
            {
                "B" => 1L,
                "KB" => 1L << 10,
                "MB" => 1L << 20,
                "GB" => 1L << 30,
                _ => throw new FormatException("Unknown size unit")
            };

            return (long)(size * multiplier);
        }
    }
}
