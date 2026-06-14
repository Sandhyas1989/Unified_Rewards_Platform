using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnifiedRewards.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PromotionNominations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NominatedById = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrentGrade = table.Column<int>(type: "INTEGER", nullable: false),
                    ProposedGrade = table.Column<int>(type: "INTEGER", nullable: false),
                    Justification = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DecisionNotes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    EffectiveDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    NominatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DecisionAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionNominations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionNominations_EmployeeId",
                table: "PromotionNominations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionNominations_Status",
                table: "PromotionNominations",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromotionNominations");
        }
    }
}
