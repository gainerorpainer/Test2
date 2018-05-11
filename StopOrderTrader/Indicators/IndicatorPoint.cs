using System;

namespace StopOrderTrader.Indicators
{
    public abstract class IndicatorPoint
    {
        protected IndicatorPoint(DateTime timestamp)
        {
            Timestamp = timestamp;
        }

        public DateTime Timestamp { get; set; }

    }

    public class SimplePoint : IndicatorPoint
    {
        public double Value { get; set; }

        public SimplePoint(DateTime timestamp, double value) : base(timestamp)
        {
            Value = value;
        }
    }
}
