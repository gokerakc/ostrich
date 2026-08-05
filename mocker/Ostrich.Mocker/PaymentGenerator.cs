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

    private static readonly string[] Merchants =
    [
        "TechGadgets", "FreshMart", "CloudHost", "BookNest",
        "FitGear", "BrewHouse", "GreenEnergy", "Streamly"
    ];

    private static readonly string[] Currencies = ["USD", "EUR", "GBP", "TRY"];

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
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                Amount = Math.Round((decimal)(Random.Shared.NextDouble() * 1000 + 1), 2),
                Currency = Currencies[Random.Shared.Next(Currencies.Length)],
                Merchant = Merchants[Random.Shared.Next(Merchants.Length)],
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
