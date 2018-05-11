using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StopOrderTrader.Lib
{
    static class Algorithm
    {
        public class LinearRegression
        {
            public LinearRegression(double m, double b, double r)
            {
                M = m;
                B = b;
                R = r;
            }

            /// <summary>
            /// f(x) = M*x + B;
            /// </summary>
            public double M { get; }

            /// <summary>
            /// f(x) = M*x + B
            /// </summary>
            public double B { get; }

            /// <summary>
            /// Fitting factor 0..1, where 1 is best fit
            /// </summary>
            public double R { get; }
        }

        public static LinearRegression GetLinearRegression(List<Point> points)
        {
            double sumOfX = 0;
            double sumOfY = 0;
            double sumOfXSq = 0;
            double sumOfYSq = 0;
            double ssX = 0;
            double ssY = 0;
            double sumCodeviates = 0;
            double sCo = 0;
            double count = points.Count;

            for (int ctr = 0; ctr < count; ctr++)
            {
                double x = points[ctr].X;
                double y = points[ctr].Y;
                sumCodeviates += x * y;
                sumOfX += x;
                sumOfY += y;
                sumOfXSq += x * x;
                sumOfYSq += y * y;
            }
            ssX = sumOfXSq - ((sumOfX * sumOfX) / count);
            ssY = sumOfYSq - ((sumOfY * sumOfY) / count);
            double RNumerator = (count * sumCodeviates) - (sumOfX * sumOfY);
            double RDenom = (count * sumOfXSq - (sumOfX * sumOfX))
             * (count * sumOfYSq - (sumOfY * sumOfY));
            sCo = sumCodeviates - ((sumOfX * sumOfY) / count);

            double meanX = sumOfX / count;
            double meanY = sumOfY / count;
            double dblR = RNumerator / Math.Sqrt(RDenom);
            double rsquared = dblR * dblR;
            double yintercept = meanY - ((sCo / ssX) * meanX);
            double slope = sCo / ssX;

            string txt = "";
            for (int i = 0; i < count; i++)
            {
                txt += points[i].X + "\t" + points[i].Y.ToString().Replace(",", ".") + Environment.NewLine; 
            }

            return new Algorithm.LinearRegression(slope, yintercept, rsquared);
        }
    }
}
