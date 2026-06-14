using System.Threading.Channels;
using UnifiedRewards.Application.Common.Interfaces;

namespace UnifiedRewards.Infrastructure.Payroll;

/// <summary>
/// In-process settlement queue over an unbounded <see cref="Channel{T}"/>. Registered as a
/// singleton; the background worker consumes <see cref="Reader"/>. (Azure Service Bus in cloud.)
/// </summary>
public sealed class SettlementChannelQueue : ISettlementQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ChannelReader<Guid> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(Guid settlementId, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(settlementId, cancellationToken);
}
