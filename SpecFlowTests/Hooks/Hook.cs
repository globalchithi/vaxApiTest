using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpecFlowTests.Drivers;
using SpecFlowTests.Support;
using TechTalk.SpecFlow;
using VaxCareApiTests.Services;

namespace SpecFlowTests.Hooks;

[Binding]
public class Hooks
{
    private readonly ScenarioContext _scenarioContext;
    private TestContext? _testContext;
    private ScenarioServices? _scenarioServices;

    public Hooks(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [BeforeScenario(Order = 0)]
    public void InitializeScenarioContext()
    {
        var configuration = BuildConfiguration();
        _scenarioContext.Set(configuration);

        var httpClient = CreateHttpClient(configuration);
        _testContext = new TestContext(httpClient);
        _scenarioContext.Set(_testContext);

        var checkoutContext = new AppointmentCheckoutContext();
        _scenarioContext.Set(checkoutContext);
        _scenarioContext.Set(new AppointmentCheckoutDriver(_testContext, checkoutContext));

        var appointmentCreationContext = new AppointmentCreationContext();
        _scenarioContext.Set(appointmentCreationContext);
        _scenarioContext.Set(new AppointmentCreationDriver(_testContext, appointmentCreationContext));

        var appointmentSyncContext = new AppointmentSyncContext();
        _scenarioContext.Set(appointmentSyncContext);

        var insuranceByStateContext = new InsuranceByStateContext();
        _scenarioContext.Set(insuranceByStateContext);

        var usersPartnerLevelContext = new UsersPartnerLevelContext();
        _scenarioContext.Set(usersPartnerLevelContext);

        var locationDataContext = new LocationDataContext();
        _scenarioContext.Set(locationDataContext);

        var services = BuildServiceProvider(configuration, httpClient);
        _scenarioServices = new ScenarioServices(services);
        _scenarioContext.Set(_scenarioServices);
    }

    [AfterScenario(Order = 100)]
    public void DisposeScenarioResources()
    {
        _testContext?.HttpClient.Dispose();
        _scenarioServices?.Dispose();
    }

    private static IConfiguration BuildConfiguration()
    {
        var environment = Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") ?? "Development";

        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
    }

    private static HttpClient CreateHttpClient(IConfiguration configuration)
    {
        var insecureHttps = bool.TryParse(configuration["ApiConfiguration:InsecureHttps"], out var insecure) && insecure;

        var handler = new HttpClientHandler();
        if (insecureHttps)
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        var baseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? configuration["ApiConfiguration:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("API base URL is not configured. Set ApiConfiguration:BaseUrl or the API_BASE_URL environment variable.");
        }

        var client = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = new Uri(baseUrl)
        };

        if (int.TryParse(configuration["ApiConfiguration:Timeout"], out var timeoutMs) && timeoutMs > 0)
        {
            client.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
        }

        ApplyDefaultHeaders(client, configuration);
        return client;
    }

    private static void ApplyDefaultHeaders(HttpClient client, IConfiguration configuration)
    {
        var headersSection = configuration.GetSection("Headers");
        foreach (var header in headersSection.GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(header.Value) && !client.DefaultRequestHeaders.Contains(header.Key))
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }
    }

    private static ServiceProvider BuildServiceProvider(IConfiguration configuration, HttpClient httpClient)
    {
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddSingleton(httpClient);
        services.AddTransient<HttpClientService>();
        services.AddTransient<TestUtilities>();
        return services.BuildServiceProvider();
    }
}
