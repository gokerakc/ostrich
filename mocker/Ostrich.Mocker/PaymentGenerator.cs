using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ostrich.Core.Json;
using Ostrich.Core.Models;
using Ostrich.Core.Redis;
using StackExchange.Redis;

namespace Ostrich.Mocker;

public class PaymentGenerator : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<PaymentGenerator> _logger;
    private readonly GeneratorOptions _options;

    private static readonly (Guid id, string name, string currency)[] Merchants =
    [
        (Guid.Parse("a1b2c3d4-0001-4000-8000-000000000001"), "TechGadgets", "USD"),
        (Guid.Parse("a1b2c3d4-0002-4000-8000-000000000002"), "FreshMart", "EUR"),
        (Guid.Parse("a1b2c3d4-0003-4000-8000-000000000003"), "CloudHost", "USD"),
        (Guid.Parse("a1b2c3d4-0004-4000-8000-000000000004"), "BookNest", "GBP"),
        (Guid.Parse("a1b2c3d4-0005-4000-8000-000000000005"), "FitGear", "USD"),
        (Guid.Parse("a1b2c3d4-0006-4000-8000-000000000006"), "BrewHouse", "EUR"),
        (Guid.Parse("a1b2c3d4-0007-4000-8000-000000000007"), "GreenEnergy", "TRY"),
        (Guid.Parse("a1b2c3d4-0008-4000-8000-000000000008"), "Streamly", "USD")
    ];

    public PaymentGenerator(
        IConnectionMultiplexer redis,
        ILogger<PaymentGenerator> logger,
        IOptions<GeneratorOptions> options)
    {
        _redis = redis;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentGenerator started. Interval: {Interval}s", _options.IntervalSeconds);

        var db = _redis.GetDatabase();

        while (!stoppingToken.IsCancellationRequested)
        {
            var merchant = Merchants[Random.Shared.Next(Merchants.Length)];

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                ExternalId = Guid.NewGuid(),
                Amount = Math.Round((decimal)(Random.Shared.NextDouble() * 1000 + 1), 2),
                Currency = merchant.currency,
                Merchant = merchant.name,
                AccountId = merchant.id,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(payment, PaymentJson.Options);
            var messageId = await db.StreamAddAsync(
                RedisStreams.PaymentStream,
                "payment",
                json);

            _logger.LogInformation(
                "Published payment {Id} | {Amount} {Currency} | {Merchant} | Stream ID: {MessageId}",
                payment.Id, payment.Amount, payment.Currency, payment.Merchant, messageId);

            await Task.Delay(TimeSpan.FromSeconds(_options.IntervalSeconds), stoppingToken);
        }
    }
}

public class GeneratorOptions
{
    public int IntervalSeconds { get; set; } = 2;
}
