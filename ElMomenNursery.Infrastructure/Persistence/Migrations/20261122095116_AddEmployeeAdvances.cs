using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElMomenNursery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeAdvances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdvanceDeduction",
                table: "PayrollRecords",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsCalculated",
                table: "PayrollRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EmployeeAdvances",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    AdvanceDate = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAdvances", x => x.Id);
                    table.CheckConstraint("CK_EmployeeAdvances_Amounts", "\"OriginalAmount\" > 0 AND \"RemainingAmount\" >= 0 AND \"RemainingAmount\" <= \"OriginalAmount\"");
                    table.ForeignKey(
                        name: "FK_EmployeeAdvances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAdvanceRepayments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EmployeeAdvanceId = table.Column<string>(type: "TEXT", nullable: false),
                    PayrollRecordId = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    RepaymentDate = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAdvanceRepayments", x => x.Id);
                    table.CheckConstraint("CK_EmployeeAdvanceRepayments_Amount", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_EmployeeAdvanceRepayments_EmployeeAdvances_EmployeeAdvanceId",
                        column: x => x.EmployeeAdvanceId,
                        principalTable: "EmployeeAdvances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeAdvanceRepayments_PayrollRecords_PayrollRecordId",
                        column: x => x.PayrollRecordId,
                        principalTable: "PayrollRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdvanceRepayments_EmployeeAdvanceId_PayrollRecordId",
                table: "EmployeeAdvanceRepayments",
                columns: new[] { "EmployeeAdvanceId", "PayrollRecordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdvanceRepayments_PayrollRecordId",
                table: "EmployeeAdvanceRepayments",
                column: "PayrollRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdvances_Employee_Date",
                table: "EmployeeAdvances",
                columns: new[] { "EmployeeId", "AdvanceDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeAdvanceRepayments");

            migrationBuilder.DropTable(
                name: "EmployeeAdvances");

            migrationBuilder.DropColumn(
                name: "AdvanceDeduction",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "IsCalculated",
                table: "PayrollRecords");
        }
    }
}
