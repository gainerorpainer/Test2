using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Objects;

namespace StopOrderTrader.Indicators
{
    public class DMI : AbstractIndicator<DMI.DMIPoint>
    {

        public class DMIPoint : IndicatorPoint
        {
            public DMIPoint(DateTime timestamp, double dmiPlus, double dmiMinus, double dmi) : base(timestamp)
            {
                DmiPlus = dmiPlus;
                DmiMinus = dmiMinus;
                Dmi = dmi;
            }

            public double DmiPlus { get; set; }
            public double DmiMinus { get; set; }
            public double Dmi { get; set; }
        }

        private DMI() { }

        List<double> TRs;
        List<double> DM1Plus;
        List<double> DM1Minus;
        List<double> TR_period;
        List<double> DMPlus_period;
        List<double> DMMinus_period;
        List<double> DPlus_period;
        List<double> DMinus_period;
        List<double> DX_period;
        List<double> ADX_period;

        public int Period { get; private set; }

        public override int MinimumInitValuesNecessary { get; }

        private BinanceKline _lastCandle;

        public DMI(int period)
        {
            Period = period;
            MinimumInitValuesNecessary = 2 * period;
        }


        protected override IEnumerable<DMIPoint> CalcInit(IList<BinanceKline> initialCandles)
        {
            List<DMIPoint> result = new List<DMIPoint>();

            TRs = new List<double>(Period * 2);
            DM1Plus = new List<double>(Period * 2);
            DM1Minus = new List<double>(Period * 2);
            TR_period = new List<double>(Period * 2);
            DMPlus_period = new List<double>(Period);
            DMMinus_period = new List<double>(Period);
            DPlus_period = new List<double>(Period);
            DMinus_period = new List<double>(Period);
            DX_period = new List<double>(Period);
            ADX_period = new List<double>(Period);

            for (int i = 1; i < initialCandles.Count; i++)
            {
                // true range
                TRs.Add((double)Math.Max(
                    initialCandles[i].High - initialCandles[i].Low,
                    Math.Max(
                        Math.Abs(initialCandles[i].High - initialCandles[i - 1].Close),
                        Math.Abs(initialCandles[i].Low - initialCandles[i - 1].Close))));

                // +DM1
                DM1Plus.Add((double)((initialCandles[i].High - initialCandles[i - 1].High) > initialCandles[i - 1].Low - initialCandles[i].Low ?
                    Math.Max(initialCandles[i].High - initialCandles[i - 1].High, 0)
                    : 0));

                // -DM1
                DM1Minus.Add((double)((initialCandles[i - 1].Low - initialCandles[i].Low) > initialCandles[i].High - initialCandles[i - 1].High ?
                    Math.Max(initialCandles[i - 1].Low - initialCandles[i].Low, 0)
                    : 0));


                if (TRs.Count >= Period)
                {
                    // Summs
                    double trperiod = TRs.Skip(TR_period.Count).Take(Period).Sum();
                    TR_period.Add(trperiod);
                    double dmplusperiod = DM1Plus.Skip(DMPlus_period.Count).Take(Period).Sum();
                    DMPlus_period.Add(dmplusperiod);
                    double dmminusperiod = DM1Minus.Skip(DMMinus_period.Count).Take(Period).Sum();
                    DMMinus_period.Add(dmminusperiod);

                    // D+ and D-
                    double dplusperiod = 100.0 * dmplusperiod / trperiod;
                    DPlus_period.Add(dplusperiod);
                    double dminusperiod = 100.0 * dmminusperiod / trperiod;
                    DMinus_period.Add(dminusperiod);

                    // Diff & sum
                    double ddiffperiod = Math.Abs(dplusperiod - dminusperiod);
                    double dsumperiod = dplusperiod + dminusperiod;

                    // Fraction
                    DX_period.Add(100.0 * ddiffperiod / dsumperiod);
                }


                if (DX_period.Count >= Period)
                {
                    double adxperiod = DX_period.Skip(ADX_period.Count).Take(Period).Average();
                    ADX_period.Add(adxperiod);
                    result.Add(new DMIPoint(initialCandles[i].OpenTime, DPlus_period.Last(), DMinus_period.Last(), adxperiod));
                }
            }

            _lastCandle = initialCandles.Last();

            return result;
        }

        protected override DMIPoint CalcNext(BinanceKline nextCandle)
        {
            // "Pop" the first value of each list to keep memory small
            TRs.RemoveAt(0);
            DM1Plus.RemoveAt(0);
            DM1Minus.RemoveAt(0);
            TR_period.RemoveAt(0);
            DMPlus_period.RemoveAt(0);
            DMMinus_period.RemoveAt(0);
            DPlus_period.RemoveAt(0);
            DMinus_period.RemoveAt(0);
            DX_period.RemoveAt(0);
            ADX_period.RemoveAt(0);

            // true range
            TRs.Add((double)Math.Max(
                nextCandle.High - nextCandle.Low,
                Math.Max(
                    Math.Abs(nextCandle.High - _lastCandle.Close),
                    Math.Abs(nextCandle.Low - _lastCandle.Close))));

            // +DM1
            DM1Plus.Add((double)((nextCandle.High - _lastCandle.High) > _lastCandle.Low - nextCandle.Low ?
                Math.Max(nextCandle.High - _lastCandle.High, 0)
                : 0));

            // -DM1
            DM1Minus.Add((double)((_lastCandle.Low - nextCandle.Low) > nextCandle.High - _lastCandle.High ?
                Math.Max(_lastCandle.Low - nextCandle.Low, 0)
                : 0));

            // Summs
            double trperiod = TRs.Skip(TR_period.Count).Take(Period).Sum();
            TR_period.Add(trperiod);
            double dmplusperiod = DM1Plus.Skip(DMPlus_period.Count).Take(Period).Sum();
            DMPlus_period.Add(dmplusperiod);
            double dmminusperiod = DM1Minus.Skip(DMMinus_period.Count).Take(Period).Sum();
            DMMinus_period.Add(dmminusperiod);

            // D+ and D-
            double dplusperiod = 100.0 * dmplusperiod / trperiod;
            DPlus_period.Add(dplusperiod);
            double dminusperiod = 100.0 * dmminusperiod / trperiod;
            DMinus_period.Add(dminusperiod);

            // Diff & sum
            double ddiffperiod = Math.Abs(dplusperiod - dminusperiod);
            double dsumperiod = dplusperiod + dminusperiod;

            // Fraction
            DX_period.Add(100.0 * ddiffperiod / dsumperiod);

            // Adx
            double adxperiod = DX_period.Skip(ADX_period.Count).Take(Period).Average();
            ADX_period.Add(adxperiod);

            _lastCandle = nextCandle;

            return new DMIPoint(nextCandle.OpenTime, dplusperiod, dminusperiod, adxperiod);
        }
    }
}

