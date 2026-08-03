using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ostrich.Core.Models;
using Ostrich.Core.Redis;
using StackExchange.Redis;

namespace Ostrich.Core.Services;

public class RedisStreamConsumer : IStreamConsumer
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisStreamConsumer> _logger;

    public RedisStreamConsumer(IConnectionMultiplexer redis, ILogger<RedisStreamConsumer> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task CreateConsumerGroupAsync()
    {
        var db = _redis.GetDatabase();
        try
        {
            await db.StreamCreateConsumerGroupAsync(
                RedisStreams.PaymentStream,
                RedisStreams.ConsumerGroup,
                "0-0",
                createStream: true);
            _logger.LogInformation("Consumer group '{Group}' created", RedisStreams.ConsumerGroup);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            _logger.LogInformation("Consumer group '{Group}' already exists", RedisStreams.ConsumerGroup);
        }
    }

    public async Task<IReadOnlyList<StreamMessage>> ReadPaymentsAsync(string consumerName, int count = 10)
    {
        var db = _redis.GetDatabase();
        var entries = await db.StreamReadGroupAsync(
            RedisStreams.PaymentStream,
            RedisStreams.ConsumerGroup,
            consumerName,
            ">",
            count: count);

        var messages = new List<StreamMessage>(entries.Length);
        foreach (var entry in entries)
        {
            var payment = JsonSerializer.Deserialize<Payment>((string)entry.Values.First().Value!);
            if (payment is not null)
            {
                messages.Add(new StreamMessage(entry.Id.ToString(), payment));
            }
        }

        return messages;
    }

    public async Task AcknowledgeAsync(string entryId)
    {
        var db = _redis.GetDatabase();
        await db.StreamAcknowledgeAsync(
            RedisStreams.PaymentStream,
            RedisStreams.ConsumerGroup,
            entryId);
    }
}
