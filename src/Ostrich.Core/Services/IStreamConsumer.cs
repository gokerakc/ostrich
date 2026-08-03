using Ostrich.Core.Models;

namespace Ostrich.Core.Services;

public record StreamMessage(string EntryId, Payment Payment);

public interface IStreamConsumer
{
    Task CreateConsumerGroupAsync();
    Task<IReadOnlyList<StreamMessage>> ReadPaymentsAsync(string consumerName, int count = 10);
    Task AcknowledgeAsync(string entryId);
}
