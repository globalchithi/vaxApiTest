using Microsoft.Extensions.DependencyInjection;

namespace SpecFlowTests.Support;

public class ScenarioServices : IDisposable
{
    public ScenarioServices(ServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public ServiceProvider ServiceProvider { get; }

    public T GetRequiredService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    public void Dispose()
    {
        ServiceProvider.Dispose();
    }
}

