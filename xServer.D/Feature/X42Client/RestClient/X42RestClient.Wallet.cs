using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using x42.Feature.X42Client.Enums;
using x42.Feature.X42Client.RestClient.Requests;
using x42.Feature.X42Client.RestClient.Responses;
using x42.Utilities;

namespace x42.Feature.X42Client.RestClient
{
    /*
     /api/Wallet/signmessage
     [DONE] /api/Wallet/verifysignature
     [DONE] /api/Wallet/mnemonic
     [DONE] /api/Wallet/create
     /api/Wallet/load
     /api/Wallet/recover
     /api/Wallet/recover-via-extpubkey
     [DONE] /api/Wallet/general-info
     [DONE] /api/Wallet/history
     [DONE] /api/Wallet/balance
     [DONE] /api/Wallet/received-by-address
     /api/Wallet/maxbalance
     [DONE]/api/Wallet/spendable-transactions
     /api/Wallet/estimate-txfee
     [DONE] /api/Wallet/build-transaction
     /api/Wallet/send-transaction
     [DONE] /api/Wallet/files
     [DONE] /api/Wallet/account
     [DONE] /api/Wallet/accounts
     /api/Wallet/unusedaddress
     [DONE] /api/Wallet/unusedaddresses
     [DONE] /api/Wallet/addresses
     [DONE] /api/Node/validateaddress
     /api/Wallet/remove-transactions
     /api/Wallet/extpubkey
     /api/Wallet/sync
     /api/Wallet/syncfromdate
     /api/Wallet/splitcoins
     */
    public partial class X42RestClient
    {
        /// <summary>
        /// Verify the signature of a message.
        /// </summary>
        /// <returns>If verification was successful it will return true.</returns>
        public async Task<SignMessageResult> SignMessage(SignMessageRequest signMessageRequest)
        {
            try
            {
                Guard.Null(signMessageRequest, nameof(signMessageRequest), "Sign Message Request Cannot Be NULL/Empty!");

                SignMessageResult response = await base.SendPostJSON<SignMessageResult>("/api/Wallet/signmessage", signMessageRequest);

                return response;
            }
            catch (Exception)
            {
                return new SignMessageResult();
            }
        }

