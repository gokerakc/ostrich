using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ostrich.Core.Models;
using Ostrich.Infrastructure.Data;
using Ostrich.Mocker;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<GeneratorOptions>(
    builder.Configuration.GetSection("Generator"));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetValue<string>("Redis:ConnectionString")!));

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHostedService<PaymentGenerator>();

var host = builder.Build();

await using (var scope = host.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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

await host.RunAsync();
