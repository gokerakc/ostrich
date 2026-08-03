namespace Ostrich.Core.Redis;

public static class RedisStreams
{
    public const string PaymentStream = "payments:stream";
    public const string ConsumerGroup = "payment-workers";
}
