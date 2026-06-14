using NRules;
using NRules.Fluent;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Infrastructure.Compensation;

/// <summary>
/// <see cref="ICompensationCalculator"/> backed by the NRules engine. The rule set is
/// loaded and compiled once into a thread-safe <see cref="ISessionFactory"/>; each call
/// opens a fresh session, inserts the request fact, fires the rules and collects the
/// resulting <see cref="ComponentResult"/> facts. Registered as a singleton.
/// </summary>
public sealed class NRulesCompensationCalculator : ICompensationCalculator
{
    private readonly ISessionFactory _factory;

    public NRulesCompensationCalculator()
    {
        var repository = new RuleRepository();
        repository.Load(spec => spec
            .From(typeof(NRulesCompensationCalculator).Assembly)
            .Where(metadata => metadata.RuleType.Namespace == typeof(BasicSalaryRule).Namespace));

        _factory = repository.Compile();
    }

    public CompensationBreakdown Calculate(decimal annualBasic, GradeBand grade)
    {
        var session = _factory.CreateSession();
        session.Insert(new CompensationRequest(annualBasic, grade));
        session.Fire();

        var lines = session.Query<ComponentResult>()
            .Select(r => new CompensationLine(r.Name, r.Amount, r.Type))
            .ToList();

        var gross = lines.Where(l => l.Type == ComponentType.Earning).Sum(l => l.Amount);
        var deductions = lines.Where(l => l.Type == ComponentType.Deduction).Sum(l => l.Amount);

        return new CompensationBreakdown(lines, gross, deductions, gross - deductions);
    }
}
