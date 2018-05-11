using System;
using System.Collections.Generic;
using System.Linq;
using GainGainGain.Markets;

namespace GainGainGain.Indicators
{
    public class Macd : AbstractIndicator<Macd.MacdPoint>
    {
        int _periodShort;
        int _periodLong;
        int _periodSignal;

        Ema _shortEMA;
        Ema _longEMA;
        Ema _signalEMA;

        public Macd(int shortPeriod, int longPeriod, int signalPeriod)
        {
            _periodShort = shortPeriod;
            _periodLong = longPeriod;
            _periodSignal = signalPeriod;

            MinimumInitValuesNecessary = longPeriod + signalPeriod - 1;
        }

        public class MacdPoint : IndicatorPoint
        {
            public MacdPoint(DateTime timestamp, double mACD, double signal) : base(timestamp)
            {
                MACD = mACD;
                Signal = signal;
            }

            public double MACD { get; private set; }

            public double Signal { get; private set; }
        }

        public override int MinimumInitValuesNecessary { get; }

        protected override IEnumerable<MacdPoint> CalcInit(IList<Candle> initialCandles)
        {
            _shortEMA = new Ema(_periodShort);
            _longEMA = new Ema(_periodLong);

            // this will init both emas such that ".Points" contains exactly signal length amount of points
            int takeShort = (_periodSignal + _shortEMA.MinimumInitValuesNecessary - 1);
            int takeLong = (_periodSignal + _longEMA.MinimumInitValuesNecessary - 1);
            int elementsToLeaveOpen = initialCandles.Count - MinimumInitValuesNecessary;

            _shortEMA.Init(initialCandles.TakeLast(takeShort, skipLast: elementsToLeaveOpen));
            _longEMA.Init(initialCandles.TakeLast(takeLong, skipLast: elementsToLeaveOpen));

            // init macdlist by piecewise subtraction
            List<Candle> macdCandles = new List<Candle>();
            for (int i = 0; i < _periodSignal; i++)
            {
                macdCandles.Add(new Candle(0m, 0m, 0m, (decimal)(_shortEMA.Points[i].Value - _longEMA.Points[i].Value), initialCandles[i + _periodLong - 1].OpenTime, initialCandles[i + _periodLong - 1].CloseTime));
            }

            // init signal ema
            _signalEMA = new Ema(_periodSignal);
            _signalEMA.Init(macdCandles);

            // First signal is different
            List<MacdPoint> result = new List<MacdPoint>
            {
                new MacdPoint( _signalEMA.Points.Last().Timestamp, (double)macdCandles.Last().Close, _signalEMA.Points.Last().Value)
            };

            // ... than the following signals
            int offset = MinimumInitValuesNecessary;
            for (int i = offset; i < initialCandles.Count; i++)
            {
                result.Add(CalcNext(initialCandles[i]));
            }

            return result;
        }

        protected override MacdPoint CalcNext(Candle nextCandle)
        {
            // advance emas
            _shortEMA.Next(nextCandle);
            _longEMA.Next(nextCandle);

            // macd
            double macd = _shortEMA.Points.Last().Value - _longEMA.Points.Last().Value;

            // signal
            _signalEMA.Next(new Candle(0m, 0m, 0m, (decimal)macd, nextCandle.OpenTime, nextCandle.CloseTime));

            return new MacdPoint(nextCandle.OpenTime, macd, _signalEMA.Points.Last().Value);
        }
    }
}
