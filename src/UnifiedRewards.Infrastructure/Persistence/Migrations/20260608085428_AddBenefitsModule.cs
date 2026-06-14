using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnifiedRewards.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBenefitsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BenefitPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    MonthlyCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BenefitEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BenefitPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CoverageStartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CancelledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenefitEnrollments_BenefitPlans_BenefitPlanId",
                        column: x => x.BenefitPlanId,
                        principalTable: "BenefitPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BenefitEnrollments_BenefitPlanId",
                table: "BenefitEnrollments",
                column: "BenefitPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_BenefitEnrollments_EmployeeId_BenefitPlanId",
                table: "BenefitEnrollments",
                columns: new[] { "EmployeeId", "BenefitPlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_BenefitPlans_Name",
                table: "BenefitPlans",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenefitEnrollments");

            migrationBuilder.DropTable(
                name: "BenefitPlans");
        }
    }
}
