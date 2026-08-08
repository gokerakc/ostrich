using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ostrich.Application.Services;
using Ostrich.Core.Json;
using Ostrich.Core.Models;
using Ostrich.Core.Redis;
using Ostrich.Core.Services;
using Ostrich.Infrastructure.Data;
using Ostrich.Infrastructure.Services;
using Ostrich.Worker;
using StackExchange.Redis;

namespace Ostrich.IntegrationTests;

public class PaymentProcessorTests : IClassFixture<AppFixture>, IAsyncDisposable
{
    private readonly AppFixture _fixture;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IConnectionMultiplexer _redis;

    public PaymentProcessorTests(AppFixture fixture)
    {
        _fixture = fixture;
        _dbFactory = fixture.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        _redis = ConnectionMultiplexer.Connect(fixture.RedisConnectionString);
    }

    [Fact]
    public async Task ProcessMessage_StoresPaymentInDatabase()
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(), ExternalId = Guid.NewGuid(),
            Amount = 500m, Currency = "USD",
            Merchant = "WorkerMerchant"
        };

        var host = BuildWorkerHost();
        var cts = new CancellationTokenSource();

        try
        {
            await host.StartAsync(cts.Token);

            var json = JsonSerializer.Serialize(payment, PaymentJson.Options);
            var db = _redis.GetDatabase();
            await db.StreamAddAsync(
                RedisStreams.PaymentStream,
                [new NameValueEntry("payment", json)]);

            await Task.Delay(3000, cts.Token);

            await cts.CancelAsync();
            await host.StopAsync(CancellationToken.None);

            await using var context = await _dbFactory.CreateDbContextAsync();
            var saved = await context.Payments.FindAsync(payment.Id);
            Assert.NotNull(saved);
            Assert.Equal("Processed", saved!.Status);
            Assert.Equal(payment.Amount, saved.Amount);
            Assert.Equal(payment.Currency, saved.Currency);
        }
        finally
        {
            if (!cts.IsCancellationRequested)
                await cts.CancelAsync();

            host.Dispose();
        }
    }

    private IHost BuildWorkerHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = _fixture.PostgresConnectionString,
                    ["Redis:ConnectionString"] = _fixture.RedisConnectionString
                });
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<PaymentMetrics>();
                services.AddSingleton<IConnectionMultiplexer>(_ => _redis);
                services.AddDbContextFactory<AppDbContext>(options =>
                    options.UseNpgsql(_fixture.PostgresConnectionString));
                services.AddSingleton<IPaymentRepository, PaymentRepository>();
                services.AddSingleton<IStreamConsumer, RedisStreamConsumer>();
                services.AddSingleton<IPaymentService, PaymentService>();
                services.AddHostedService<PaymentProcessor>();
            })
            .ConfigureLogging(logging => logging.ClearProviders())
            .Build();
    }

    public async ValueTask DisposeAsync()
    {
        _redis.Dispose();
    }
}
