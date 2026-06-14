using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using UnifiedRewards.Domain.Benefits;
using UnifiedRewards.Domain.Claims;
using UnifiedRewards.Domain.Compensation;
using UnifiedRewards.Domain.Payroll;
using UnifiedRewards.Domain.Promotions;
using UnifiedRewards.Domain.Reporting;
using UnifiedRewards.Domain.UserManagement;

namespace UnifiedRewards.Application.Common.Interfaces;

/// <summary>
/// Persistence seam the Application layer depends on. Implemented by the
/// EF Core DbContext in Infrastructure, so handlers never reference a provider.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }

    DbSet<BenefitPlan> BenefitPlans { get; }

    DbSet<BenefitEnrollment> BenefitEnrollments { get; }

    DbSet<CompensationStructure> CompensationStructures { get; }

    DbSet<CompensationComponent> CompensationComponents { get; }

    DbSet<Claim> Claims { get; }

    DbSet<ClaimTransition> ClaimTransitions { get; }

    DbSet<Payslip> Payslips { get; }

    DbSet<SettlementRequest> SettlementRequests { get; }

    DbSet<PromotionNomination> PromotionNominations { get; }

    DbSet<AuditEntry> AuditEntries { get; }

    /// <summary>Exposes the underlying database facade (e.g. to detect the active provider).</summary>
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
