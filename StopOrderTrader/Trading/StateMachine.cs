using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Objects;

namespace StopOrderTrader.Trading
{
    static class StateMachine
    {
        private const bool PAYFEESINBNB = true;
        private const decimal FEEMODIFIER = PAYFEESINBNB ? 0.999m : 0.998m;

        public static void Run(IEnumerable<Deal> deals)
        {
            foreach (var deal in deals.Where(x => x.CurrentState != Deal.State.Done))
                Process(deal);
        }

        private static void Process(Deal deal)
        {
            switch (deal.CurrentState)
            {
                case Deal.State.Done:
                    return;

                case Deal.State.WaitForBuy:
                    WaitForBuy(deal);
                    break;

                case Deal.State.WaitForGoal1:
                    WaitForGoal1(deal);
                    break;

                case Deal.State.WaitForGoal2:
                    WaitForGoal2(deal);
                    break;
            }

            // Check panic sell condition
            decimal price = TradeInterface.Client.GetPrice(deal.Symbol).GetOrThrow().Price.Normalize();
            decimal panicSellPrice = (deal.BuyPrice * (1 - deal.SellStopLossPerc)).Normalize();
            if (price < panicSellPrice)
            {
                Toolbox.InfoPopup("Panic sell!", $"Your sell condition was met: (price = {price}) < ({panicSellPrice} = price * {1 - deal.SellStopLossPerc:P2}", System.Windows.MessageBoxImage.Exclamation);
            }
        }

        private static void WaitForGoal2(Deal deal)
        {
            var sellOrder = TradeInterface.GetOrderById(deal.Symbol, deal.Goal2SellOrder.ClientOrderId);
            if (OrderWorked(sellOrder))
            {
                // Download actual order
                var actualOrders = TradeInterface.GetActualTrades(sellOrder.Symbol, sellOrder.OrderId);
                deal.Goal2SellOrder.FilledOrders = actualOrders.Select(x => x.OrderId).ToList();

                deal.Leftovers -= actualOrders.Select(x => x.Quantity).Sum();

                // Set next state
                deal.CurrentState = Deal.State.Done;
                deal.CurrentResult = Deal.Result.GoalsArchived;
            }
            else if (OrderCancelled(sellOrder))
                StateTransistion_Cancelled(deal);
        }

        private static void WaitForGoal1(Deal deal)
        {
            var sellOrder = TradeInterface.GetOrderById(deal.Symbol, deal.Goal1SellOrder.ClientOrderId);
            if (OrderWorked(sellOrder))
            {
                // Download actual order
                var actualOrders = TradeInterface.GetActualTrades(sellOrder.Symbol, sellOrder.OrderId);
                deal.Goal1SellOrder.FilledOrders = actualOrders.Select(x => x.OrderId).ToList();

                deal.Leftovers -= actualOrders.Select(x => x.Quantity).Sum();

                // Set next state
                deal.CurrentState = Deal.State.WaitForGoal2;

                // Check 2nd goal immediately
                WaitForGoal2(deal);
            }
            else if (OrderCancelled(sellOrder))
                StateTransistion_Cancelled(deal);
        }

        private static void WaitForBuy(Deal deal)
        {
            var buyOrder = TradeInterface.GetOrderById(deal.Symbol, deal.BuyOrder.ClientOrderId);
            if (OrderWorked(buyOrder))
            {
                // Download actual order
                var actualOrders = TradeInterface.GetActualTrades(buyOrder.Symbol, buyOrder.OrderId);
                deal.BuyOrder.FilledOrders = actualOrders.Select(x => x.OrderId).ToList();

                decimal quantity = (actualOrders.Sum(x => x.Quantity) * FEEMODIFIER).Normalize();

                // Set leftovers
                deal.Leftovers = quantity;

                // Calculate Sell points
                decimal price = actualOrders.EffectivePrice();

                // We put 50 % sell on 1 % profit 
                var order1 = TradeInterface.PlaceTakeProfitOrder(deal.Symbol, quantity: quantity * 0.5m, limit: price * (1 + deal.Sell1Perc), orderSide: OrderSide.Sell);

                // And we put another 50 % sell on 2 % profit
                var order2 = TradeInterface.PlaceTakeProfitOrder(deal.Symbol, quantity: quantity * 0.5m, limit: price * (1 + deal.Sell2Perc), orderSide: OrderSide.Sell);

                deal.Goal1SellOrder = new ClientServerOrder() { ClientOrderId = order1.ClientOrderId };
                deal.Goal2SellOrder = new ClientServerOrder() { ClientOrderId = order2.ClientOrderId };

                // Set next state
                deal.CurrentState = Deal.State.WaitForGoal1;
            }
            else if (OrderCancelled(buyOrder))
                StateTransistion_Cancelled(deal);
        }

        private static void StateTransistion_Cancelled(Deal deal)
        {
            deal.CurrentState = Deal.State.Done;
            deal.CurrentResult = Deal.Result.Cancelled;
        }

        private static bool OrderWorked(BinanceOrder order) => order.Status == OrderStatus.Filled;
        private static bool OrderCancelled(BinanceOrder order) => order.Status == OrderStatus.Canceled;
    }
}