        /// <summary>
        /// Verify the signature of a message.
        /// </summary>
        /// <returns>If verification was successful it will return true.</returns>
        public async Task<bool> VerifySignedMessage(string externalAddress, string message, string signature)
        {
            try
            {
                Guard.Null(externalAddress, nameof(externalAddress), "External Address Cannot Be NULL/Empty!");
                Guard.Null(message, nameof(message), "Message Cannot Be NULL/Empty!");
                Guard.Null(signature, nameof(signature), "Signature Cannot Be NULL/Empty!");

                VerifyRequest request = new VerifyRequest
                {
                    externalAddress = externalAddress,
                    message = message,
                    signature = signature
                };

                string response = await base.SendPostJSON<string>("/api/Wallet/verifymessage", request);

                return response == "True";
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        ///     Gets a List of Spendable TX's
        /// </summary>
        /// <param name="walletName">Name of Wallet</param>
        /// <param name="account">Name of Accounts</param>
        /// <param name="minConfirms">Minimum # of Confirmations</param>
        /// <returns>List of TX's</returns>
        public async Task<GetSpendableTXResponse> GetSpendableTransactions(string walletName, string account,
            int minConfirms = 50)
        {
            try
            {
                Guard.Null(walletName, nameof(walletName), "Wallet Name Cannot Be NULL/Empty!");
                Guard.Null(account, nameof(account), "Wallet Account Cannot Be NULL/Empty!");

                GetSpendableTXResponse response = await base.SendGet<GetSpendableTXResponse>(
                    $"api/Wallet/spendable-transactions?WalletName={walletName}&AccountName={WebUtility.UrlEncode(account)}&MinConfirmations={minConfirms}");
                Guard.Null(response, nameof(response), "'api/Wallet/spendable-transactions' API Response Was Null!");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogCritical(
                    $"An Error '{ex.Message}' Occured When Getting a List of Spendable TX's for Wallet '{walletName}'/'{account}'",
                    ex);
                throw;
            } //end of try-catch
        } //end of public async Task<List<GetSpendableTXResponse>> GetSpendableTransactions(string walletName, string account, int minConfirms = 50)


        /// <summary>
        ///     Generates A New Mnemonic Foe Use In Creating A Wallet
        /// </summary>
        /// <param name="language">What Language To Use (Default: English)</param>
        /// <param name="wordCount">How Many Words To Generate (Default 24)</param>
        public async Task<string> CreateMnemonic(MnemonicWordCount wordCount = MnemonicWordCount.Words_24,
            MnemomicLanguage language = MnemomicLanguage.English)
        {
            try
            {
                string languageParam = "English";

                switch (language)
                {
                    case MnemomicLanguage.ChineseSimplified:
                        languageParam = "ChineseSimplified";
                        break;
                    case MnemomicLanguage.ChineseTraditional:
                        languageParam = "ChineseTraditional";
                        break;
                    case MnemomicLanguage.English: break; // its the default
                    case MnemomicLanguage.French:
                        languageParam = "French";
                        break;
                    case MnemomicLanguage.Japanese:
                        languageParam = "Japanese";
                        break;
                    case MnemomicLanguage.Spanish:
                        languageParam = "Spanish";
                        break;
                } //end of switch (language)

                string response =
                    await base.SendGet<string>($"api/Wallet/mnemonic?language={languageParam}&wordCount=${wordCount}");

                Guard.Null(response, nameof(response), "'api/Wallet/mnemonic' API Response Was Null!");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogCritical(
                    $"An Error '{ex.Message}' Occured When Creating a Mnemonic With The Paramiters Language '{language}', Word Count '{wordCount}'",
                    ex);
                throw;
            } //end of try-catch
        } //end of public async Task<string> CreateMnemonic(MnemomicLanguage language, MnemonicWordCount wordCount)


        /// <summary>
        ///     Creates A Wallet
        /// </summary>
        /// <param name="mnemonic">Nemonic To Use</param>
        /// <param name="password">Wallet Password</param>
        /// <param name="passphrase">Wallet Passphrase</param>
        /// <param name="name"></param>
        /// <returns>MNemonic Used To Create The Wallet</returns>
        public async Task<string> CreateWallet(string mnemonic, string password, string name, string passphrase = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(passphrase))
                {
                    passphrase = password;
                }

                Guard.Null(mnemonic, nameof(mnemonic), "mnemonic Cannot Be NULL/Empty!");
                Guard.Null(password, nameof(password), "Wallet Password Cannot Be NULL/Empty!");
                Guard.Null(name, nameof(name), "Wallet Name Cannot Be NULL/Empty!");

                CreateWalletRequest request = new CreateWalletRequest
                {
                    name = name,
                    passphrase = passphrase,
                    password = password,
                    mnemonic = mnemonic
                };

                string response = await base.SendPostJSON<string>("api/Wallet/create", request);

                Guard.Null(response, nameof(response), "'api/Wallet/create' API Response Was Null!");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogCritical($"An Error '{ex.Message}' Occured When Creating Wallet '{name}'", ex);
                throw;
            } //end of try-catch
        } //end of public async Task<string> CreateWallet(string mnemonic, string password, string passphrase, string name)

