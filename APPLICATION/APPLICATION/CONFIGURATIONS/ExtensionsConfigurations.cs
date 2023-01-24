
using APPLICATION.APPLICATION.CONFIGURATIONS.APPLICATIONINSIGHTS;
using APPLICATION.DOMAIN.CONTRACTS.CONFIGURATIONS;
using APPLICATION.DOMAIN.CONTRACTS.CONFIGURATIONS.APPLICATIONINSIGHTS;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using System.Globalization;

namespace APPLICATION.APPLICATION.CONFIGURATIONS;

public static class ExtensionsConfigurations
{
    public static readonly string HealthCheckEndpoint = "/application/healthcheck";

    private static string _applicationInsightsKey;

    private static TelemetryConfiguration _telemetryConfig;

    private static TelemetryClient _telemetryClient;

    /// <summary>
    /// Configuração de Logs do sistema.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureSerilog(this IServiceCollection services)
    {
        Log.Logger = new LoggerConfiguration()
                                 .MinimumLevel.Debug()
                                 .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                                 .MinimumLevel.Override("System", LogEventLevel.Error)
                                 .Enrich.FromLogContext()
                                 .Enrich.WithEnvironmentUserName()
                                 .Enrich.WithMachineName()
                                 .Enrich.WithProcessId()
                                 .Enrich.WithProcessName()
                                 .Enrich.WithThreadId()
                                 .Enrich.WithThreadName()
                                 .WriteTo.Console()
                                 .WriteTo.ApplicationInsights(_telemetryConfig, TelemetryConverter.Traces, LogEventLevel.Information)
                                 .CreateLogger();
        services
            .AddTransient<ILogWithMetric, LogWithMetric>();

        return services;
    }

    /// <summary>
    /// Configuração de linguagem principal do sistema.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureLanguage(this IServiceCollection services)
    {
        var cultureInfo = new CultureInfo("pt-BR");

        CultureInfo
            .DefaultThreadCurrentCulture = cultureInfo;

        CultureInfo
            .DefaultThreadCurrentUICulture = cultureInfo;

        return services;
    }

    /// <summary>
    /// Configura os cookies da applicação.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureApllicationCookie(this IServiceCollection services)
    {
        return services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;

            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);

            options.SlidingExpiration = true;
        });
    }

    /// <summary>
    /// Configuração de métricas
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var httpContextAccessor = services.BuildServiceProvider().GetService<IHttpContextAccessor>();

        _telemetryConfig = TelemetryConfiguration.CreateDefault();

        _telemetryConfig.ConnectionString = configuration.GetSection("ApplicationInsights:ConnectionStringApplicationInsightsKey").Value;

        _telemetryConfig.TelemetryInitializers.Add(new ApplicationInsightsInitializer(configuration, httpContextAccessor));

        _telemetryClient = new TelemetryClient(_telemetryConfig);

        services
            .AddSingleton<ITelemetryInitializer>(x => new ApplicationInsightsInitializer(configuration, httpContextAccessor))
            .AddSingleton<ITelemetryProxy>(x => new TelemetryProxy(_telemetryClient));

        return services;
    }

    /// <summary>
    /// Configuração de App Insights
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureApplicationInsights(this IServiceCollection services, IConfiguration configuration)
    {
        var metrics = new ApplicationInsightsMetrics(_telemetryClient, _applicationInsightsKey);

        var applicationInsightsServiceOptions = new ApplicationInsightsServiceOptions
        {
            ConnectionString = configuration.GetSection("ApplicationInsights:ConnectionStringApplicationInsightsKey").Value
        };

        services
            .AddApplicationInsightsTelemetry(applicationInsightsServiceOptions)
            .AddTransient(x => metrics)
            .AddTransient<IApplicationInsightsMetrics>(x => metrics);

        return services;
    }

    /// <summary>
    /// Configuração do swagger do sistema.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configurations"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureSwagger(this IServiceCollection services, IConfiguration configurations)
    {
        var apiVersion = configurations.GetValue<string>("SwaggerInfo:ApiVersion"); var apiDescription = configurations.GetValue<string>("SwaggerInfo:ApiDescription"); var uriMyGit = configurations.GetValue<string>("SwaggerInfo:UriMyGit");

        services.AddSwaggerGen(swagger =>
        {
            swagger.EnableAnnotations();

            swagger.SwaggerDoc(apiVersion, new OpenApiInfo
            {
                Version = apiVersion,
                Title = $"{apiDescription} - {apiVersion}",
                Description = apiDescription,

                Contact = new OpenApiContact
                {
                    Name = "HYPER.IO DESENVOLVIMENTOS LTDA",
                    Email = "HYPER.IO@OUTLOOK.COM",
                },
                License = new OpenApiLicense
                {
                    Name = "HYPER.IO LICENSE",

                },
                TermsOfService = new Uri(uriMyGit)
            });
        });

        return services;
    }

    /// <summary>
    /// Configuração das dependencias (Serrvices, Repository, Facades, etc...).
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureDependencies(this IServiceCollection services, IConfiguration configurations, IWebHostEnvironment webHostEnvironment)
    {
        // Se for ambiente de produção executa
        if (webHostEnvironment.IsProduction())
        {
            if (string.IsNullOrEmpty(configurations.GetValue<string>("ApplicationInsights:InstrumentationKey")))
            {
                var argNullEx = new ArgumentNullException("AppInsightsKey não pode ser nulo.", new Exception("Parametro inexistente.")); throw argNullEx;
            }
            else
            {
                _applicationInsightsKey = configurations.GetValue<string>("ApplicationInsights:InstrumentationKey");
            }
        }

        return services;
    }

    /// <summary>
    /// Configuração dos cors aceitos.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureCors(this IServiceCollection services)
    {
        return services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", policy =>
            {
                policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed((host) => true).AllowCredentials();
            });
        });
    }

    /// <summary>
    /// Configuração de uso do swagger do sistema.
    /// </summary>
    /// <param name="application"></param>
    /// <param name="configurations"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseSwaggerConfigurations(this IApplicationBuilder application, IConfiguration configurations)
    {
        var apiVersion = configurations.GetValue<string>("SwaggerInfo:ApiVersion");

        application.UseSwagger(c =>
        {
            c.RouteTemplate = "swagger/{documentName}/swagger.json";
        });

        application
            .UseSwaggerUI(swagger =>
            {
                swagger.SwaggerEndpoint($"/swagger/{apiVersion}/swagger.json", $"{apiVersion}");
            });

        application
            .UseMvcWithDefaultRoute();

        return application;
    }
}
