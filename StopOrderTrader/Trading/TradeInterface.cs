using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Objects;

namespace StopOrderTrader.Trading
{

    static class TradeInterface
    {
        [Serializable]
        public class TradingException : Exception
        {
            public TradingException(string message) : base(message) { }
        }

        public class FilterContainer
        {
            public FilterContainer(BinanceSymbolPriceFilter priceFilter, BinanceSymbolLotSizeFilter quantityFilter)
            {
                PriceFilter = priceFilter;
                QuantityFilter = quantityFilter;
            }

            public BinanceSymbolPriceFilter PriceFilter { get; }

            public BinanceSymbolLotSizeFilter QuantityFilter { get; }
        }

        public static BinanceClient Client { get; private set; }

        public static Dictionary<string, FilterContainer> Filters;

        private static decimal ComplyPriceRule(decimal price, string symbol)
        {
            decimal unit = Filters[symbol].PriceFilter.TickSize;
            return Truncate(price, unit);
        }

        /// <summary>
        /// Complies the quantity to minimum Qu
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        private static decimal ComplyQuantityRule(decimal amount, string symbol)
        {
            decimal minQuantity = Filters[symbol].QuantityFilter.MinQuantity;

            if (amount < minQuantity)
                throw new TradingException($"The quantity ({amount}) is less than the min trade quantity ({minQuantity.Normalize()}).");

            decimal unit = Filters[symbol].QuantityFilter.StepSize;
            decimal trunctated = Truncate(amount, unit);

            if (trunctated < minQuantity)
                throw new TradingException($"After truncation ({amount} --> {trunctated}), the quantity is less than the min trade quantity ({minQuantity.Normalize()}).");

            return trunctated;
        }

        /// <summary>
        /// Removes as many digits from the 'number' such that it is an integer multiple of 'unit. E.g number '1.123' truncated with unit '0.01' will result in '1.12'
        /// </summary>
        /// <param name="number">Number to truncate</param>
        /// <param name="unit">Minimal unit after truncation</param>
        /// <returns></returns>
        private static decimal Truncate(decimal number, decimal unit) => (number - decimal.Remainder(number, unit)).Normalize();


        /// <summary>
        /// Initializes the Binance Client with Login data from the Secrets lib, syncs the client with the server and downloads trading rules
        /// </summary>
        public static void Load()
        {
            Client = new BinanceClient(new BinanceClientOptions()
            {
                ApiCredentials = new CryptoExchange.Net.Authentication.ApiCredentials(
                    Lib.Encryption.ToInsecureString(Lib.Secrets.APIKey.Value), Lib.Encryption.ToInsecureString(Lib.Secrets.APISecret.Value)),
                AutoTimestamp = true,
                TradeRulesBehaviour = TradeRulesBehaviour.AutoComply,
                TradeRulesUpdateInterval = TimeSpan.FromMinutes(5)
            });

            Client.GetServerTime(true);

            Filters = Client.GetExchangeInfo().GetOrThrow().Symbols.ToDictionary(x => x.Name, x => new FilterContainer(x.Filters.OfType<BinanceSymbolPriceFilter>().Single(), x.Filters.OfType<BinanceSymbolLotSizeFilter>().Single()));
        }

        /// <summary>
        /// Places a buy/sell order (Market price). Warning, the price can be severy unfavorable if timed badly
        /// </summary>
        /// <param name="symbol">Symbol to trade</param>
        /// <param name="quantity">Quantity to trade</param>
        /// <param name="orderSide">Buy/Sell</param>
        /// <returns></returns>
        public static ClientServerOrder PlaceImmediateOrder(string symbol, decimal quantity, OrderSide orderSide)
        {
            quantity = ComplyQuantityRule(quantity, symbol);

            var order = Client.PlaceOrder(symbol, orderSide,
                type: OrderType.Market,
                quantity: quantity).GetOrThrow();

            // Try to donwload the actual orders (if this fails to often, cancel)
            List<BinanceTrade> order2;
            int i = 0;
            const int retries = 3;
            do
            {
                i++;
                order2 = GetActualTrades(symbol, order.OrderId);
            } while ((order2 == null) && (i < retries));


            // It is not crutial to have downloaded the actual trades, they can be downloaded later as well
            return new ClientServerOrder() { ClientOrderId = order.ClientOrderId, FilledOrders = order2?.Select(x => x.OrderId).ToList() };
        }



        /// <summary>
        /// Woll place on order that tries to limit "Loss", meaning that it will buy if the price is higher or sell if the price is lower
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="quantity"></param>
        /// <param name="price"></param>
        /// <param name="orderSide"></param>
        /// <returns></returns>
        public static BinancePlacedOrder PlaceStopLossOrder(string symbol, decimal quantity, decimal price, OrderSide orderSide)
        {
            price = ComplyPriceRule(price, symbol);

            return Client.PlaceOrder(symbol, orderSide,
                type: OrderType.StopLossLimit,
                timeInForce: TimeInForce.GoodTillCancel,
                quantity: quantity,
                stopPrice: price, price: price).GetOrThrow();
        }

        /// <summary>
        /// Will place an order that tries to maximize "Profit", meaning that it will try to buy if the price is lower or sell if the price is higher than the limit
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="quantity"></param>
        /// <param name="limit"></param>
        /// <param name="orderSide"></param>
        /// <returns></returns>
        public static BinancePlacedOrder PlaceTakeProfitOrder(string symbol, decimal quantity, decimal limit, OrderSide orderSide)
        {
            limit = ComplyPriceRule(limit, symbol);

            return Client.PlaceOrder(symbol, orderSide,
                type: OrderType.TakeProfitLimit,
                timeInForce: TimeInForce.GoodTillCancel,
                quantity: quantity,
                stopPrice: limit, price: limit).GetOrThrow();
        }


        /// <summary>
        /// Gets a placed order from the server based on the ClientOrderId
        /// </summary>
        /// <param name="symbol">Symbol in which to search</param>
        /// <param name="id">ClientOrderid to look for (must not be null / empty)</param>
        /// <returns></returns>
        public static BinanceOrder GetOrderById(string symbol, string id) => Client.QueryOrder(symbol, origClientOrderId: id).GetOrThrow();

        /// <summary>
        /// Gets the corresponding trades for a placed order from the server
        /// </summary>
        /// <param name="symbol">Symbol in which to search</param>
        /// <param name="id">OrderId to look for</param>
        /// <returns></returns>
        public static List<BinanceTrade> GetActualOrders(string symbol, string id) => GetActualTrades(symbol, GetOrderById(symbol, id).OrderId);
        public static List<BinanceTrade> GetActualTrades(string symbol, long id) => Client.GetMyTrades(symbol).GetOrThrow().Where(x => x.OrderId == id).ToList();
    }
}
