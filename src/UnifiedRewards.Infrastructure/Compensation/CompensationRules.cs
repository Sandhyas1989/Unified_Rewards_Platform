using NRules.Fluent.Dsl;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Infrastructure.Compensation;

// Each rule matches the single CompensationRequest fact and inserts one ComponentResult.
// The set is intentionally simple/declarative — new pay rules are added as new classes
// without touching the calculator or handlers.

/// <summary>Basic pay — the input amount, surfaced as the first earning line.</summary>
public sealed class BasicSalaryRule : Rule
{
    public override void Define()
    {
        CompensationRequest req = default!;
        When().Match(() => req);
        Then().Do(ctx => ctx.Insert(new ComponentResult("Basic", req.AnnualBasic, ComponentType.Earning)));
    }
}

/// <summary>House Rent Allowance = 40% of basic.</summary>
public sealed class HouseRentAllowanceRule : Rule
{
    public override void Define()
    {
        CompensationRequest req = default!;
        When().Match(() => req);
        Then().Do(ctx => ctx.Insert(
            new ComponentResult("House Rent Allowance", Math.Round(req.AnnualBasic * 0.40m, 2), ComponentType.Earning)));
    }
}

/// <summary>Special Allowance = 25% of basic.</summary>
public sealed class SpecialAllowanceRule : Rule
{
    public override void Define()
    {
        CompensationRequest req = default!;
        When().Match(() => req);
        Then().Do(ctx => ctx.Insert(
            new ComponentResult("Special Allowance", Math.Round(req.AnnualBasic * 0.25m, 2), ComponentType.Earning)));
    }
}

/// <summary>Performance Bonus — rate driven by the grade band.</summary>
public sealed class PerformanceBonusRule : Rule
{
    public override void Define()
    {
        CompensationRequest req = default!;
        When().Match(() => req);
        // Rule actions are compiled to expression trees, so the rate switch lives in a helper method.
        Then().Do(ctx => ctx.Insert(
            new ComponentResult("Performance Bonus", BonusAmount(req), ComponentType.Earning)));
    }

    private static decimal BonusAmount(CompensationRequest req)
    {
        var rate = req.Grade switch
        {
            GradeBand.Junior => 0.08m,
            GradeBand.Mid => 0.12m,
            GradeBand.Senior => 0.18m,
            GradeBand.Lead => 0.25m,
            _ => 0m
        };
        return Math.Round(req.AnnualBasic * rate, 2);
    }
}

/// <summary>Provident Fund deduction = 12% of basic.</summary>
public sealed class ProvidentFundRule : Rule
{
    public override void Define()
    {
        CompensationRequest req = default!;
        When().Match(() => req);
        Then().Do(ctx => ctx.Insert(
            new ComponentResult("Provident Fund", Math.Round(req.AnnualBasic * 0.12m, 2), ComponentType.Deduction)));
    }
}

/// <summary>Professional Tax — flat annual deduction.</summary>
public sealed class ProfessionalTaxRule : Rule
{
    public override void Define()
    {
        CompensationRequest req = default!;
        When().Match(() => req);
        Then().Do(ctx => ctx.Insert(
            new ComponentResult("Professional Tax", 2400m, ComponentType.Deduction)));
    }
}
