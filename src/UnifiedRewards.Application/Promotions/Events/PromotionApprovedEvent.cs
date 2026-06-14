using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Common.Interfaces.Integration;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.Promotions.Events;

/// <summary>Raised when a promotion nomination is approved. Published via IEventBus (MediatR).</summary>
public sealed record PromotionApprovedEvent(
    Guid NominationId,
    Guid EmployeeId,
    GradeBand ProposedGrade,
    DateOnly EffectiveDate) : INotification;

/// <summary>Notifies the promoted employee by email — an example event-driven, decoupled reaction.</summary>
public sealed class PromotionApprovedEmailHandler : INotificationHandler<PromotionApprovedEvent>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<PromotionApprovedEmailHandler> _logger;

    public PromotionApprovedEmailHandler(
        IApplicationDbContext db, IEmailService emailService, ILogger<PromotionApprovedEmailHandler> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(PromotionApprovedEvent notification, CancellationToken cancellationToken)
    {
        var employee = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == notification.EmployeeId, cancellationToken);

        if (employee is null)
        {
            _logger.LogWarning("PromotionApproved for unknown employee {EmployeeId}", notification.EmployeeId);
            return;
        }

        await _emailService.SendAsync(
            employee.Email,
            "Congratulations on your promotion!",
            $"You have been promoted to {notification.ProposedGrade}, effective {notification.EffectiveDate:yyyy-MM-dd}.",
            cancellationToken);
    }
}
