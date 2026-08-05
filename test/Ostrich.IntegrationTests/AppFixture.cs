using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ostrich.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Ostrich.IntegrationTests;

public class AppFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private readonly RedisContainer _redis;
    private WebApplicationFactory<Program> _factory = null!;

    public HttpClient HttpClient => _factory.CreateClient();
    public IServiceProvider Services => _factory.Services;
    public string RedisConnectionString => _redis.GetConnectionString();
    public string PostgresConnectionString => _postgres.GetConnectionString();

    public AppFixture()
    {
        _postgres = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("ostrich_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _redis = new RedisBuilder("redis:7-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
                        ["Redis:ConnectionString"] = _redis.GetConnectionString()
                    });
                });
                builder.UseSetting(WebHostDefaults.EnvironmentKey, "Development");
            });

        using var scope = _factory.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var context = await dbFactory.CreateDbContextAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();

        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
