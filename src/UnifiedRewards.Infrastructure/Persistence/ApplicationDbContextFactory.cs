using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UnifiedRewards.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by the EF Core tools (migrations / scaffolding).
/// Lets `dotnet ef` build the context without booting the API host or running seeding.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=unifiedrewards.db")
            .Options;

        return new ApplicationDbContext(options);
    }
}
