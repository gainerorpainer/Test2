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

        /// <summary>
        /// Either throws an exception if the request was faulty or returns the data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callResult"></param>
        /// <returns></returns>
        public static T GetOrThrow<T>(this CryptoExchange.Net.CallResult<T> callResult)
        {
            if (callResult.Success == false)
                throw new Exception($"Could not get result: \"{callResult.Error.Message} ({callResult.Error.Code})\"");
            else return callResult.Data;
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
        /// Displays a generic error message
        /// </summary>
        /// <param name="title"></param>
        /// <param name="text"></param>
        public static void Error(string title, string text)
        {
            MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error);
            // System.Windows.Application.Current.Shutdown(-1);
        }

        /// <summary>
        /// Deserializes a class that has been marked as serializable into a file
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filepath">Relative or absolute path to the file</param>
        /// <returns></returns>
        public static T DeserializeXml<T>(string filepath)
        {
            using (var fs = System.IO.File.OpenRead(filepath))
                return (T)new System.Xml.Serialization.XmlSerializer(typeof(T)).Deserialize(fs);
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
        public static void CallWithWaitWindow(this Window window, Action action)
        {
            window.IsEnabled = false;
            var hdnl = new WaitWindowHandle(window);

            void Reenable()
            {
                hdnl.Close();
                window.IsEnabled = true;
            }

            // Detach heavy action so that we can redraw the screen
            window.Dispatcher.RefreshThenContinue(action).ContinueWith(x => window.Dispatcher.Invoke(Reenable));
        }

        /// <summary>
        /// Sychronizes the collections such that observable collections notices the change.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="c1"></param>
        /// <param name="c2">Will overwrite the observable collection with this collection</param>
        public static void SyncWith<T>(this System.Collections.ObjectModel.ObservableCollection<T> c1, ICollection<T> c2)
        {
            c1.Clear();
            foreach (var item in c2)
                c1.Add(item);
        }

        /// <summary>
        /// Calculates the variance for a list of values
        /// </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        public static double Variance(this List<double> col)
        {
            double average = col.Average();
            double sumOfSquaresOfDifferences = col.Select(val => (val - average) * (val - average)).Sum();
            return Math.Sqrt(sumOfSquaresOfDifferences / col.Count());
        }

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

        public static List<T> GetLast<T>(this ICollection<T> collection, int howMany)
        {
            return collection.Skip(collection.Count - howMany).Take(howMany).ToList();
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
