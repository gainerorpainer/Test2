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
        public long? ActualOrderId { get; set; }

        public override string ToString() => $"{ClientOrderId},{ActualOrderId}";
    }

    [Serializable]
    public class Deal_old
    {
        public enum State { WaitForBuy, WaitForGoal1, WaitForGoal2, Done }
        public enum Result { NotDoneYet, PanicSell, GoalsArchived, Cancelled }

        public string Symbol { get; set; }

        public State CurrentState { get; set; }
        public Result CurrentResult { get; set; }

        public string BuyOrder { get; set; }
        public string SellOrder1 { get; set; }
        public string SellOrder2 { get; set; }
        public string PanicSellOrder { get; set; }

        public DateTime? CreationTime { get; set; }
        public DateTime? LastChangedTime { get; set; }
    }

    [Serializable]
    public class Deal
    {
        public enum State { WaitForBuy, WaitForGoal1, WaitForGoal2, WaitForLeftovers, Done }
        public enum Result { NotDoneYet, PanicSell, GoalsArchived, Cancelled }

        public string Symbol { get; set; }

        public State CurrentState { get; set; }
        public Result CurrentResult { get; set; }

        public ClientServerOrder BuyOrder { get; set; }
        public ClientServerOrder SellOrder1 { get; set; }
        public ClientServerOrder SellOrder2 { get; set; }
        public ClientServerOrder PanicSellOrder { get; set; }
        public ClientServerOrder SelloffLeftovers { get; set; }

        public DateTime? CreationTime { get; set; }
        public DateTime? LastChangedTime { get; set; }

        public decimal Leftovers { get; set; }
    }
}
