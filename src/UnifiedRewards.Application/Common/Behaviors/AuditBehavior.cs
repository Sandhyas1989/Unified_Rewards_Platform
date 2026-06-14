using MediatR;
using Microsoft.Extensions.Logging;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Domain.Reporting;

namespace UnifiedRewards.Application.Common.Behaviors;

/// <summary>
/// Records an <see cref="AuditEntry"/> for every command (request type ending in "Command"),
/// capturing the acting user and the success/failure outcome. Queries are not audited.
/// </summary>
public sealed class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;

    public AuditBehavior(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        ILogger<AuditBehavior<TRequest, TResponse>> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var action = typeof(TRequest).Name;
        if (!action.EndsWith("Command", StringComparison.Ordinal))
        {
            return await next();
        }

        var entry = new AuditEntry
        {
            Action = action,
            UserId = _currentUser.UserId,
            UserEmail = _currentUser.Email,
            OccurredAtUtc = DateTime.UtcNow
        };

        try
        {
            var response = await next();
            entry.Succeeded = true;
            return response;
        }
        catch (Exception ex)
        {
            entry.Succeeded = false;
            entry.Error = ex.Message;
            throw;
        }
        finally
        {
            try
            {
                _db.AuditEntries.Add(entry);
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                // Auditing must never break the request it is observing.
                _logger.LogError(saveEx, "Failed to persist audit entry for {Action}", action);
            }
        }
    }
}
