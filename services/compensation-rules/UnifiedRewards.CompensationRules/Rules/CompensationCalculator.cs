using NRules;
using NRules.Fluent;
using UnifiedRewards.CompensationRules.Domain;

namespace UnifiedRewards.CompensationRules.Rules;

public sealed record CompensationLine(string Name, decimal Amount, ComponentType Type);
public sealed record CompensationBreakdown(IReadOnlyList<CompensationLine> Lines, decimal GrossAnnual, decimal TotalDeductions, decimal NetAnnual);

public interface ICompensationCalculator
{
    CompensationBreakdown Calculate(decimal annualBasic, GradeBand grade);
}

// NRules-backed calculator: rules compiled once into a thread-safe session factory; a fresh session
// per call. Ported from the monolith's Compensation module.
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
