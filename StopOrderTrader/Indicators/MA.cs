using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Objects;

namespace StopOrderTrader.Indicators
{
    public class MA : AbstractIndicator<SimplePoint>
    {

        public int Period { get; private set; }

        public override int MinimumInitValuesNecessary { get; }

        private Queue<double> _buffer;

        public MA(int periode)
        {
            Period = periode;
            MinimumInitValuesNecessary = Period;
        }

        protected override IEnumerable<SimplePoint> CalcInit(IList<BinanceKline> input)
        {
            List<SimplePoint> result = new List<SimplePoint>(input.Count - Period + 1);


            for (int i = (Period - 1); i < input.Count; i++)
            {
                double _sum = input.Skip(i - (Period -1)).Take(Period).Select(x => (double)x.Close).Sum();
                result.Add(new SimplePoint(input[i].OpenTime, _sum / Period));
            }

            _buffer = new Queue<double>(input.Skip(input.Count - Period).Select(x => (double)x.Close));

            return result;
        }

        protected override SimplePoint CalcNext(BinanceKline candle)
        {

            _buffer.Dequeue();
            _buffer.Enqueue((double)candle.Close);

            SimplePoint nextPoint = new SimplePoint(candle.OpenTime, _buffer.Sum() / Period);

            return nextPoint;
        }
    }
}
