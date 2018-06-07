using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StopOrderTrader.Trading
{
    [Serializable]
    public class ClientServerOrder
    {
        public string ClientOrderId { get; set; }
        public List<long> FilledOrders { get; set; }

        public override string ToString() => FilledOrders?.Count > 0 ? $"Filled: {FilledOrders.Count}" : "Open";
    }

    [Serializable]
    public class Deal
    {
        private decimal _leftovers;
        private decimal _buyPrice;
        private decimal _sell1Perc;
        private decimal _sell2Perc;
        private decimal _sellStopLoss;

        public int Id { get; set; }

        public enum State { WaitForBuy, WaitForGoal1, WaitForGoal2, Done }
        public enum Result { NotDoneYet, PanicSell, GoalsArchived, Cancelled }

        public string Symbol { get; set; }

        public State CurrentState { get; set; }
        public Result CurrentResult { get; set; }

        public ClientServerOrder BuyOrder { get; set; }
        public ClientServerOrder Goal1SellOrder { get; set; }
        public ClientServerOrder Goal2SellOrder { get; set; }
        public ClientServerOrder OtherSellOrder { get; set; }

        public DateTime CreationTime { get; set; }
        public DateTime? LastChangedTime { get; set; }

        public decimal Leftovers { get => _leftovers; set => _leftovers = value.Normalize(); }
        public decimal BuyPrice { get => _buyPrice; set => _buyPrice = value.Normalize(); }
        public decimal Sell1Perc { get => _sell1Perc; set => _sell1Perc = value.Normalize(); }
        public decimal Sell2Perc { get => _sell2Perc; set => _sell2Perc = value.Normalize(); }
        public decimal SellStopLossPerc { get => _sellStopLoss; set => _sellStopLoss = value.Normalize(); }
    }
}
