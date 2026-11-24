using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElMomenNursery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixEmployeeAdvanceDecimalCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_EmployeeAdvances_Amounts",
                table: "EmployeeAdvances");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmployeeAdvances_Amounts",
                table: "EmployeeAdvances",
                sql: "CAST(\"OriginalAmount\" AS REAL) > 0 AND CAST(\"RemainingAmount\" AS REAL) >= 0 AND CAST(\"RemainingAmount\" AS REAL) <= CAST(\"OriginalAmount\" AS REAL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_EmployeeAdvances_Amounts",
                table: "EmployeeAdvances");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmployeeAdvances_Amounts",
                table: "EmployeeAdvances",
                sql: "\"OriginalAmount\" > 0 AND \"RemainingAmount\" >= 0 AND \"RemainingAmount\" <= \"OriginalAmount\"");
        }
    }
}
