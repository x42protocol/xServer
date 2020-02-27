using System;
using System.Threading.Tasks;
using X42.Feature.Database.Tables;
using X42.Server.Results;
using X42.Utilities;

namespace X42.Server
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

        /// <summary>Latest log output.</summary>
        string LastLogOutput { get; }

        /// <summary>
        ///     Starts the x42 server and all its features.
        /// </summary>
        void Start();

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
        /// <returns>Will return true if new record wad created, otherwise false.</returns>
        bool AddServerToSetup(ServerData serverData);
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