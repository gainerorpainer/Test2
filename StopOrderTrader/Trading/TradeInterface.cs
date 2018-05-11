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
        public static BinanceClient Client { get; }

        private static decimal ComplyTradingRule(decimal d, string symbol)
        {
            decimal priceUnit = Client.GetExchangeInfo().GetOrThrow().Symbols.First(x => x.Name == symbol).Filters.OfType<BinanceSymbolPriceFilter>().FirstOrDefault().TickSize;
            return (d - decimal.Remainder(d, priceUnit)) / 1.000000000000000000000000000000000m;
        }

        static TradeInterface()
        {
            Client = new BinanceClient(new BinanceClientOptions()
            {
                ApiCredentials = new CryptoExchange.Net.Authentication.ApiCredentials(
                    Lib.Encryption.ToInsecureString(Lib.Secrets.APIKey.Value), Lib.Encryption.ToInsecureString(Lib.Secrets.APISecret.Value)),
                AutoTimestamp = true,
                TradeRulesBehaviour = TradeRulesBehaviour.AutoComply,
                TradeRulesUpdateInterval = TimeSpan.FromMinutes(5)
            });
        }

        public static ClientServerOrder PlaceImmediateOrder(string symbol, decimal quantity, OrderSide orderSide)
        {
            var order = Client.PlaceOrder(symbol, orderSide,
                type: OrderType.Market,
                quantity: quantity).GetOrThrow();

            BinanceTrade order2;
            int i = 0;
            const int retries = 3;
            do
            {
                i++;
                order2 = GetActualOrder(symbol, order.OrderId);
            } while ((order2 == null) && (i < retries));
            return new ClientServerOrder() { ClientOrderId = order.ClientOrderId, ActualOrderId = order2?.OrderId };
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
            price = ComplyTradingRule(price, symbol);

            return Client.PlaceOrder(symbol, orderSide,
                type: OrderType.StopLossLimit,
                timeInForce: TimeInForce.GoodTillCancel,
                quantity: quantity,
                stopPrice: price, price: price).GetOrThrow();
        }

        /// <summary>
        /// Will place an order that tries to maximize "Profit", meaning that it will try to buy if the price is lower or sell if the price is higher
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="quantity"></param>
        /// <param name="price"></param>
        /// <param name="orderSide"></param>
        /// <returns></returns>
        public static BinancePlacedOrder PlaceProfitOrder(string symbol, decimal quantity, decimal price, OrderSide orderSide)
        {
            price = ComplyTradingRule(price, symbol);

            return Client.PlaceOrder(symbol, orderSide,
                type: OrderType.TakeProfitLimit,
                timeInForce: TimeInForce.GoodTillCancel,
                quantity: quantity,
                stopPrice: price, price: price).GetOrThrow();
        }


        public static BinanceOrder GetOrderById(string symbol, string id) => Client.QueryOrder(symbol, origClientOrderId: id).GetOrThrow();
        public static BinanceTrade GetActualOrder(string symbol, long id)
        {
            return Client.GetMyTrades(symbol).GetOrThrow().FirstOrDefault(x => x.OrderId == id);
        }
    }
}
