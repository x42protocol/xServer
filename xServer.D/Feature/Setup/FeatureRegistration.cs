﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using x42.Utilities;

namespace x42.Feature.Setup
{
    /// <summary>
    ///     Defines methods for a representation of registered features of the Server.
    /// </summary>
    public interface IFeatureRegistration
    {
        /// <summary>
        ///     Type of the feature startup class. If it implements ConfigureServices method,
        ///     it is invoked to configure the feature's services.
        /// </summary>
        Type FeatureStartupType { get; }

        /// <summary>Type of the feature class.</summary>
        Type FeatureType { get; }

        /// <summary>
        ///     Initializes feature registration DI services and calls configuration delegates of each service
        ///     and the startup type.
        /// </summary>
        /// <param name="serviceCollection">Collection of feature registration's DI services.</param>
        void BuildFeature(IServiceCollection serviceCollection);

        /// <summary>
        ///     Initializes the list of delegates to configure DI services of the feature registration.
        /// </summary>
        /// <param name="configureServices">List of delegates to configure DI services of the feature registration.</param>
        /// <returns>This interface to allow fluent code.</returns>
        IFeatureRegistration FeatureServices(Action<IServiceCollection> configureServices);

        /// <summary>
        ///     Sets the specific startup type to be used by the feature registration.
        /// </summary>
        /// <typeparam name="TStartup">Type of feature startup class to use.</typeparam>
        /// <returns>This interface to allow fluent code.</returns>
        IFeatureRegistration UseStartup<TStartup>();

        /// <summary>
        ///     Adds a feature type to the dependency feature list.
        /// </summary>
        /// <typeparam name="TImplementation">Type of the registered feature class.</typeparam>
        /// <returns>This interface to allow fluent code.</returns>
        IFeatureRegistration DependOn<TImplementation>() where TImplementation : class, IServerFeature;

        /// <summary>
        ///     Ensures dependency feature types are present in the registered features list.
        /// </summary>
        /// <param name="featureRegistrations">List of registered features.</param>
        /// <exception cref="MissingDependencyException">Thrown if feature type is missing.</exception>
        void EnsureDependencies(List<IFeatureRegistration> featureRegistrations);
    }

    /// <summary>
    ///     Default implementation of a representation of a registered feature of the Server.
    /// </summary>
    /// <typeparam name="TImplementation">Type of the registered feature class.</typeparam>
    public class FeatureRegistration<TImplementation> : IFeatureRegistration
        where TImplementation : class, IServerFeature
    {
        /// <summary>List of delegates to configure services of the feature.</summary>
        public readonly List<Action<IServiceCollection>> ConfigureServicesDelegates;

        /// <summary> List of dependency features that should be registered in order to add this feature.</summary>
        private readonly List<Type> dependencies;

        /// <summary>Initializes the instance of the object.</summary>
        public FeatureRegistration()
        {
            ConfigureServicesDelegates = new List<Action<IServiceCollection>>();
            FeatureType = typeof(TImplementation);

            dependencies = new List<Type>();
        }

        /// <inheritdoc />
        public Type FeatureStartupType { get; private set; }

        /// <inheritdoc />
        public Type FeatureType { get; }

        /// <inheritdoc />
        public void BuildFeature(IServiceCollection serviceCollection)
        {
            Guard.NotNull(serviceCollection, nameof(serviceCollection));

            // features can only be singletons
            serviceCollection
                .AddSingleton(FeatureType)
                .AddSingleton(typeof(IServerFeature), provider => provider.GetService(FeatureType));

            foreach (Action<IServiceCollection> configureServicesDelegate in ConfigureServicesDelegates)
                configureServicesDelegate(serviceCollection);

            if (FeatureStartupType != null)
                FeatureStartup(serviceCollection, FeatureStartupType);
        }

        /// <inheritdoc />
        public IFeatureRegistration FeatureServices(Action<IServiceCollection> configureServices)
        {
            Guard.NotNull(configureServices, nameof(configureServices));

            ConfigureServicesDelegates.Add(configureServices);

            return this;
        }

        /// <inheritdoc />
        public IFeatureRegistration UseStartup<TStartup>()
        {
            FeatureStartupType = typeof(TStartup);
            return this;
        }

        /// <inheritdoc />
        public IFeatureRegistration DependOn<TFeatureImplementation>()
            where TFeatureImplementation : class, IServerFeature
        {
            dependencies.Add(typeof(TFeatureImplementation));

            return this;
        }

        /// <inheritdoc />
        public void EnsureDependencies(List<IFeatureRegistration> featureRegistrations)
        {
            foreach (Type dependency in dependencies)
                if (featureRegistrations.All(x => !dependency.IsAssignableFrom(x.FeatureType)))
                    throw new MissingDependencyException($"Dependency feature {dependency.Name} cannot be found.");
        }

        /// <summary>
        ///     A feature can use specified method to configure its services.
        ///     The specified method needs to have the following signature to be invoked:
        ///     <c>void ConfigureServices(IServiceCollection serviceCollection)</c>.
        /// </summary>
        /// <param name="serviceCollection">
        ///     Collection of service descriptors to be passed to the ConfigureServices method of the
        ///     feature registration startup class.
        /// </param>
        /// <param name="startupType">
        ///     Type of the feature registration startup class. If it implements ConfigureServices method, it
        ///     is invoked to configure the feature's services.
        /// </param>
        private void FeatureStartup(IServiceCollection serviceCollection, Type startupType)
        {
            MethodInfo method = startupType.GetMethod("ConfigureServices");
            ParameterInfo[] parameters = method?.GetParameters();
            if (method != null && method.IsStatic && parameters?.Length == 1 &&
                parameters.First().ParameterType == typeof(IServiceCollection))
                method.Invoke(null, new object[] {serviceCollection});
        }
    }
}