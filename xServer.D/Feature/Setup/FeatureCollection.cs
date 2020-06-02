using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace x42.Feature.Setup
{
    /// <summary>
    ///     Defines methods for collection of features of the Server.
    /// </summary>
    public interface IFeatureCollection
    {
        /// <summary>List of features already registered in the collection.</summary>
        List<IFeatureRegistration> FeatureRegistrations { get; }

        /// <summary>Adds a new feature to the collection provided that the feature of the same type has not been added already.</summary>
        /// <typeparam name="TImplementation">Type of the feature to be added to the collection.</typeparam>
        /// <returns>Representation of the registered feature.</returns>
        IFeatureRegistration AddFeature<TImplementation>() where TImplementation : class, IServerFeature;
    }

    /// <summary>
    ///     Collection of features available to and/or used by the Server.
    /// </summary>
    public class FeatureCollection : IFeatureCollection
    {
        /// <summary>List of features already registered in the collection.</summary>
        private readonly List<IFeatureRegistration> featureRegistrations;

        /// <summary>Initializes the object instance.</summary>
        public FeatureCollection()
        {
            featureRegistrations = new List<IFeatureRegistration>();
        }

        /// <inheritdoc />
        public List<IFeatureRegistration> FeatureRegistrations => featureRegistrations;

        /// <inheritdoc />
        public IFeatureRegistration AddFeature<TImplementation>() where TImplementation : class, IServerFeature
        {
            if (featureRegistrations.Any(f => f.FeatureType == typeof(TImplementation)))
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                    "Feature of type {0} has already been registered.", typeof(TImplementation).FullName));

            FeatureRegistration<TImplementation> featureRegistration = new FeatureRegistration<TImplementation>();
            featureRegistrations.Add(featureRegistration);

            return featureRegistration;
        }
    }
}