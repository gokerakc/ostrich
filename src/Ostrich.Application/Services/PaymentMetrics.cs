using System.Diagnostics.Metrics;

namespace Ostrich.Application.Services;

public class PaymentMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _paymentsProcessed;
    private readonly Histogram<double> _paymentProcessingDuration;
    private readonly Counter<long> _paymentsRefunded;
    private readonly Counter<long> _paymentsCancelled;

    public PaymentMetrics()
    {
        _meter = new Meter("Ostrich.Payments", "1.0.0");

        _paymentsProcessed = _meter.CreateCounter<long>(
            "payments.processed",
            description: "Number of payments successfully processed");

        _paymentProcessingDuration = _meter.CreateHistogram<double>(
            "payments.processing.duration",
            unit: "ms",
            description: "Time taken to process and persist a payment");

        _paymentsRefunded = _meter.CreateCounter<long>(
            "payments.refunded",
            description: "Number of refund requests that succeeded");

        _paymentsCancelled = _meter.CreateCounter<long>(
            "payments.cancelled",
            description: "Number of pending payments cancelled via refund");
    }

    public void PaymentProcessed(double durationMs)
    {
        _paymentsProcessed.Add(1);
        _paymentProcessingDuration.Record(durationMs);
    }

    public void PaymentRefunded() => _paymentsRefunded.Add(1);

    public void PaymentCancelled() => _paymentsCancelled.Add(1);
}
