using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using X42.Feature.X42Client.Enums;
using X42.Feature.X42Client.RestClient;
using X42.Feature.X42Client.RestClient.Responses;
using X42.Feature.X42Client.Utils.Extensions;
using X42.Utilities;

namespace X42.Feature.X42Client
{
    public sealed partial class X42Node : IDisposable
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>
        /// A cancellation token source that can cancel the node monitoring processes and is linked to the <see cref="IxServerLifetime.ApplicationStopping"/>.
        /// </summary>
        private CancellationTokenSource nodeCancellationTokenSource;

        /// <summary>Global application life cycle control - triggers when application shuts down.</summary>
        private readonly IxServerLifetime serverLifetime;

        /// <summary>Loop in which the node attempts to maintain a connection with the x42 node.</summary>
        private IAsyncLoop nodeMonitorLoop;

        /// <summary>Factory for creating background async loop tasks.</summary>
        private readonly IAsyncLoopFactory asyncLoopFactory;

        /// <summary>Time in milliseconds between attempts to connect to x42 node.</summary>
        private readonly int monitorSleep;

        public X42Node(string name, IPAddress address, uint port, ILogger mainLogger, IxServerLifetime serverLifetime, IAsyncLoopFactory asyncLoopFactory, bool eventsEnabled = true)
        {
            logger = mainLogger;
            this.serverLifetime = serverLifetime;
            this.asyncLoopFactory = asyncLoopFactory;

            monitorSleep = 1000;

            SetupNodeConnection(name, address, port);

            if (eventsEnabled)
            {
                OnConnected(address, port, ConnectionType.DirectApi);
            }
        }

        /// <summary>
        ///     Sets Up The API Connection To The x42 Node
        /// </summary>
        private void SetupNodeConnection(string name, IPAddress address, uint port)
        {
            restClient = new X42RestClient($"http://{address}:{port}/", logger);
        }

        public void StartNodeMonitor()
        {
            this.nodeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new[] { this.serverLifetime.ApplicationStopping });

            this.nodeMonitorLoop = this.asyncLoopFactory.Run("X42Node.StartNodeMonitor", async token =>
            {
                try
                {
                    await this.UpdateNodeData(this.nodeCancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogError("Exception: {0}", ex);
                    this.logger.LogTrace("(-)[UNHANDLED_EXCEPTION]");
                    throw;
                }
            },
            this.nodeCancellationTokenSource.Token,
            repeatEvery: TimeSpan.FromMilliseconds(this.monitorSleep),
            startAfter: TimeSpans.Second);
        }

        //The Workhorse which refreshes all Node Data
        public async Task UpdateNodeData(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    //are we connected??
                    if (ConnectionMethod == ConnectionType.Disconnected)
                    {
                        logger.LogDebug(
                            $"Node '{Name}' ({Address}:{Port}), Aborting 'Status' Update.  Internal State Is Disconnected!");
                    }

                    //############  Status Data #################
                    NodeStatusResponse statusData = await restClient.GetNodeStatus();
                    if (statusData == null)
                    {
                        logger.LogDebug(
                            $"Node '{Name}' ({Address}:{Port}) An Error Occured Getting Node Status!");
                        Status = ConnectionStatus.Offline;
                    }
                    else
                    {
                        //we have a new block, so fire off an event
                        if (statusData.consensusHeight > BlockTIP)
                        {
                            OnNewBlock(statusData.consensusHeight);
                        }

                        //update current height
                        BlockTIP = statusData.consensusHeight;

                        DataDirectory = statusData.dataDirectoryPath;

                        NodeVersion = statusData.version;
                        ProtocolVersion = $"{statusData.protocolVersion}";
                        IsTestNet = statusData.testnet;

                        Status = ConnectionStatus.Online;
                    }

                    //############  Update Peers #################
                    List<GetPeerInfoResponse> peersResponse = await restClient.GetPeerInfo();
                    if (peersResponse == null)
                    {
                        logger.LogDebug(
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

                    await Task.Delay(10000);
                }
                catch (HttpRequestException ex) //API is not accessible or responding
                {
                    OnDisconnected(Address, Port);
                    logger.LogDebug(
                        $"Node '{Name}' ({Address}:{Port}) Something Happened & The Node API Is Not Accessible", ex);
                    Status = ConnectionStatus.Offline;
                }
                catch (Exception ex)
                {
                    logger.LogDebug($"Node '{Name}' ({Address}:{Port}) An Error Occured When Polling For Data!",
                        ex);
                    Status = ConnectionStatus.Offline;
                }
            }
        }


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
                GetWalletFilesResponse filesData = await restClient.GetWalletFiles();
                if (filesData == null)
                {
                    errorFsInfo = true; //an error occured, this data is relied upon in the "RefreshNodeData" Method
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
                        List<string> walletAccounts = await restClient.GetWalletAccounts(walletName);

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

                    errorFsInfo = false;
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

        public async Task<GetTXOutResponse> GetTXOutData(string txid, long vout)
        {
            return await restClient.GetTXOut(txid, vout.ToString());
        }

        public async Task<GetAddressesBalancesResponse> GetAddressBalances(string address)
        {
            return await restClient.GetAddressBalances(address);
        }

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

                this.nodeMonitorLoop?.Dispose();
                this.nodeMonitorLoop = null;

                if (restClient != null)
                {
                    restClient.Dispose();
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
    }
}