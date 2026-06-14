using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Domain.Benefits;
using UnifiedRewards.Domain.Enums;
using UnifiedRewards.Domain.UserManagement;

namespace UnifiedRewards.Infrastructure.Persistence;

/// <summary>Seeds one user per role for local development (password: Password123!).</summary>
public static class ApplicationDbContextSeeder
{
    public const string DefaultPassword = "Password123!";

    public static async Task SeedAsync(
        ApplicationDbContext db,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken = default)
    {
        await SeedUsersAsync(db, passwordHasher, cancellationToken);
        await SeedBenefitPlansAsync(db, cancellationToken);
    }

    private static async Task SeedUsersAsync(
        ApplicationDbContext db,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var hash = passwordHasher.Hash(DefaultPassword);

        db.Users.AddRange(
            new HrAdmin { FullName = "Hannah HR", Email = "hr@urp.local", PasswordHash = hash },
            new FinanceUser { FullName = "Frank Finance", Email = "finance@urp.local", PasswordHash = hash },
            new Manager
            {
                FullName = "Mary Manager",
                Email = "manager@urp.local",
                PasswordHash = hash,
                Grade = "M3",
                DateOfJoining = new DateOnly(2020, 1, 15)
            },
            new Employee
            {
                FullName = "Ed Employee",
                Email = "employee@urp.local",
                PasswordHash = hash,
                Grade = "E2",
                DateOfJoining = new DateOnly(2022, 6, 1)
            });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedBenefitPlansAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        if (await db.BenefitPlans.AnyAsync(cancellationToken))
        {
            return;
        }

        db.BenefitPlans.AddRange(
            new BenefitPlan
            {
                Name = "Comprehensive Health Insurance",
                Description = "Family floater medical cover up to 5 lakh.",
                Category = BenefitCategory.Insurance,
                MonthlyCost = 1200m
            },
            new BenefitPlan
            {
                Name = "Gym & Wellness Membership",
                Description = "Reimbursement for gym, yoga and wellness programmes.",
                Category = BenefitCategory.Wellness,
                MonthlyCost = 500m
            },
            new BenefitPlan
            {
                Name = "Meal Card",
                Description = "Tax-exempt meal allowance loaded monthly.",
                Category = BenefitCategory.Food,
                MonthlyCost = 2200m
            });

        await db.SaveChangesAsync(cancellationToken);
    }
}
