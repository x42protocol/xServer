using System;
using Microsoft.Extensions.DependencyInjection;
using x42.Configuration;
using x42.Feature.Setup;
using x42.ServerNode;

namespace x42.Server
{
    /// <summary>
    ///     x42 server builder allows constructing a x42 server using specific components.
    /// </summary>
    public interface IServerBuilder
    {
        /// <summary>User defined server settings.</summary>
        ServerSettings ServerSettings { get; }

        /// <summary>Specification of the xServer.</summary>
        ServerNodeBase ServerNode { get; }

        /// <summary>Collection of DI services.</summary>
        IServiceCollection Services { get; }

        /// <summary>
        ///     Constructs the x42 server with the required features, services, and settings.
        /// </summary>
        /// <returns>Initialized x42 server.</returns>
        IxServer Build();

        /// <summary>
        ///     Adds features to the builder.
        /// </summary>
        /// <param name="configureFeatures">A method that adds features to the collection.</param>
        /// <returns>Interface to allow fluent code.</returns>
        IServerBuilder ConfigureFeature(Action<IFeatureCollection> configureFeatures);

        /// <summary>
        ///     Adds services to the builder.
        /// </summary>
        /// <param name="configureServices">A method that adds services to the builder.</param>
        /// <returns>Interface to allow fluent code.</returns>
        IServerBuilder ConfigureServices(Action<IServiceCollection> configureServices);

        /// <summary>
        ///     Add configurations for the service provider.
        /// </summary>
        /// <param name="configure">A method that configures the service provider.</param>
        /// <returns>Interface to allow fluent code.</returns>
        IServerBuilder ConfigureServiceProvider(Action<IServiceProvider> configure);
    }
}