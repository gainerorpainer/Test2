using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace StopOrderTrader
{
    static class Toolbox
    {
        /// <summary>
        /// Get the last n characters of a string. If there are not enough characters in the string, the whole string is returned
        /// </summary>
        /// <param name="source"></param>
        /// <param name="tail_length">How many elements to take from the end</param>
        /// <returns></returns>
        public static string GetLast(this string source, int tail_length)
        {
            if (tail_length >= source.Length)
                return source;
            return source.Substring(source.Length - tail_length);
        }

        /// <summary>
        /// Removes the last n characters from a string and returns the rest
        /// </summary>
        /// <param name="source"></param>
        /// <param name="length">How many characters to remove from the end</param>
        /// <returns></returns>
        public static string RemoveLast(this string source, int length)
        {
            return source.Substring(0, source.Length - length);
        }


        [Serializable]
        public class BinanceAPIException : Exception
        {
            public BinanceAPIException(string message, int code) : base(message) { Code = code; }

            public int Code { get; }
        }

        /// <summary>
        /// Either throws an exception if the request was faulty or returns the data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callResult"></param>
        /// <returns></returns>
        public static T GetOrThrow<T>(this CryptoExchange.Net.CallResult<T> callResult)
        {
            if (callResult.Success == false)
            {
                var errorObj = Trading.BinanceErrorJSON.FromJson(callResult.Error.Message.Replace("Server error: ", ""));
                throw new BinanceAPIException($"Could not get result: \"{errorObj.Msg} ({errorObj.Code})\"", errorObj.Code);
            }
            else return callResult.Data;
        }

        /// <summary>
        /// Either gets data or displays the exception in a popup
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callResult"></param>
        /// <returns></returns>
        public static T GetOrDisplayError<T>(this CryptoExchange.Net.CallResult<T> callResult)
        {
            try
            {
                return callResult.GetOrThrow();
            }
            catch (BinanceAPIException ex)
            {
                InfoPopup("Error", ex.Message, MessageBoxImage.Error);
                return default(T);
            }
        }

        /// <summary>
        /// Returns whether a request succeded
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callResult"></param>
        /// <returns></returns>
        public static bool Fails<T>(this CryptoExchange.Net.CallResult<T> callResult)
        {
            return !callResult.Success;
        }

        /// <summary>
        /// Displays a generic popup message
        /// </summary>
        /// <param name="title"></param>
        /// <param name="text"></param>
        public static void InfoPopup(string title, string text, MessageBoxImage image)
        {
            MessageBox.Show(text, title, MessageBoxButton.OK, image);
        }

        public static bool ConfirmPopup(string title, string question)
        {
            return MessageBoxResult.OK == MessageBox.Show(question, title, MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Will call this function later to give the window some time to refresh
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <param name="action">Which action to call after refresh</param>
        /// <returns></returns>
        public static Task RefreshThenContinue(this Dispatcher dispatcher, Action action)
        {
            return Task.Delay(1).ContinueWith(x => dispatcher.Invoke(action));
        }

        /// <summary>
        /// Use this to call a heavy function while displaying the loading symbol
        /// </summary>
        /// <param name="window"></param>
        /// <param name="action">Which Action to call</param>
        public static Task CallWithWaitWindow(this Window window, Action action)
        {
            window.IsEnabled = false;
            var hdnl = new WaitWindowHandle(window);

            // Detach heavy action so that we can redraw the screen
            return window.Dispatcher.RefreshThenContinue(action).ContinueWith(x =>
            {
                // Can happen that the window has been closed early (exceptions etc), so only reenable if window is open
                if (window.IsVisible)
                    window.Dispatcher.Invoke(() =>
                    {
                        hdnl.Close();
                        window.IsEnabled = true;
                    });
                else
                    hdnl.Close();
            });
        }

        /// <summary>
        /// Sychronizes the collections such that observable collections notices the change.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="c1"></param>
        /// <param name="c2">Will overwrite the observable collection with this collection</param>
        public static void SyncWith<T>(this System.Collections.ObjectModel.ObservableCollection<T> c1, IEnumerable<T> c2)
        {
            c1.Clear();
            foreach (var item in c2)
                c1.Add(item);
        }

        /// <summary>
        /// Removes unnecessary trailing zeros from a decimal
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static decimal Normalize(this decimal d) => d / 1.000000000000000000000000000000000m;

        /// <summary>
        /// Sorts a DataGrid according to a column name an a direction
        /// </summary>
        /// <param name="datagrid"></param>
        /// <param name="columnName"></param>
        /// <param name="direction"></param>
        public static void SortBy(this DataGrid datagrid, string columnName, System.ComponentModel.ListSortDirection direction)
        {
            datagrid.Items.SortDescriptions.Clear();
            datagrid.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription(columnName, direction));
            foreach (var item in datagrid.Columns)
                if ((string)item.Header == columnName)
                    item.SortDirection = direction;
                else
                    item.SortDirection = null;
        }

        /// <summary>
        /// Returns the last n elements from a list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="howMany"></param>
        /// <returns></returns>
        public static List<T> TakeLast<T>(this ICollection<T> collection, int howMany)
        {
            return collection.Skip(collection.Count - howMany).Take(howMany).ToList();
        }

        /// <summary>
        /// Calculates the average for a list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="records"></param>
        /// <param name="value">Choose the value to average over</param>
        /// <param name="weight">Choose the weight that each element has</param>
        /// <param name="nullIfEmpty">If false, will return '0' rather than null if the collection is empty</param>
        /// <returns></returns>
        public static decimal? WeightedAverage<T>(this IEnumerable<T> records, Func<T, decimal> value, Func<T, decimal> weight, bool nullIfEmpty = false)
        {
            decimal weightedValueSum = records.Sum(x => value(x) * weight(x));
            decimal weightSum = records.Sum(x => weight(x));

            if (weightSum != 0)
                return weightedValueSum / weightSum;
            else if (nullIfEmpty)
                return null;
            else
                throw new DivideByZeroException("Cannot calculate weighted avg if all weights are 0");
        }

        /// <summary>
        /// Calculates the effective price of a splitted transaction. In particular, calcs the weighted average of all prices weighted by their quantity
        /// </summary>
        /// <param name="binanceTrades"></param>
        /// <returns></returns>
        public static decimal EffectivePrice(this IEnumerable<Binance.Net.Objects.BinanceTrade> binanceTrades)
        {
            return binanceTrades.WeightedAverage(x => x.Price, x => x.Quantity).Value;
        }


        public static string RandomString(int length)
        {
            Random rng = new Random();
            StringBuilder stringBuilder = new StringBuilder(new string('\0', length));

            for (int i = 0; i < length; i++)
            {
                double rand = rng.NextDouble();
                if (rand > (2.0 / 3.0))
                    // alpha
                    stringBuilder[i] = (char)rng.Next(48, 58);
                else if (rand > (1.0 / 3.0))
                    // upper
                    stringBuilder[i] = (char)rng.Next(65, 91);
                else
                    // lower
                    stringBuilder[i] = (char)rng.Next(97, 123);
            }

            return stringBuilder.ToString();
        }

        public class WaitWindowHandle
        {
            WaitWindow w;
            Thread thread;

            public WaitWindowHandle(Window window)
            {
                Point centerParent = new Point(window.Left + window.ActualWidth / 2, window.Top + window.ActualHeight / 2);

                thread = new Thread(() =>
                {
                    w = new WaitWindow();
                    w.Left = centerParent.X - w.Width / 2;
                    w.Top = centerParent.Y - w.Height / 2;
                    w.Show();

                    w.Closed += (sender2, e2) =>
                        w.Dispatcher.InvokeShutdown();

                    Dispatcher.Run();
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }

            public void Close()
            {
                if (w.Dispatcher.CheckAccess())
                    w.Close();
                else
                    w.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(w.Close));

                thread.Join();
            }
        }
    }
}
