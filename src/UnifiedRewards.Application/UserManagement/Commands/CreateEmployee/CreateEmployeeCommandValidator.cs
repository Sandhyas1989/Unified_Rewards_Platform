using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Interfaces;

namespace UnifiedRewards.Application.UserManagement.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator(IApplicationDbContext db)
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(async (email, ct) =>
                !await db.Users.AnyAsync(u => u.Email == email.Trim().ToLower(), ct))
            .WithMessage("A user with this email already exists.");

        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);

        RuleFor(x => x.Grade).NotEmpty().MaximumLength(20);

        RuleFor(x => x.DateOfJoining).NotEqual(default(DateOnly));
    }
}
