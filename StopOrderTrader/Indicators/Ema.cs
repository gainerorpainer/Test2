using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Objects;

namespace StopOrderTrader.Indicators
{
    public class Ema : AbstractIndicator<SimplePoint>
    {
        public int Period { get; private set; }

        public override int MinimumInitValuesNecessary { get; }

        double _indicator = 0;
        readonly double _coeff;

        public Ema(int period)
        {
            Period = period;

            MinimumInitValuesNecessary = Period;

            _coeff = 2.0 / (1.0 + period);
        }


        protected override IEnumerable<SimplePoint> CalcInit(IList<BinanceKline> initialCandles)
        {
            List<SimplePoint> result = new List<SimplePoint>(initialCandles.Count - Period + 1);

            // First point is different
            _indicator = initialCandles.Take(Period).Sum(x => (double)x.Close) / Period;
            result.Add(new SimplePoint(initialCandles[Period - 1].OpenTime, _indicator));

            // Than the following
            for (int i = Period; i < initialCandles.Count; i++)
            {
                _indicator += _coeff * ((double)initialCandles[i].Close - _indicator);
                result.Add(new SimplePoint(initialCandles[i].OpenTime, _indicator));
            }

            return result;
        }

        protected override SimplePoint CalcNext(BinanceKline nextCandle)
        {
            _indicator += _coeff * ((double)nextCandle.Close - _indicator);

            return new SimplePoint(nextCandle.OpenTime, _indicator);
        }
    }
}
