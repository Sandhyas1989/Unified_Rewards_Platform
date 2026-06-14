using NRules.Fluent.Dsl;
using UnifiedRewards.CompensationRules.Domain;

namespace UnifiedRewards.CompensationRules.Rules;

// Facts
public sealed record CompensationRequest(decimal AnnualBasic, GradeBand Grade);
public sealed record ComponentResult(string Name, decimal Amount, ComponentType Type);

// Each rule matches the request fact and inserts one ComponentResult. New pay rules = new classes.
public sealed class BasicSalaryRule : Rule
{
    public override void Define()
    {
        CompensationRequest req = default!;
        When().Match(() => req);
        Then().Do(ctx => ctx.Insert(new ComponentResult("Basic", req.AnnualBasic, ComponentType.Earning)));
    }
}

public sealed class HouseRentAllowanceRule : Rule
{
    public override void Define()
    {
        CompensationRequest req = default!;
        When().Match(() => req);
        Then().Do(ctx => ctx.Insert(new ComponentResult("House Rent Allowance", Math.Round(req.AnnualBasic * 0.40m, 2), ComponentType.Earning)));
    }
}

public sealed class SpecialAllowanceRule : Rule
{
    public override void Define()
    {
        CompensationRequest req = default!;
        When().Match(() => req);
        Then().Do(ctx => ctx.Insert(new ComponentResult("Special Allowance", Math.Round(req.AnnualBasic * 0.25m, 2), ComponentType.Earning)));
    }
}

public sealed class PerformanceBonusRule : Rule
{
    public override void Define()
    {
        CompensationRequest req = default!;
        When().Match(() => req);
        // Rule actions compile to expression trees, so the rate switch lives in a helper method.
        Then().Do(ctx => ctx.Insert(new ComponentResult("Performance Bonus", BonusAmount(req), ComponentType.Earning)));
    }

    private static decimal BonusAmount(CompensationRequest req)
    {
        var rate = req.Grade switch
        {
            GradeBand.Junior => 0.08m,
            GradeBand.Mid => 0.12m,
            GradeBand.Senior => 0.18m,
            GradeBand.Lead => 0.25m,
            _ => 0m,
        };
        return Math.Round(req.AnnualBasic * rate, 2);
    }
}

public sealed class ProvidentFundRule : Rule
{
    public override void Define()
    {
        CompensationRequest req = default!;
        When().Match(() => req);
        Then().Do(ctx => ctx.Insert(new ComponentResult("Provident Fund", Math.Round(req.AnnualBasic * 0.12m, 2), ComponentType.Deduction)));
    }
}

public sealed class ProfessionalTaxRule : Rule
{
    public override void Define()
    {
        CompensationRequest req = default!;
        When().Match(() => req);
        Then().Do(ctx => ctx.Insert(new ComponentResult("Professional Tax", 2400m, ComponentType.Deduction)));
    }
}
