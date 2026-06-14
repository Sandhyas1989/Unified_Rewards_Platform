using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnifiedRewards.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Payslips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    GrossMonthly = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDeductionsMonthly = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetMonthly = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CompensationStructureId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payslips", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SettlementRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Attempts = table.Column<int>(type: "INTEGER", nullable: false),
                    PayrollConfirmation = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_EmployeeId_Year_Month",
                table: "Payslips",
                columns: new[] { "EmployeeId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SettlementRequests_EmployeeId",
                table: "SettlementRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementRequests_Reference",
                table: "SettlementRequests",
                column: "Reference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payslips");

            migrationBuilder.DropTable(
                name: "SettlementRequests");
        }
    }
}
