using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Objects;

namespace StopOrderTrader.Indicators
{
    public class BollingerBand : AbstractIndicator<BollingerBand.BollingerPoint>
    {
        public class BollingerPoint : IndicatorPoint
        {
            public BollingerPoint(DateTime timestamp, double bottom, double average, double top) : base(timestamp)
            {
                Bottom = bottom;
                Average = average;
                Top = top;
            }

            public double Bottom { get; set; }
            public double Average { get; set; }
            public double Top { get; set; }
        }

        public int Period { get; private set; }
        public double Std { get; private set; }

        public override int MinimumInitValuesNecessary { get; }

        Queue<double> _buffer;

        public BollingerBand(int period, double std)
        {
            Period = period;
            Std = std;

            MinimumInitValuesNecessary = Period;
        }

        protected override IEnumerable<BollingerPoint> CalcInit(IList<BinanceKline> initialCandles)
        {
            var result = new BollingerPoint[initialCandles.Count - Period + 1];

            double _total_average = 0;
            double _total_squares = 0;

            for (int i = 0; i < initialCandles.Count; i++)
            {
                double item = (double)initialCandles[i].Close;
                _total_average += item;
                _total_squares += Math.Pow(item, 2);

                if (i > (Period - 2))
                {
                    double average = _total_average / Period;
                    double stdev = Math.Sqrt((_total_squares - Math.Pow(_total_average, 2) / Period) / Period);

                    int v = i - Period + 1;

                    result[v] = new BollingerPoint(initialCandles[i].OpenTime, average - Std * stdev, average, average + Std * stdev);

                    _total_average -= (double)initialCandles[v].Close;
                    _total_squares -= Math.Pow((double)initialCandles[v].Close, 2);
                }
            }

            _buffer = new Queue<double>(initialCandles.Skip(initialCandles.Count - Period).Take(Period).Select(x => (double)x.Close));

            return result;
        }

        protected override BollingerPoint CalcNext(BinanceKline nextCandle)
        {
            _buffer.Dequeue();
            _buffer.Enqueue((double)nextCandle.Close);

            double _total_average = _buffer.Sum();
            double _total_squares = _buffer.Sum(x => Math.Pow(x, 2));

            double average = _total_average / Period;
            double stdev = Math.Sqrt((_total_squares - Math.Pow(_total_average, 2) / Period) / Period);

            var result = new BollingerPoint(nextCandle.OpenTime, average - Std * stdev, average, average + Std * stdev);
            return result;
        }
    }
}
