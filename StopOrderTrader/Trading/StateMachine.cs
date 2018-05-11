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
        public static void Run(List<Deal> deals)
        {
            foreach (var deal in deals)
                Process(deal);
        }

        private static void Process(Deal deal)
        {
            switch (deal.CurrentState)
            {
                case Deal.State.WaitForBuy:
                    WaitForBuy(deal);
                    break;

                case Deal.State.WaitForGoal1:
                    WaitForGoal1(deal);
                    break;

                case Deal.State.WaitForGoal2:
                    WaitForGoal2(deal);
                    break;

                case Deal.State.WaitForLeftovers:
                    WaitForLeftovers(deal);
                    break;
            }
        }

        private static void WaitForLeftovers(Deal deal)
        {
            // Place immediate order for all leftovers
            var orders = TradeInterface.PlaceImmediateOrder(deal.Symbol, deal.Leftovers, OrderSide.Sell);

            deal.SelloffLeftovers = orders;
            deal.Leftovers = 0;

            deal.CurrentState = Deal.State.Done;
            deal.CurrentResult = Deal.Result.GoalsArchived;
        }

        private static void WaitForGoal2(Deal deal)
        {
            var sellOrder = TradeInterface.GetOrderById(deal.Symbol, deal.SellOrder2.ClientOrderId);
            if (OrderWorked(sellOrder))
            {
                // Download actual order
                var actualOrder = TradeInterface.GetActualOrder(sellOrder.Symbol, sellOrder.OrderId);
                deal.SellOrder2.ActualOrderId = actualOrder.OrderId;

                // Set next state
                deal.CurrentState = Deal.State.WaitForLeftovers;
            }
            else if (OrderCancelled(sellOrder))
                StateTransistion_Cancelled(deal);
        }

        private static void WaitForGoal1(Deal deal)
        {
            var sellOrder = TradeInterface.GetOrderById(deal.Symbol, deal.SellOrder1.ClientOrderId);
            if (OrderWorked(sellOrder))
            {
                // Download actual order
                var actualOrder = TradeInterface.GetActualOrder(sellOrder.Symbol, sellOrder.OrderId);
                deal.SellOrder1.ActualOrderId = actualOrder.OrderId;

                deal.Leftovers -= actualOrder.Quantity;

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
                var actualOrder = TradeInterface.GetActualOrder(buyOrder.Symbol, buyOrder.OrderId);
                deal.BuyOrder.ActualOrderId = actualOrder.OrderId;

                // Set leftovers
                deal.Leftovers = buyOrder.ExecutedQuantity;

                // Calculate Sell points
                decimal price = actualOrder.Price;
                decimal quantity = buyOrder.ExecutedQuantity;

                // We put 50 % sell on 1 % profit
                var order1 = TradeInterface.PlaceProfitOrder(deal.Symbol, quantity: quantity * 0.5m, price: price * 1.01m, orderSide: OrderSide.Sell);

                // And we put another 50 % sell on 2 % profit
                var order2 = TradeInterface.PlaceProfitOrder(deal.Symbol, quantity: quantity * 0.5m, price: price * 1.02m, orderSide: OrderSide.Sell);

                deal.SellOrder1 = new ClientServerOrder() { ClientOrderId = order1.ClientOrderId };
                deal.SellOrder2 = new ClientServerOrder() { ClientOrderId = order2.ClientOrderId };

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