        /// <summary>
        ///     Validates Whether The Supplied Address Is Correct
        /// </summary>
        /// <param name="address">x42 Address</param>
        public async Task<bool> ValidateAddress(string address)
        {
            try
            {
                Guard.Null(address, nameof(address), "Cannot Validate User Address, It Is Null/Empty!");

                logger.LogDebug($"Validating Address '{address.Trim()}'");

                HttpStatusCode responseCode = await base.SendGet($"api/Node/validateaddress?address={address.Trim()}");


                switch (responseCode)
                {
                    case HttpStatusCode.OK:
                        logger.LogDebug($"Address Validation '{address.Trim()}' Was Good!");
                        return true;
                    default:
                        logger.LogDebug($"Address Validation '{address.Trim()}' Failed!");
                        return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical($"An Error '{ex.Message}' Occured When Validating Address '{address.Trim()}'", ex);
                throw;
            } //end of try-catch
        } //end of public async Task<bool> ValidateAddress(string address)

        /// <summary>
        ///     Get General Wallet Information
        /// </summary>
        /// <param name="walletName">Name of The Wallet</param>
        public async Task<WalletGeneralInfoResponse> GetWalletGeneralInfo(string walletName)
        {
            try
            {
                Guard.Null(walletName, nameof(walletName),
                    "Unable To Get Wallet General Info, Provided Name Is NULL/Empty!");

                WalletGeneralInfoResponse response =
                    await base.SendGet<WalletGeneralInfoResponse>($"api/Wallet/general-info?Name={walletName.Trim()}");

                Guard.Null(response, nameof(response), "'api/Wallet/general-info' API Response Was Null!");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogCritical(
                    $"An Error '{ex.Message}' Occured When Getting General Wallet Info, For Wallet '{walletName.Trim()}'!",
                    ex);
                throw;
            } //end of try-catch
        } //end of public async Task<WalletGeneralInfoResponse> GetWalletGeneralInfo()

        /// <summary>
        ///     Generate A New Account In The Wallet
        /// </summary>
        /// <param name="walletName">Wallet Name</param>
        /// <param name="password">Wallet Password</param>
        /// <returns></returns>
        public async Task<string> CreateAccount(string walletName, string password)
        {
            try
            {
                Guard.Null(walletName, nameof(walletName),
                    "Unable To Create Wallet Account, Provided Name Is NULL/Empty!");
                Guard.Null(password, nameof(password),
                    "Unable To Create Wallet Account, Provided Password Is NULL/Empty!");

                CreateAccountRequest request = new CreateAccountRequest
                {
                    walletName = walletName,
                    password = password
                };

                string response = await base.SendPostJSON<string>("api/Wallet/account", request);

                Guard.Null(response, nameof(response), "'api/Wallet/account' API Response Was Null!");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogCritical(
                    $"An Error '{ex.Message}' Occured When Creating Wallet Account Info, For Wallet '{walletName.Trim()}'!",
                    ex);
                throw;
            } //end of try-catch
        } //end of public async Task<string> CreateAccount(string walletName, string password)

        /// <summary>
        ///     Gets The Physical Wallet File Paths
        /// </summary>
        public async Task<GetWalletFilesResponse> GetWalletFiles()
        {
            try
            {
                GetWalletFilesResponse response = await base.SendGet<GetWalletFilesResponse>("api/Wallet/files");

                Guard.Null(response, nameof(response), "'api/Wallet/files' API Response Was Null!");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogCritical($"An Error '{ex.Message}' Occured When Getting Wallet File Paths!!", ex);

                throw;
            }
        } //end of public async Task<GetWalletFilesResponse> GetWalletFiles()

        /// <summary>
        ///     Gets All Accounts Within The Wallet
        /// </summary>
        /// <param name="walletName">Name of Wallet</param>
        public async Task<List<string>> GetWalletAccounts(string walletName)
        {
            try
            {
                Guard.Null(walletName, nameof(walletName),
                    "Unable To Get Wallet Account Info, Provided Name Is NULL/Empty!");

                List<string> response =
                    await base.SendGet<List<string>>($"api/Wallet/accounts?WalletName={walletName.Trim()}");

                Guard.Null(response, nameof(response), "'api/Wallet/general-info' API Response Was Null!");

                Guard.AssertTrue(response.Count > 0,
                    $"No Account Information Returned For Wallet '{walletName.Trim()}'");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogCritical(
                    $"An Error '{ex.Message}' Occured When Getting Wallet Account Info, For Wallet '{walletName.Trim()}'!",
                    ex);
                throw;
            } //end of try-catch
        } //end of public async Task<List<string>> GetWalletAccounts(string walletName)


        /// <summary>
        ///     Generate A List of Unused Addresses
        /// </summary>
        /// <param name="walletName">Name of Wallet</param>
        /// <param name="account">Name of Account</param>
        /// <param name="count"># of Addresses To Generate</param>
        /// <returns></returns>
        public async Task<List<string>> GenerateUnusedWalletAddresses(string walletName, string account, int count)
        {
            try
            {
                Guard.Null(walletName, nameof(walletName),
                    "Unable To Get Generate Unused Addresses, Provided Wallet Name Is NULL/Empty!");
                Guard.Null(account, nameof(account),
                    "Unable To Get Generate Unused Addresses, Provided Account Name Is NULL/Empty!");
                Guard.AssertTrue(count > 0, $"Invalid Number '{count}' of Addresses Specified");

                List<string> response = await base.SendGet<List<string>>(
                    $"api/Wallet/unusedaddresses?WalletName={walletName.Trim()}&AccountName={Uri.EscapeDataString(account.Trim())}&Count={count}");

                Guard.Null(response, nameof(response), "'api/Wallet/unusedaddresses' API Response Was Null!");

                Guard.AssertTrue(response.Count > 0,
                    $"No Unused Addresses Returned For Wallet '{walletName.Trim()}' and Account '{account.Trim()}'");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogCritical(
                    $"An Error '{ex.Message}' Occured When Getting Unused Addresses, For Wallet '{walletName.Trim()}' and Account '{account.Trim()}'!",
                    ex);
                throw;
            } //end of try-catch
        } //end of public async Task<List<string>> GenerateUnusedAddresses(string walletName, string account, int count)

        /// <summary>
        ///     List All Addresses In A Wallet
        /// </summary>
        /// <param name="walletName">Name of Wallet</param>
        /// <param name="account">Name of Account</param>
        public async Task<GetWalletAddressesResponse> GetWalletAddresses(string walletName, string account)
        {
            try
            {
                Guard.Null(walletName, nameof(walletName),
                    "Unable To Get Wallet Addresses, Provided Wallet Name Is NULL/Empty!");
                Guard.Null(account, nameof(account),
                    "Unable To Get Wallet Addresses, Provided Account Name Is NULL/Empty!");

                GetWalletAddressesResponse response = await base.SendGet<GetWalletAddressesResponse>(
                    $"api/Wallet/addresses?WalletName={walletName.Trim()}&AccountName={Uri.EscapeDataString(account.Trim())}");

                Guard.Null(response, nameof(response), "'api/Wallet/unusedaddresses' API Response Was Null!");

                Guard.AssertTrue(response.addresses.Length > 0,
                    $"No Addresses Returned For Wallet '{walletName.Trim()}' and Account '{account.Trim()}'");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogCritical(
                    $"An Error '{ex.Message}' Occured When Getting Addresses, For Wallet '{walletName.Trim()}' and Account '{account.Trim()}'!",
                    ex);
                throw;
            } //end of try-catch
        }

        /// <summary>
        ///     Gets The Balence Info For An Address In A Wallet
        /// </summary>
        /// <param name="address">Address To Check</param>
        /// <returns></returns>
        public async Task<GetRecievedAddressInfoResponse> GetRecievedAddressBalence(string address)
        {
            try
            {
                Guard.AssertTrue(await ValidateAddress(address.Trim()),
                    $"Supplied Address '{address.Trim()}' Is Not Valid!");

                GetRecievedAddressInfoResponse response =
                    await base.SendGet<GetRecievedAddressInfoResponse>(
                        $"api/Wallet/received-by-address?Address={address.Trim()}");

                Guard.Null(response, nameof(response), "'api/Wallet/received-by-address' API Response Was Null!");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogCritical(
                    $"An Error '{ex.Message}' Occured When Getting Addresses Balence, Address'{address.Trim()}'!", ex);
                throw;
            } //end of try-catch
        } //end of public async Task<GetRecievedAddressInfoResponse> GetRecievedAddressBalence(string address)

        /// <summary>
        ///     Gets The Balance of The Wallet
        /// </summary>
        /// <param name="walletName">Name of Wallet</param>
        /// <param name="account">Name of Account (Leave Blank for All)</param>
        /// <returns></returns>
        public async Task<GetWalletBalenceResponse> GetWalletBalance(string walletName, string account = null)
        {
            try
            {
                Guard.Null(walletName, nameof(walletName),
                    "Unable To Get Wallet Addresses, Provided Wallet Name Is NULL/Empty!");

                StringBuilder queryURL = new StringBuilder($"api/Wallet/balance?WalletName={walletName.Trim()}");

                if (!string.IsNullOrWhiteSpace(account))
                {
                    queryURL.Append($"&AccountName={Uri.EscapeDataString(account.Trim())}");
                }

                GetWalletBalenceResponse response = await base.SendGet<GetWalletBalenceResponse>(queryURL.ToString());

                Guard.Null(response, nameof(response), "'api/Wallet/balance' API Response Was Null!");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogCritical(
                    $"An Error '{ex.Message}' Occured When Getting Ballence For Wallet '{walletName.Trim()}'!", ex);
                throw;
            } //end of try-catch
        } //end of public async Task<string> GetWalletBalence(string walletName, string accountName = null)


        /// <summary>
        ///     Get The Transaction History
        /// </summary>
        /// <param name="walletName">Wallet Name</param>
        /// <param name="account">Account Name (Leave Blank for all)</param>
        /// <param name="skip">Skip X Records (Leave Blank for all)</param>
        /// <param name="take">Take X Records (Leave Blank for all)</param>
        /// <param name="searchQuery">Search Query To Use (Leave Blank for all)</param>
        public async Task<GetWalletHistoryResponse> GetWalletHistory(string walletName, string account = null,
            int skip = -1, int take = -1, string searchQuery = null)
        {
            try
            {
                StringBuilder queryURL = new StringBuilder($"api/Wallet/history?WalletName={walletName.Trim()}");

                if (!string.IsNullOrWhiteSpace(account))
                {
                    queryURL.Append($"&AccountName={Uri.EscapeDataString(account.Trim())}");
                }

                if (skip > -1)
                {
                    queryURL.Append($"&Skip={skip}");
                }

                if (take > -1)
                {
                    queryURL.Append($"&Take={take}");
                }

                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    queryURL.Append($"&SearchQuery={Uri.EscapeDataString(searchQuery.Trim())}");
                }

                GetWalletHistoryResponse response = await base.SendGet<GetWalletHistoryResponse>(queryURL.ToString());

                Guard.Null(response, nameof(response), "'api/Wallet/history' API Response Was Null!");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogCritical(
                    $"An Error '{ex.Message}' Occured When Getting History For Wallet '{walletName.Trim()}'!", ex);
                throw;
            } //end of try-catch
        } //end of public async Task<GetWalletHistoryResponse> GetWalletHistory(string walletName, string account = null, int skip = -1, int take = -1, string searchQuery = null)


