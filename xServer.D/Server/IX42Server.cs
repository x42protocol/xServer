using System;
using System.Threading.Tasks;
using x42.Controllers.Results;
using x42.Controllers.Requests;
using x42.Feature.Database.Tables;
using x42.Server.Results;
using x42.Utilities;

namespace x42.Server
{
    /// <summary>
    ///     Contract for the x42 server built by x42 server builder.
    /// </summary>
    public interface IxServer : IDisposable
    {
        /// <summary>Global application life cycle control - triggers when application shuts down.</summary>
        IxServerLifetime xServerLifetime { get; }

        /// <summary>Provider of date time functionality.</summary>
        IDateTimeProvider DateTimeProvider { get; }

        /// <summary>Access to DI services and features registered for the x42 server.</summary>
        IServerServiceProvider Services { get; }

        /// <summary>Software version of the x42 server.</summary>
        Version Version { get; }

        /// <summary>Register a server to the networlk.</summary>
        Task<RegisterResult> Register(ServerNodeData serverNode);

        /// <summary>Provides current state of the server.</summary>
        XServerState State { get; }

        /// <summary>Time the server started.</summary>
        DateTime StartTime { get; }

        /// <summary>Time the server started.</summary>
        RuntimeStats Stats { get; }

        /// <summary>Latest log output.</summary>
        string LastLogOutput { get; }

        /// <summary>
        ///     Starts the x42 server.
        /// </summary>
        void Start(StartRequest startRequest);

        /// <summary>
        ///     Starts the xServer features.
        /// </summary>
        void StartFeature();

        /// <summary>
        ///     Stops the x42 server.
        /// </summary>
        void Stop();

        /// <summary>
        ///     Find a service of a particular type
        /// </summary>
        /// <typeparam name="T">Class of type</typeparam>
        /// <param name="failWithDefault">Set to true to return null instead of throwing an error</param>
        /// <returns></returns>
        T ServerService<T>(bool failWithDefault = false);

        /// <summary>
        ///     Add the server to setup
        /// </summary>
        /// <param name="ServerData">Server Data.</param>
        /// <returns>Will return an address if it was able to either find an existing address or gather one from the node, otherwise empty string.</returns>
        Task<string> SetupServer(SetupRequest setupRequest = null);

        /// <summary>
        ///     Get's the server setup status
        /// </summary>
        /// <returns>Will return the status of the server setup.</returns>
        SetupStatusResult GetServerSetupStatus();

        /// <summary>
        ///     Get's the top xServers available to connect to
        ///     MAX 100
        /// </summary>
        /// <returns>Will return the information about the top xServers.</returns>
        TopResult GetTopXServers(int top);

        /// <summary>
        ///     Get's the count of active servers.
        /// </summary>
        /// <returns>Will return the count of active servers.</returns>
        int GetActiveServerCount();

        /// <summary>
        ///     Get the nodes fee address.
        /// </summary>
        /// <returns>Will return a the fee address as a string</returns>
        string GetMyFeeAddress();

        /// <summary>
        ///     Get public key for the xServer.
        /// </summary>
        /// <returns>Will return a the public key as a string</returns>
        string GetPublicKey();

        /// <summary>
        ///     Get the nodes profile name.
        /// </summary>
        /// <returns>Will return the profile name as a string.</returns>
        string GetServerProfileName();
    }

    /// <summary>Represents <see cref="IServer" /> state.</summary>
    public enum XServerState
    {
        /// <summary>Assigned when <see cref="IServer" /> instance is created.</summary>
        Created,

        /// <summary>Assigned when <see cref="IServer.Initialize" /> is called.</summary>
        Initializing,

        /// <summary>Assigned when <see cref="IServer.Initialize" /> finished executing.</summary>
        Initialized,

        /// <summary>Assigned when <see cref="IServer.Start" /> is called.</summary>
        Starting,

        /// <summary>Assigned when <see cref="IServer.Start" /> finished executing.</summary>
        Started,

        /// <summary>Assigned when <see cref="IServer.Dispose" /> is called.</summary>
        Disposing,

        /// <summary>Assigned when <see cref="IServer.Dispose" /> finished executing.</summary>
        Disposed
    }
}