using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using x42.Feature.X42Client.Enums;
using x42.Feature.X42Client.Models;
using x42.Feature.X42Client.RestClient.Responses;
using x42.Feature.X42Client.Utils.Extensions;
using x42.Utilities;

namespace x42.Feature.X42Client
{
    public sealed partial class X42Node
    {
        /// <summary>
        ///     Update Wallet TX Data
        /// </summary>
        public async void UpdateWalletTXs()
        {
            //this is called by a different method, if it errored then all the code below would mess up.
            if (!errorFsInfo)
            {
                //loop through all loaded wallets
                foreach (string wallet in WalletAccounts.Keys)
                {
                    //Loop through all Wallet Accounts
                    foreach (string account in WalletAccounts[wallet])
                    {
                        //Get Account history
                        GetWalletHistoryResponse accountHistory = await restClient.GetWalletHistory(wallet, account);
                        if (accountHistory != null)
                        {
                            //there is only one entry for "history"
                            ProcessAccountTXs(wallet, account, accountHistory.history[0].transactionsHistory);
                        }
                        else
                        {
                            logger.LogInformation(
                                $"An Error Occured Getting Account '{account}' TX History For Wallet '{wallet}', API Response Was NULL!");
                        }
                    }
                }
            }
        }

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
                }

                //add the TX
                accountTXs.Add(new Transaction
                {
                    Address = tx.toAddress,
                    Amount = tx.amount.ParseApiAmount(),
                    BlockID = tx.confirmedInBlock,
                    TXID = tx.id,
                    Timestamp = tx.timestamp,
                    Type = txType
                });
            }

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
            }
        }


        /// <summary>
        ///     Gets The Balance of The Wallet
        /// </summary>
        /// <param name="WalletName">Wallet Name</param>
        /// <param name="accountName">Account Name (Optional)</param>
        /// <returns>2 Balences, First Is Confirmed, Second Is Unconfirmed</returns>
        public async Task<Tuple<decimal, decimal>> GetWalletBalance(string walletName, string accountName = null)
        {
            GetWalletBalenceResponse walletBalance = await restClient.GetWalletBalance(walletName, accountName);
            Guard.Null(walletBalance, nameof(walletBalance),
                $"Node '{Name}' ({Address}:{Port}) An Error Occured When Trying To Get The Wallet Balance of Wallet '{walletName}' and Account '{accountName}'");

            decimal confirmedBalance = 0;
            decimal unConfirmedBalance = 0;

            foreach (AccountBalance accountBalence in walletBalance.balances)
            {
                confirmedBalance += accountBalence.amountConfirmed.ParseApiAmount();
                unConfirmedBalance += accountBalence.amountUnconfirmed.ParseApiAmount();
            }

            return new Tuple<decimal, decimal>(confirmedBalance, unConfirmedBalance);
        }

        /// <summary>
        ///     Gets a raw transaction that is present on this full node.
        ///     This method first searches the transaction pool and then tries the block store.
        /// </summary>
        /// <param name="trxid">The transaction ID (a hash of the trancaction).</param>
        /// <param name="verbose">A flag that specifies whether to return verbose information about the transaction.</param>
        /// <returns>Json formatted <see cref="RawTransactionResponse"/> or <see cref="RawTransactionResponse"/>. <c>null</c> if transaction not found. Returns a formatted error if otherwise fails.</returns>
        public async Task<RawTransactionResponse> GetRawTransaction(string trxid, bool verbose)
        {
            RawTransactionResponse rawTransactionResult = await restClient.GetRawTransaction(trxid, verbose);
            Guard.Null(rawTransactionResult, nameof(rawTransactionResult), $"An Error Occured When Trying To Get The Raw Transaction for '{trxid}' with verbose '{verbose}'");
            return rawTransactionResult;
        }

        /// <summary>
        ///     Gets a JSON representation for a given transaction in hex format.
        /// </summary>
        /// <param name="rawHex">A string containing the necessary parameters for a block search request.</param>
        /// <returns>The raw transaction result of the transaction.</returns>
        public async Task<RawTransactionResponse> DecodeRawTransaction(string rawHex)
        {
            RawTransactionResponse rawTransactionResult = await restClient.DecodeRawTransaction(rawHex);
            Guard.Null(rawTransactionResult, nameof(rawTransactionResult), $"An Error Occured When Trying To decode the Transaction for '{rawHex}'");
            return rawTransactionResult;
        }
    }
}