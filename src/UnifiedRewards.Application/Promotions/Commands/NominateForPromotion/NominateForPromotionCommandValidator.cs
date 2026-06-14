using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.Promotions.Commands.NominateForPromotion;

public sealed class NominateForPromotionCommandValidator : AbstractValidator<NominateForPromotionCommand>
{
    public NominateForPromotionCommandValidator(IApplicationDbContext db)
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty()
            .MustAsync(async (id, ct) => await db.Users.AnyAsync(u => u.Id == id, ct))
            .WithMessage("The nominated employee does not exist.");

        RuleFor(x => x.Justification).NotEmpty().MaximumLength(2000);

        RuleFor(x => x.ProposedGrade).IsInEnum();

        RuleFor(x => x)
            .Must(x => x.ProposedGrade > x.CurrentGrade)
            .WithName(nameof(NominateForPromotionCommand.ProposedGrade))
            .WithMessage("A promotion must propose a higher grade band than the current one.");

        // Only one open nomination per employee at a time.
        RuleFor(x => x.EmployeeId)
            .MustAsync(async (id, ct) => !await db.PromotionNominations.AnyAsync(
                n => n.EmployeeId == id &&
                     (n.Status == PromotionStatus.Nominated || n.Status == PromotionStatus.UnderReview), ct))
            .WithMessage("This employee already has an open promotion nomination.");
    }
}
