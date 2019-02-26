using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using X42.Feature.X42Client.Enums;
using X42.Feature.X42Client.Models;
using X42.Feature.X42Client.RestClient.Responses;
using X42.Feature.X42Client.Utils.Extensions;
using X42.Utilities;

namespace X42.Feature.X42Client
{
    public sealed partial class X42Node
    {
        /// <summary>
        ///     Update Wallet TX Data
        /// </summary>
        public async void UpdateWalletTXs()
        {
            //this is called by a different method, if it errored then all the code below would mess up.
            if (!_Error_FS_Info)
            {
                //loop through all loaded wallets
                foreach (string wallet in WalletAccounts.Keys)
                {
                    //Loop through all Wallet Accounts
                    foreach (string account in WalletAccounts[wallet])
                    {
                        //Get Account history
                        GetWalletHistoryResponse accountHistory = await _RestClient.GetWalletHistory(wallet, account);
                        if (accountHistory != null)
                        {
                            //there is only one entry for "history"
                            ProcessAccountTXs(wallet, account, accountHistory.history[0].transactionsHistory);
                        }
                        else
                        {
                            logger.LogInformation(
                                $"An Error Occured Getting Account '{account}' TX History For Wallet '{wallet}', API Response Was NULL!");
                        } //end of if-else if(accountHistory != null)
                    } //end of foreach(string account in WalletAccounts[wallet])
                } //end of foreach(string wallet in WalletAccounts.Keys)
            } //end of if (!_Error_FS_Info)
        } //end of private async void UpdateWalletTXs()

        /// <summary>
        ///     Processes the TX History For a Given Account
        /// </summary>
        /// <param name="wallet">wallet name</param>
        /// <param name="account">account name</param>
        private void ProcessAccountTXs(string wallet, string account, WalletTransactionshistory[] txs)
        {
            List<Transaction> accountTXs = new List<Transaction>();

            //this is the Key that will be used to store the TX's
            string walletAccountKey = $"{wallet}.{account}";

            foreach (WalletTransactionshistory tx in txs)
            {
                TXType txType = TXType.Staked;

                switch (tx.type)
                {
                    case "staked":
                        txType = TXType.Staked;
                        break;
                    case "received":
                        txType = TXType.Received;
                        break;
                    case "send":
                        txType = TXType.Sent;
                        break;
                } //end of switch (tx.type)

                //add the TX
                accountTXs.Add(new Transaction
                {
                    Address = tx.toAddress,
                    Amount = tx.amount.ParseAPIAmount(),
                    BlockID = tx.confirmedInBlock,
                    TXID = tx.id,
                    Timestamp = tx.timestamp,
                    Type = txType
                });
            } //end of foreach(WalletTransactionshistory tx in txs)

            if (AccountTXs.ContainsKey(walletAccountKey))
            {
                //get a list of new TX's
                List<Transaction> newTransactions = accountTXs.Except(AccountTXs[walletAccountKey]).ToList();

                //add to TX History
                AccountTXs[walletAccountKey].AddRange(newTransactions);

                //fire off a 'new TX' event
                OnNewTX(wallet, account, newTransactions);
            }
            else
            {
                AccountTXs.Add(walletAccountKey, accountTXs);

                //fire off a 'new TX' event
                OnNewTX(wallet, account, accountTXs);
            } //end of if-else if (AccountTXs.ContainsKey(walletAccountKey))
        } //end of private void ProcessAccountTX(string wallet, string account)


        /// <summary>
        ///     Gets The Balance of The Wallet
        /// </summary>
        /// <param name="WalletName">Wallet Name</param>
        /// <param name="accountName">Account Name (Optional)</param>
        /// <returns>2 Balences, First Is Confirmed, Second Is Unconfirmed</returns>
        public async Task<Tuple<decimal, decimal>> GetWalletBalance(string walletName, string accountName = null)
        {
            GetWalletBalenceResponse walletBalance = await _RestClient.GetWalletBalance(walletName, accountName);
            Guard.Null(walletBalance, nameof(walletBalance),
                $"Node '{Name}' ({Address}:{Port}) An Error Occured When Trying To Get The Wallet Balance of Wallet '{walletName}' and Account '{accountName}'");

            decimal confirmedBalance = 0;
            decimal unConfirmedBalance = 0;

            foreach (AccountBalance accountBalence in walletBalance.balances)
            {
                confirmedBalance += accountBalence.amountConfirmed.ParseAPIAmount();
                unConfirmedBalance += accountBalence.amountUnconfirmed.ParseAPIAmount();
            } //end of foreach (AccountBalance accountBalence in walletBalence.balances)

            return new Tuple<decimal, decimal>(confirmedBalance, unConfirmedBalance);
        } //end of public decimal GetWalletBalence(string WalletName, string accountName)
    } //end of X42Node.Transactions
}