using APPLICATION.APPLICATION.CONFIGURATIONS;
using APPLICATION.DOMAIN.DTOS.CONFIGURATION;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;

try
{
    // Preparando builder.
    var builder = WebApplication.CreateBuilder(args);

    // Pegando configura��es do appsettings.json.
    var configurations = builder.Configuration;

    // Pegando configura��es do gateway.json.
    var gatewayConfigurations = builder.Configuration;

    // Pega o appsettings baseado no ambiente em execu��o.
    configurations
         .SetBasePath(builder.Environment.ContentRootPath).AddJsonFile("appsettings.json", false, true).AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true).AddEnvironmentVariables();

    // Pega o gateway.json baseado no ambiente em execu��o.
    gatewayConfigurations
         .SetBasePath(builder.Environment.ContentRootPath).AddJsonFile("gateway.json", false, true).AddJsonFile($"gateway.{builder.Environment.EnvironmentName}.json", true, true).AddEnvironmentVariables();

    /// <summary>
    /// Chamada das configura��es do projeto.
    /// </summary>
    builder.Services
        .AddHttpContextAccessor()
        .Configure<AppSettings>(configurations).AddSingleton<AppSettings>()
        .AddEndpointsApiExplorer()
        .AddOptions()
        .ConfigureLanguage()
        .ConfigureApllicationCookie()
        .ConfigureSwagger(configurations)
        .ConfigureDependencies(configurations, builder.Environment)
        .AddControllers(options =>
         {
             options.EnableEndpointRouting = false;

             options.Filters.Add(new ProducesAttribute("application/json"));

         }).AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);

    // Se for em produ��o executa.
    if (builder.Environment.IsProduction())
    {
        builder.Services
            .ConfigureTelemetry(configurations)
            .ConfigureApplicationInsights(configurations);
    }

    // Continua��o do pipeline...
    builder.Services
        .ConfigureSerilog()
        .ConfigureCors()
        .AddOcelot(gatewayConfigurations);

    // Preparando WebApplication Build.
    var applicationbuilder = builder.Build();

    // Chamada das connfigura��es do WebApplication Build.
    applicationbuilder
        .UseHttpsRedirection()
        .UseDefaultFiles()
        .UseStaticFiles()
        .UseCookiePolicy()
        .UseRouting()
        .UseSwaggerConfigurations(configurations)
        .UseResponseCaching()
        .UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("API GATEWAY");
            });
        })
        .UseOcelot();

    Log.Information($"[LOG INFORMATION] - Inicializando aplica��o [TOOLS.GATEWAY]\n");

    // Iniciando a aplica��o com todas as configura��es j� carregadas.
    applicationbuilder.Run();
}
catch (Exception exception)
{
    Log.Error($"[LOG ERROR] - Ocorreu um erro ao inicializar a aplicacao [TOOLS.GATEWAY] - {exception.Message}\n");
}