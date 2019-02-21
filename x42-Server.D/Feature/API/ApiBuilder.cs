using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;
using x42.Feature.API.Requirements;
using X42.Server;
using X42.Utilities;
using X42.Utilities.JsonConverters;

namespace X42.Feature.Api
{
    public class ApiBuilder
    {
        /// <summary>Instance logger.</summary>
        private ILogger logger;

        public ApiBuilder(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add service and create Policy to allow Cross-Origin Requests
            services.AddCors
            (
                options =>
                {
                    options.AddPolicy
                    (
                        "CorsPolicy",
                        builder =>
                        {
                            string[] publicAddress = {"http://*", "https://*"};

                            builder
                                .WithOrigins(publicAddress)
                                .AllowAnyMethod()
                                .AllowAnyHeader()
                                .AllowCredentials();
                        }
                    );
                }
            );

            services.AddAuthorization(options =>
            {
                var privateAddressList = new List<string>
                {
                    "127.0.0.1",
                    "::1"
                };

                options.AddPolicy(Policy.PrivateAccess,
                    policy => policy.Requirements.Add(new PrivateOnlyRequirement(privateAddressList)));
            });

            // Add framework services.
            services.AddMvc(options =>
                {
                    options.Filters.Add(typeof(LoggingActionFilter));

                    var serviceProvider = services.BuildServiceProvider();
                    var apiSettings = (ApiSettings) serviceProvider.GetRequiredService(typeof(ApiSettings));
                    if (apiSettings.KeepaliveTimer != null) options.Filters.Add(typeof(KeepaliveActionFilter));
                })
                // add serializers for NBitcoin objects
                .AddJsonOptions(options => Serializer.RegisterFrontConverters(options.SerializerSettings))
                .AddControllers(services);

            // Register the Swagger generator, defining one or more Swagger documents
            services.AddSwaggerGen(setup =>
            {
                setup.SwaggerDoc("v1", new Info {Title = "X42.MasterNode.Api", Version = "v1"});

                //Set the comments path for the swagger json and ui.
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var apiXmlPath = Path.Combine(basePath, "X42.MasterNode..xml");

                if (File.Exists(apiXmlPath)) setup.IncludeXmlComments(apiXmlPath);

                setup.DescribeAllEnumsAsStrings();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory,
            ApiSettings apiSettings)
        {
            logger = loggerFactory.CreateLogger(typeof(ApiBuilder).FullName);

            app.UseCors("CorsPolicy");

            app.UseMvc();

            if (apiSettings.EnableSwagger)
            {
                // Enable middleware to serve generated Swagger as a JSON endpoint.
                app.UseSwagger();

                // Enable middleware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
                app.UseSwaggerUI(c =>
                {
                    c.DefaultModelRendering(ModelRendering.Model);
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "X42.MasterNode.Api V1");
                });
            }
        }

        public static IWebHost Initialize(IEnumerable<ServiceDescriptor> services, X42Server x42Server,
            ApiSettings apiSettings, ICertificateStore store, IWebHostBuilder webHostBuilder)
        {
            Guard.NotNull(x42Server, nameof(x42Server));
            Guard.NotNull(webHostBuilder, nameof(webHostBuilder));

            var apiUri = apiSettings.ApiUri;

            var certificate = apiSettings.UseHttps
                ? GetHttpsCertificate(apiSettings.HttpsCertificateFilePath, store)
                : null;

            webHostBuilder
                .UseKestrel(options =>
                {
                    if (!apiSettings.UseHttps)
                        return;

                    Action<ListenOptions> configureListener = listenOptions => { listenOptions.UseHttps(certificate); };
                    var ipAddresses = Dns.GetHostAddresses(apiSettings.ApiUri.DnsSafeHost);
                    foreach (var ipAddress in ipAddresses)
                        options.Listen(ipAddress, apiSettings.ApiPort, configureListener);
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(apiUri.ToString())
                .ConfigureServices(collection =>
                {
                    if (services == null) return;

                    // copies all the services defined for the x42 server to the Api.
                    // also copies over singleton instances already defined
                    foreach (var service in services)
                    {
                        var obj = x42Server.Services.ServiceProvider.GetService(service.ServiceType);
                        if (obj != null && service.Lifetime == ServiceLifetime.Singleton &&
                            service.ImplementationInstance == null)
                            collection.AddSingleton(service.ServiceType, obj);
                        else
                            collection.Add(service);
                    }
                })
                .UseStartup<ApiBuilder>();

            var host = webHostBuilder.Build();

            host.Start();

            return host;
        }

        private static X509Certificate2 GetHttpsCertificate(string certificateFilePath, ICertificateStore store)
        {
            if (store.TryGet(certificateFilePath, out var certificate))
                return certificate;

            throw new FileLoadException($"Failed to load certificate from path {certificateFilePath}");
        }
    }
}