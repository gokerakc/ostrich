using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ostrich.Core.Models;
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

        if (!await context.Accounts.AnyAsync())
        {
            var accounts = new[]
            {
                new Account { Id = Guid.Parse("a1b2c3d4-0001-4000-8000-000000000001"), Name = "TechGadgets", Currency = "USD", Balance = 10000m, Version = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Account { Id = Guid.Parse("a1b2c3d4-0002-4000-8000-000000000002"), Name = "FreshMart", Currency = "EUR", Balance = 10000m, Version = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Account { Id = Guid.Parse("a1b2c3d4-0003-4000-8000-000000000003"), Name = "CloudHost", Currency = "USD", Balance = 10000m, Version = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Account { Id = Guid.Parse("a1b2c3d4-0004-4000-8000-000000000004"), Name = "BookNest", Currency = "GBP", Balance = 10000m, Version = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Account { Id = Guid.Parse("a1b2c3d4-0005-4000-8000-000000000005"), Name = "FitGear", Currency = "USD", Balance = 10000m, Version = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Account { Id = Guid.Parse("a1b2c3d4-0006-4000-8000-000000000006"), Name = "BrewHouse", Currency = "EUR", Balance = 10000m, Version = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Account { Id = Guid.Parse("a1b2c3d4-0007-4000-8000-000000000007"), Name = "GreenEnergy", Currency = "TRY", Balance = 10000m, Version = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Account { Id = Guid.Parse("a1b2c3d4-0008-4000-8000-000000000008"), Name = "Streamly", Currency = "USD", Balance = 10000m, Version = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };
            context.Accounts.AddRange(accounts);
            await context.SaveChangesAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();

        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
