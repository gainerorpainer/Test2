using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StopOrderTrader.Trading
{
    static class DealHandlerXT
    {
        public static void CancelAndSell(this Deal d)
        {
            void SelloffLeftovers()
            {
                // Selloff leftovers
                var trade = TradeInterface.PlaceImmediateOrder(d.Symbol, d.Leftovers, Binance.Net.Objects.OrderSide.Sell);
                d.Leftovers -= TradeInterface.GetActualOrders(d.Symbol, trade.ClientOrderId).Sum(x => x.Quantity);

                d.OtherSellOrder = new ClientServerOrder()
                {
                    ClientOrderId = trade.ClientOrderId,
                    FilledOrders = trade.FilledOrders
                };
            }

            // Cancel open orders
            switch (d.CurrentState)
            {
                case Deal.State.WaitForBuy:
                    // Cancel Buy order
                    TradeInterface.Client.CancelOrder(d.Symbol, origClientOrderId: d.BuyOrder.ClientOrderId).GetOrThrow();
                    break;
                case Deal.State.WaitForGoal1:
                    TradeInterface.Client.CancelOrder(d.Symbol, origClientOrderId: d.Goal1SellOrder.ClientOrderId).GetOrThrow();
                    TradeInterface.Client.CancelOrder(d.Symbol, origClientOrderId: d.Goal2SellOrder.ClientOrderId).GetOrThrow();
                    SelloffLeftovers();
                    break;
                case Deal.State.WaitForGoal2:
                    TradeInterface.Client.CancelOrder(d.Symbol, origClientOrderId: d.Goal2SellOrder.ClientOrderId).GetOrThrow();
                    SelloffLeftovers();
                    break;
                case Deal.State.Done:
                    throw new Exception("You cannot cancel a done deal");
            }

            d.CurrentState = Deal.State.Done;
            d.CurrentResult = Deal.Result.Cancelled;

        }
    }
}
