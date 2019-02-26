using System;
using System.Net;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using X42.Feature.X42Client.Enums;
using X42.Feature.X42Client.Utils.Extensions;
using X42.Utilities;

namespace X42.Feature.X42Client
{
    public sealed partial class X42Node
    {
        private readonly SshClient _SSHClient;
        private readonly ForwardedPortLocal _SSHForwardPort;

        //(string name, IPAddress address, ushort port)

        /// <summary>
        ///     Connects To A Remote Node VIA SSH!
        /// </summary>
        /// <param name="name">Node Name</param>
        /// <param name="username">SSH Login</param>
        /// <param name="password">SSH Password</param>
        /// <param name="sshServerAddress">IP Address of SSH Server</param>
        /// <param name="sshPort">SSH Server Port (Default: 22)</param>
        /// <param name="nodeIPAddress">IP Address x42 Node Is Bound To (Default: 127.0.0.1)</param>
        /// <param name="nodePort">Port x42 Node Is Bound To (Default: 42220 - MainNet)</param>
        /// <param name="localBoundAddress">IP Address To Bind Locally (Default: 127.0.0.1)</param>
        /// <param name="localBoundPort">Local Port To Bind To (Default: 42220 - MainNet)</param>
        public X42Node(string name, string username, string password, string sshServerAddress, ushort sshPort = 22,
            string nodeIPAddress = "127.0.0.1", uint nodePort = 42220, string localBoundAddress = "127.0.0.1",
            uint localBoundPort = 42220)
        {
            try
            {
                Guard.Null(name, nameof(name), "Node Name Cannot Be Null");
                Guard.Null(username, nameof(username), "SSH Username Cannot Be Null");
                Guard.Null(password, nameof(password), "SSH Password Cannot Be Null");

                Guard.AssertTrue(IPAddress.TryParse(sshServerAddress, out IPAddress sshAddress),
                    $"Invalid SSH IP Address Provided '{sshServerAddress}'");
                Guard.AssertTrue(IPAddress.TryParse(nodeIPAddress, out IPAddress nodeAddress),
                    $"Invalid x42 Node IP Address Provided '{sshServerAddress}'");
                Guard.AssertTrue(IPAddress.TryParse(localBoundAddress, out IPAddress localBoundIP),
                    $"Invalid Local Bound IP Address Provided '{sshServerAddress}'");

                Guard.AssertTrue(sshPort.IsValidPortRange(),
                    $"Invalid Port Specified For The SSH Server, Value Provided Is '{sshPort}'");
                Guard.AssertTrue(nodePort.IsValidPortRange(),
                    $"Invalid Port Specified For The x42 Node, Value Provided Is '{nodePort}'");
                Guard.AssertTrue(localBoundPort.IsValidPortRange(),
                    $"Invalid Port Specified For Binding Locally, Value Provided Is '{localBoundPort}'");

                _SSHClient = new SshClient(sshServerAddress, username, password);
                _SSHClient.KeepAliveInterval = new TimeSpan(0, 0, 5);
                _SSHClient.ConnectionInfo.Timeout = new TimeSpan(0, 0, 20);
                _SSHClient.Connect();


                if (!_SSHClient.IsConnected)
                {
                    throw new Exception(
                        $"An Error Occured When Connecting To SSH Server '{username}'@'{sshServerAddress}':'{sshPort}'");
                }

                _SSHForwardPort = new ForwardedPortLocal(localBoundPort, nodeIPAddress, nodePort);
                _SSHClient.AddForwardedPort(_SSHForwardPort);

                _SSHForwardPort.Start();

                SetupNodeConnection(name, localBoundIP, (ushort) localBoundPort);

                OnConnected(sshAddress, sshPort, ConnectionType.SSH);
            }
            catch (Exception ex)
            {
                logger.LogInformation(
                    $"Node '{Name}' ({Address}:{Port}) An Error Occured When Connecting To The Remote Node Via SSH '{username}'@'{sshServerAddress}':'{sshPort}'",
                    ex);
                throw;
            } //end of try-catch
        } //end of public X42Node(string name, string sshServerAddress, int port = 22, string nodeIPAddress = "127.0.0.1", int nodePort = 422220)
    } //end of class
}