        /// <summary>
        ///     Build a TX Hex String Ready To Be Broadcast on The Network
        /// </summary>
        /// <param name="walletName">Wallet Name</param>
        /// <param name="account">Account Name</param>
        /// <param name="password">Wallet Password</param>
        /// <param name="destinationAddress">Destination Address</param>
        /// <param name="amount">Amount To Send</param>
        /// <param name="allowUnconfirmed">Include Unconfirmed TX's</param>
        /// <param name="shuffleCoins">Coin Control?</param>
        /// <returns>TX Hex String</returns>
        public async Task<BuildTXResponse> BuildTransaction(string walletName, string account, string password,
            string destinationAddress, long amount, bool allowUnconfirmed = false, bool shuffleCoins = true)
        {
            try
            {
                Guard.Null(walletName, nameof(walletName), "Unable To Build TX, Provided Wallet Name Is NULL/Empty!");
                Guard.Null(account, nameof(account), "Unable To Build TX, Provided Account Name Is NULL/Empty!");
                Guard.Null(password, nameof(password), "Unable To Build TX, Provided Password Is NULL/Empty!");
                Guard.AssertTrue(await ValidateAddress(destinationAddress),
                    $"Unable To Build TX, Destination Address '{destinationAddress.Trim()}' Is Not Valid!");

                GetWalletBalenceResponse accountBalence = await GetWalletBalance(walletName, account);
                Guard.Null(accountBalence, nameof(accountBalence),
                    $"Unable To Build TX, Account '{account}' Balence Request Was NULL/Empty!");

                Guard.AssertTrue(accountBalence.balances[0].amountConfirmed > 0,
                    $"Unable To Build TX, Insufficient Funds! Trying To Send '{amount}' When Account Only Has '{accountBalence.balances[0].amountConfirmed}'");

                BuildTXRequest buildRequest = new BuildTXRequest
                {
                    feeAmount = "0",
                    password = password,
                    walletName = walletName,
                    accountName = account,
                    recipients = new[]
                        {new x42Recipient {destinationAddress = destinationAddress, amount = amount.ToString()}},
                    allowUnconfirmed = allowUnconfirmed,
                    shuffleOutputs = shuffleCoins
                };

                BuildTXResponse response =
                    await base.SendPostJSON<BuildTXResponse>("api/Wallet/build-transaction", buildRequest);

                Guard.Null(response, nameof(response), "'api/Wallet/build-transaction' API Response Was Null!");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogCritical(
                    $"An Error '{ex.Message}' Occured When Building A TX! [Wallet: '{walletName.Trim()}', Account: '{account.Trim()}', To: '{destinationAddress.Trim()}', Amount: '{amount}'",
                    ex);
                throw;
            } //end of try-catch
        } //end of public async Task<BuildTXResponse> BuildTransaction(string walletName, string account, string destinationAddress, decimal amount, bool allowUnconfirmed = false, bool shuffleCoins = true)
    } //end of class
}