using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chunky.dlls
{
    internal class Threshold
    {
        private const int _defaultPercent = 10;

        private int _percent;
        private int _percentCounter;
        private decimal _threshold;
        private decimal _thresholdCounter;

        private decimal _counter;

        internal Threshold(long summary, int? percent) => Init((decimal)summary, percent);
        internal Threshold(int summary, int? percent) => Init((decimal)summary, percent);
        internal Threshold(float summary, int? percent) => Init((decimal)summary, percent);
        internal Threshold(double summary, int? percent) => Init((decimal)summary, percent);
        internal Threshold(decimal summary, int? percent) => Init(summary, percent);

        private void Init(decimal summary, int? percent)
        {
            _percent = percent ?? _defaultPercent;
            _percentCounter = _percent;

            _threshold = (decimal)(summary / 100 * _percent);
            _thresholdCounter = _threshold;
        }

        public bool TryAdvance(long step, out int percent) => Advance((decimal)step, out percent);
        public bool TryAdvance(int step, out int percent) => Advance((decimal)step, out percent);
        public bool TryAdvance(float step, out int percent) => Advance((decimal)step, out percent);
        public bool TryAdvance(double step, out int percent) => Advance((decimal)step, out percent);
        public bool TryAdvance(decimal step, out int percent) => Advance((decimal)step, out percent);

        private bool Advance(decimal step, out int percent)
        {
            percent = 0;
            _counter += step;

            if (_counter < _thresholdCounter)
                return false;

            _thresholdCounter += _threshold;
            _percent += _percentCounter;

            percent = _percent;

            return true;
        }
    }
}
