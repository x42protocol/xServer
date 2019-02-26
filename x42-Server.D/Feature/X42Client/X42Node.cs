using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using X42.Feature.X42Client.Enums;
using X42.Feature.X42Client.RestClient;
using X42.Feature.X42Client.RestClient.Responses;
using X42.Feature.X42Client.Utils.Extensions;

namespace X42.Feature.X42Client
{
    public sealed partial class X42Node : IDisposable
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        public X42Node(string name, IPAddress address, ushort port, ILogger mainLogger)
        {
            logger = mainLogger;

            SetupNodeConnection(name, address, port);

            OnConnected(address, port, ConnectionType.DirectAPI);
        }

        /// <summary>
        ///     Sets Up The API Connection To The x42 Node
        /// </summary>
        private void SetupNodeConnection(string name, IPAddress address, ushort port)
        {
            _RestClient = new X42RestClient($"http://{address}:{port}/", logger);
            // _RefreshTimer = new Timer(UpdateNodeData, null, 0, _RefreshTime);

            Name = name;

            UpdateStaticData();
        }

        //The Workhorse which refreshes all Node Data
        public async void UpdateNodeData(object timerState = null)
        {
            try
            {
                //are we connected??
                if (ConnectionMethod == ConnectionType.Disconnected)
                {
                    logger.LogInformation(
                        $"Node '{Name}' ({Address}:{Port}), Aborting 'Status' Update.  Internal State Is Disconnected!");
                    return;
                } //end of if(ConnectionMethod == ConnectionType.Disconnected)

                //############  Status Data #################
                NodeStatusResponse statusData = await _RestClient.GetNodeStatus();
                if (statusData == null)
                {
                    logger.LogInformation($"Node '{Name}' ({Address}:{Port}) An Error Occured Getting Node Status!");
                }
                else
                {
                    //we have a new block, so fire off an event
                    if (statusData.consensusHeight > BlockTIP)
                    {
                        OnNewBlock(statusData.consensusHeight);
                    }

                    //update current height (use consensus because they have been fully validated)
                    BlockTIP = statusData.consensusHeight;

                    DataDirectory = statusData.dataDirectoryPath;

                    NodeVersion = statusData.version;
                    ProtocolVersion = $"{statusData.protocolVersion}";
                    IsTestNet = statusData.testnet;
                } //end of if(statusData == null)


                //############  Update Peers #################
                List<GetPeerInfoResponse> peersResponse = await _RestClient.GetPeerInfo();
                if (peersResponse == null)
                {
                    logger.LogInformation(
                        $"Node '{Name}' ({Address}:{Port}) An Error Occured Getting The Node Peer List!");
                }
                else
                {
                    Peers = peersResponse.ToPeersList();
                } //end of if-else (_Peers == null)


                //############  TX History Processing #################
                UpdateWalletTXs();

                //############  Staking Info #################
                UpdateStakingInformation();
            }
            catch (HttpRequestException ex) //API is not accessible or responding
            {
                OnDisconnected(Address, Port);
                logger.LogInformation(
                    $"Node '{Name}' ({Address}:{Port}) Something Happened & The Node API Is Not Accessible", ex);
            }
            catch (Exception ex)
            {
                logger.LogInformation($"Node '{Name}' ({Address}:{Port}) An Error Occured When Polling For Data!", ex);
            }
        } //end of private async void RefreshNodeData(object timerState)


        //Obtains Data that is NOT likely to change!
        public async void UpdateStaticData()
        {
            //are we connected??
            if (ConnectionMethod == ConnectionType.Disconnected)
            {
                logger.LogInformation(
                    $"Node '{Name}' ({Address}:{Port}), Aborting 'Static Data' Update.  Internal State Is Disconnected!");
                return;
            } //end of if(ConnectionMethod == ConnectionType.Disconnected)

            try
            {
                GetWalletFilesResponse filesData = await _RestClient.GetWalletFiles();
                if (filesData == null)
                {
                    _Error_FS_Info = true; //an error occured, this data is relied upon in the "RefreshNodeData" Method
                    logger.LogInformation(
                        $"Node '{Name}' ({Address}:{Port}), An Error Occured When Getting Node File Information!");
                }
                else
                {
                    WalletPath = filesData.walletsPath;
                    WalletFiles = new List<string>(filesData.walletsFiles);

                    foreach (string wallet in WalletFiles)
                    {
                        //parse MyWallet.wallet.json  to "MyWallet"
                        string walletName = wallet.Substring(0, wallet.IndexOf("."));

                        //Get a list of accounts
                        List<string> walletAccounts = await _RestClient.GetWalletAccounts(walletName);

                        if (walletAccounts == null)
                        {
                            logger.LogInformation(
                                $"An Error Occured When Trying To Get Wallet Accounts For Wallet '{walletName}'");
                        }

                        if (walletAccounts.Count > 0)
                        {
                            //Are there already present wallets? if so overwrite the data, if not then lets add a new record
                            if (WalletAccounts.ContainsKey(walletName))
                            {
                                WalletAccounts[walletName] = walletAccounts;
                            }
                            else
                            {
                                WalletAccounts.Add(walletName, walletAccounts);
                            }
                        } //end of if(walletAccounts.Count > 0)
                    } //end of foreach

                    _Error_FS_Info = false;
                } //end of if-else if (filesData == null)
            }
            catch (HttpRequestException ex) //API is not accessible or responding
            {
                OnDisconnected(Address, Port);
                logger.LogInformation(
                    $"Node '{Name}' ({Address}:{Port}) Something Happened & The Node API Is Not Accessible", ex);
            }
            catch (Exception ex)
            {
                logger.LogInformation(
                    $"Node '{Name}' ({Address}:{Port}) An Error Occured When Polling For Static Data!", ex);
            } //end of try-catch
        } //end of private async void GetStaticData()

        #region IDisposable Code

        private bool _Disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_Disposed || !disposing) return;


            try
            {
                if (_RestClient != null)
                {
                    _RestClient.Dispose();
                }

                if (_SSHForwardPort != null)
                {
                    _SSHClient?.RemoveForwardedPort(_SSHForwardPort);

                    _SSHForwardPort.Dispose();
                } //end of if(_SSHForwardPort != null)

                if (_SSHClient != null)
                {
                    _SSHClient.Disconnect();
                    _SSHClient.Dispose();
                } //end of if (_SSHClient != null)
            }
            finally
            {
                _Disposed = true;
            } //end of try-finally               
        } //end of private void Dispose(bool disposing)

        // no unmanaged code present
        //~X42Node()
        //{
        //    Dispose(true);
        //}

        #endregion
    } //end of public class X42Node
}