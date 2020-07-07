using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using x42.Configuration.Logging;
using x42.Feature.Database;
using x42.Feature.Setup;
using x42.Feature.X42Client;
using x42.ServerNode;
using x42.Server;
using x42.Utilities;
using x42.Configuration;
using System.Collections.Generic;
using x42.Feature.Database.Context;
using System.Linq;
using x42.Controllers.Results;
using x42.Feature.Database.Tables;
using x42.Controllers.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace x42.Feature.Profile
{
    /// <inheritdoc />
    /// <summary>
    ///     Provides an ability to communicate with different network types.
    /// </summary>
    public class ProfileFeature : ServerFeature
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Global application life cycle control - triggers when application shuts down.</summary>
        private readonly IxServerLifetime serverLifetime;

        /// <summary>Factory for creating background async loop tasks.</summary>
        private readonly IAsyncLoopFactory asyncLoopFactory;

        private readonly ServerNodeBase network;
        private readonly DatabaseSettings databaseSettings;
        private readonly ServerSettings nodeSettings;
        private X42ClientSettings x42ClientSettings;
        private readonly X42ClientFeature x42FullNode;
        private readonly DatabaseFeatures database;
        private X42Node x42Client;

        public ProfileFeature(
            ServerNodeBase network,
            ServerSettings nodeSettings,
            ILoggerFactory loggerFactory,
            DatabaseSettings databaseSettings,
            X42ClientSettings x42ClientSettings,
            IxServerLifetime serverLifetime,
            IAsyncLoopFactory asyncLoopFactory,
            X42ClientFeature x42FullNode,
            DatabaseFeatures database
            )
        {
            this.network = network;
            this.nodeSettings = nodeSettings;
            logger = loggerFactory.CreateLogger(GetType().FullName);
            this.databaseSettings = databaseSettings;
            this.serverLifetime = serverLifetime;
            this.asyncLoopFactory = asyncLoopFactory;
            this.x42ClientSettings = x42ClientSettings;
            this.x42FullNode = x42FullNode;
            this.database = database;

            x42Client = new X42Node(x42ClientSettings.Name, x42ClientSettings.Address, x42ClientSettings.Port, logger, serverLifetime, asyncLoopFactory, false);
        }

        /// <inheritdoc />
        public override Task InitializeAsync()
        {
            logger.LogInformation("Profile Initialized");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override void ValidateDependencies(IServerServiceProvider services)
        {
            if (string.IsNullOrEmpty(x42ClientSettings.Name))
            {
                throw new ConfigurationException("x42Client Name setting must be set.");
            }

            if (x42ClientSettings.Port <= 0)
            {
                throw new ConfigurationException("x42Client Port setting must be set.");
            }

            if (x42ClientSettings.Address.AddressFamily == System.Net.Sockets.AddressFamily.Unknown)
            {
                throw new ConfigurationException("x42Client Address setting must be set, and a valid IP address.");
            }
        }

        /// <summary>
        ///     Register a new profile.
        /// </summary>
        public ProfileChangeResult RegisterProfile(ProfileRegisterRequest profileRegisterRequest)
        {
            ProfileChangeResult result = new ProfileChangeResult();

            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                var profiles = dbContext.Profiles.Where(n => n.Name == profileRegisterRequest.Name || n.KeyAddress == profileRegisterRequest.KeyAddress).ToList();
                if (profiles.Count() == 0)
                {
                    var newProfile = new ProfileData()
                    {
                        Name = profileRegisterRequest.Name,
                        KeyAddress = profileRegisterRequest.KeyAddress,
                        TransactionId = profileRegisterRequest.TransactionId
                    };

                    var newRecord = dbContext.Profiles.Add(newProfile);
                    if (newRecord.State == EntityState.Added)
                    {
                        dbContext.SaveChanges();
                        result.Success = true;
                    }
                }
                else
                {
                    result.Success = false;
                    result.ResultMessage = "Profile already exists.";
                }
            }

            return result;
        }

        /// <summary>
        ///     Get profile.
        /// </summary>
        public ProfileResult GetProfile(ProfileRequest profileRequest)
        {
            ProfileResult result = null;

            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                if (!string.IsNullOrWhiteSpace(profileRequest.KeyAddress))
                {
                    var profile = dbContext.Profiles.Where(n => n.KeyAddress == profileRequest.KeyAddress).FirstOrDefault();
                    if (profile != null)
                    {
                        result = new ProfileResult()
                        {
                            KeyAddress = profile.KeyAddress,
                            Name = profile.Name,
                            Signature = profile.Signature,
                            TransactionId = profile.TransactionId
                        };
                    }
                }
                else if (!string.IsNullOrWhiteSpace(profileRequest.Name))
                {
                    var profile = dbContext.Profiles.Where(n => n.Name == profileRequest.Name).FirstOrDefault();
                    if (profile != null)
                    {

                    }
                }
            }

            return result;
        }
    }

    /// <summary>
    ///     A class providing extension methods for <see cref="ProfileFeature" />.
    /// </summary>
    public static class ProfileBuilderExtension
    {
        /// <summary>
        ///     Adds profile components to the server.
        /// </summary>
        /// <param name="serverBuilder">The object used to build the current node.</param>
        /// <returns>The server builder, enriched with the new component.</returns>
        public static IServerBuilder UseProfile(this IServerBuilder serverBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<ProfileFeature>("profile");

            serverBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<ProfileFeature>()
                    .DependOn<DatabaseFeatures>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<ProfileFeature>();
                    });
            });

            return serverBuilder;
        }
    }
}
