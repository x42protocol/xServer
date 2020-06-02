using System;
using System.Collections.Generic;

namespace x42.Feature.X42Client.Models.Event
{
    public class NewTXEvent : BaseEvent
    {
        public readonly string AccountName;
        public readonly List<Transaction> NewTransactions;
        public readonly string WalletName;


        public NewTXEvent(string walletName, string accountName, List<Transaction> txs)
        {
            NewTransactions = txs;
            Time = DateTime.Now;
        } //end of public NewTXEvent(List<Transaction> txs)
    } //end of public class NewTXEvent: EventArgs
}