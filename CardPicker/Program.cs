using CardPicker.Options;
using CardPicker.Services;
using Serilog;
using Serilog.Events;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace CardPicker;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureLogging(builder);
            ConfigureServices(builder);

            var app = builder.Build();

            ConfigurePipeline(app);

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "CardPicker terminated unexpectedly during startup.");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.UseSerilog(
            (context, services, loggerConfiguration) =>
            {
                var logDirectoryPath = Path.Combine(context.HostingEnvironment.ContentRootPath, "logs");
                Directory.CreateDirectory(logDirectoryPath);

                loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
                    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                    .WriteTo.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(
                        path: Path.Combine(logDirectoryPath, "cardpicker-.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 14,
                        shared: true,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}");
            });
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddRazorPages();
        builder.Services.AddWebEncoders(
            options =>
            {
                options.TextEncoderSettings = new TextEncoderSettings(
                    UnicodeRanges.BasicLatin,
                    UnicodeRanges.Bopomofo,
                    UnicodeRanges.CjkSymbolsandPunctuation,
                    UnicodeRanges.CjkUnifiedIdeographs,
                    UnicodeRanges.HalfwidthandFullwidthForms);
            });

        builder.Services.AddHttpsRedirection(
            options =>
            {
                options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
            });

        builder.Services.AddHsts(
            options =>
            {
                options.MaxAge = TimeSpan.FromDays(180);
                options.IncludeSubDomains = true;
            });

        builder.Services
            .AddOptions<CardStorageOptions>()
            .Bind(builder.Configuration.GetSection(CardStorageOptions.SectionName))
            .Validate(ValidateCardStorageOptions, "Card storage configuration is invalid.")
            .ValidateOnStart();

        builder.Services.AddSingleton<IMealCardRepository, JsonMealCardRepository>();
        builder.Services.AddSingleton<IRandomIndexProvider, CryptoRandomIndexProvider>();
        builder.Services.AddScoped<IMealCardService, MealCardService>();
        builder.Services.AddScoped<IMealDrawService, MealDrawService>();
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
            app.Use(ApplyProductionSecurityHeaders);
        }

        app.UseSerilogRequestLogging();
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapRazorPages()
            .WithStaticAssets();
    }

    private static bool ValidateCardStorageOptions(CardStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();
        return true;
    }

    private static Task ApplyProductionSecurityHeaders(HttpContext context, Func<Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        context.Response.OnStarting(
            () =>
            {
                var headers = context.Response.Headers;

                headers["Content-Security-Policy"] =
                    "default-src 'self'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'; " +
                    "img-src 'self' data:; object-src 'none'; script-src 'self' 'unsafe-inline'; " +
                    "style-src 'self'; upgrade-insecure-requests";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Permissions-Policy"] = "camera=(), geolocation=(), microphone=()";

                return Task.CompletedTask;
            });

        return next();
    }
}
