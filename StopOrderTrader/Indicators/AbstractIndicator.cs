using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Objects;

namespace StopOrderTrader.Indicators
{
    public abstract class AbstractIndicator<T> where T : IndicatorPoint
    {
        /// <summary>
        /// Ring buffered list of always the last n points of the indicator
        /// </summary>
        public List<T> Points { get; private set; }

        public abstract int MinimumInitValuesNecessary { get; }

        /// <summary>
        /// Initializes the  class with indicator points
        /// </summary>
        /// <param name="initialCandles">The initial candles to init with</param>
        public void Init(IList<BinanceKline> initialCandles)
        {
           Points = CalcInit(initialCandles).ToList();
        }

        /// <summary>
        /// Calculates the first points
        /// </summary>
        /// <param name="initialCandles"></param>
        /// <returns></returns>
        protected abstract IEnumerable<T> CalcInit(IList<BinanceKline> initialCandles);

        /// <summary>
        /// Calculats the next value of the indicator
        /// </summary>
        /// <param name="nextCandle">The next candle</param>
        /// <returns>The resulting indicator point</returns>
        protected abstract T CalcNext(BinanceKline nextCandle);

        /// <summary>
        /// Calculates the indicator for the next n values, updates the ".Points" property
        /// </summary>
        /// <param name="nextCandles"></param>
        public void NextPoints(IEnumerable<BinanceKline> nextCandles)
        {
            foreach (var candle in nextCandles)
                Next(candle);
        }

        /// <summary>
        /// Calculates the next indicator point, updates the ".Points" property
        /// </summary>
        /// <param name="nextCandle"></param>
        /// <returns>The resulting next indicator point</returns>
        public T Next(BinanceKline nextCandle)
        {
            var next = CalcNext(nextCandle);

            // Push the ring buffer by one
            Points.Add(next);
            Points.RemoveAt(0);

            return next;
        }
    }
}
