using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ostrich.Application.Services;
using Ostrich.Core.Services;
using StackExchange.Redis;

namespace Ostrich.Worker;

public class PaymentProcessor : BackgroundService
{
    private readonly IStreamConsumer _streamConsumer;
    private readonly IPaymentService _paymentService;
    private readonly PaymentMetrics _metrics;
    private readonly ILogger<PaymentProcessor> _logger;
    private readonly string _consumerName;

    public PaymentProcessor(
        IStreamConsumer streamConsumer,
        IPaymentService paymentService,
        PaymentMetrics metrics,
        ILogger<PaymentProcessor> logger)
    {
        _streamConsumer = streamConsumer;
        _paymentService = paymentService;
        _metrics = metrics;
        _logger = logger;
        _consumerName = $"worker-{Guid.NewGuid():N}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _streamConsumer.CreateConsumerGroupAsync();
        _logger.LogInformation("PaymentProcessor started. Consumer: {Consumer}", _consumerName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = await _streamConsumer.ReadPaymentsAsync(_consumerName);

                foreach (var message in messages)
                {
                    await ProcessMessageAsync(message, stoppingToken);
                }
            }
            catch (RedisTimeoutException)
            {
                await Task.Delay(1000, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error reading stream");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(StreamMessage message, CancellationToken ct)
    {
        var payment = message.Payment;
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "Processing payment {Id} | {Amount} {Currency} | {Merchant}",
            payment.Id, payment.Amount, payment.Currency, payment.Merchant);

        await _paymentService.ProcessPaymentAsync(payment, ct);
        await _streamConsumer.AcknowledgeAsync(message.EntryId);

        sw.Stop();
        _metrics.PaymentProcessed(sw.Elapsed.TotalMilliseconds);

        _logger.LogInformation("Payment {Id} processed and stored", payment.Id);
    }
}
