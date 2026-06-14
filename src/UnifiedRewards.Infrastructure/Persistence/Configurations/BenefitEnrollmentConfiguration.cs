using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnifiedRewards.Domain.Benefits;

namespace UnifiedRewards.Infrastructure.Persistence.Configurations;

public sealed class BenefitEnrollmentConfiguration : IEntityTypeConfiguration<BenefitEnrollment>
{
    public void Configure(EntityTypeBuilder<BenefitEnrollment> builder)
    {
        builder.ToTable("BenefitEnrollments");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status).HasConversion<int>();

        builder.HasIndex(e => new { e.EmployeeId, e.BenefitPlanId });

        // EmployeeId references Users.Id but is kept as a plain FK-less id to avoid
        // coupling the Benefits aggregate to the User TPH table.
    }
}
