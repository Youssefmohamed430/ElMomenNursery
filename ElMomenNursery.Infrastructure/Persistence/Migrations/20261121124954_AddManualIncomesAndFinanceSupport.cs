using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElMomenNursery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManualIncomesAndFinanceSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManualIncomes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    IncomeDate = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: true),
                    RecordedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualIncomes", x => x.Id);
                    table.CheckConstraint("CK_ManualIncomes_AmountPositive", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_ManualIncomes_UserAccounts_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManualIncomes_IncomeDate",
                table: "ManualIncomes",
                column: "IncomeDate");

            migrationBuilder.CreateIndex(
                name: "IX_ManualIncomes_RecordedByUserId",
                table: "ManualIncomes",
                column: "RecordedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManualIncomes");
        }
    }
}
