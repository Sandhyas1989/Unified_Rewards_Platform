using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnifiedRewards.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ReceiptReference = table.Column<string>(type: "TEXT", nullable: true),
                    ReceiptFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: true),
                    ReceiptContentType = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    OcrText = table.Column<string>(type: "TEXT", nullable: true),
                    OcrConfidence = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    OcrExtractedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ReviewerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DecisionNotes = table.Column<string>(type: "TEXT", nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DecisionAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SettledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PayrollReference = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClaimTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClaimId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    ToStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    ActorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimTransitions_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Claims_EmployeeId",
                table: "Claims",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Status",
                table: "Claims",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimTransitions_ClaimId",
                table: "ClaimTransitions",
                column: "ClaimId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClaimTransitions");

            migrationBuilder.DropTable(
                name: "Claims");
        }
    }
}
