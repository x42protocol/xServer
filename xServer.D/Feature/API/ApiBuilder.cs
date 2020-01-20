using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using x42.Feature.API;
using X42.Feature.API.Requirements;
using X42.Server;
using X42.Utilities;
using X42.Utilities.JsonConverters;

namespace X42.Feature.Api
{
    public class ApiBuilder
    {
        /// <summary>Instance logger.</summary>
        private ILogger logger;

        public ApiBuilder(IWebHostEnvironment env)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
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
                            builder
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowAnyOrigin();
                        }
                    );
                }
            );

            services.AddAuthorization(options =>
            {
                List<string> privateAddressList = new List<string>
                {
                    "127.0.0.1",
                    "::1"
                };

                options.AddPolicy(Policy.PrivateAccess, policy => policy.Requirements.Add(new PrivateOnlyRequirement(privateAddressList)));
            });

            // Add framework services.
            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(LoggingActionFilter));
                options.EnableEndpointRouting = false;
                ServiceProvider serviceProvider = services.BuildServiceProvider();
                var apiSettings = (ApiSettings)serviceProvider.GetRequiredService(typeof(ApiSettings));
                if (apiSettings.KeepaliveTimer != null)
                {
                    options.Filters.Add(typeof(KeepaliveActionFilter));
                }
            })
                // add serializers for NBitcoin objects
                .AddNewtonsoftJson(options => Serializer.RegisterFrontConverters(options.SerializerSettings))
                .AddControllers(services);

            // Enable API versioning.
            // Note much of this is borrowed from https://github.com/microsoft/aspnet-api-versioning/blob/master/samples/aspnetcore/SwaggerSample/Startup.cs
            services.AddApiVersioning(options =>
            {
                // Our versions are configured to be set via URL path, no need to read from querystring etc.
                options.ApiVersionReader = new UrlSegmentApiVersionReader();

                // When no API version is specified, redirect to version 1.
                options.AssumeDefaultVersionWhenUnspecified = true;
            });

            // Add the versioned API explorer, which adds the IApiVersionDescriptionProvider service and allows Swagger integration.
            services.AddVersionedApiExplorer(
                options =>
                {
                    // Format the version as "'v'major[.minor][-status]"
                    options.GroupNameFormat = "'v'VVV";

                    // Substitute the version into the URLs in the swagger interface where we would otherwise see {version:apiVersion}
                    options.SubstituteApiVersionInUrl = true;
                });

            // Add custom Options injectable for Swagger. This is injected with the IApiVersionDescriptionProvider service from above.
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

            // Register the Swagger generator. This will use the options we injected just above.
            services.AddSwaggerGen();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, ApiSettings apiSettings)
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
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "V1");
                });
            }
        }

        public static IWebHost Initialize(IEnumerable<ServiceDescriptor> services, X42Server x42Server,
            ApiSettings apiSettings, ICertificateStore store, IWebHostBuilder webHostBuilder)
        {
            Guard.NotNull(x42Server, nameof(x42Server));
            Guard.NotNull(webHostBuilder, nameof(webHostBuilder));

            Uri apiUri = apiSettings.ApiUri;

            X509Certificate2 certificate = apiSettings.UseHttps
                ? GetHttpsCertificate(apiSettings.HttpsCertificateFilePath, store)
                : null;

            webHostBuilder
                .UseKestrel(options =>
                {
                    if (!apiSettings.UseHttps)
                        return;

                    Action<ListenOptions> configureListener = listenOptions => { listenOptions.UseHttps(certificate); };
                    IPAddress[] ipAddresses = Dns.GetHostAddresses(apiSettings.ApiUri.DnsSafeHost);
                    foreach (IPAddress ipAddress in ipAddresses)
                        options.Listen(ipAddress, apiSettings.ApiPort, configureListener);
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(apiUri.ToString())
                .ConfigureServices(collection =>
                {
                    if (services == null) return;

                    // copies all the services defined for the xServer to the Api.
                    // also copies over singleton instances already defined
                    foreach (ServiceDescriptor service in services)
                    {
                        object obj = x42Server.Services.ServiceProvider.GetService(service.ServiceType);
                        if (obj != null && service.Lifetime == ServiceLifetime.Singleton &&
                            service.ImplementationInstance == null)
                            collection.AddSingleton(service.ServiceType, obj);
                        else
                            collection.Add(service);
                    }
                })
                .UseStartup<ApiBuilder>();

            IWebHost host = webHostBuilder.Build();

            host.Start();

            return host;
        }

        private static X509Certificate2 GetHttpsCertificate(string certificateFilePath, ICertificateStore store)
        {
            if (store.TryGet(certificateFilePath, out X509Certificate2 certificate))
                return certificate;

            throw new FileLoadException($"Failed to load certificate from path {certificateFilePath}");
        }
    }
}