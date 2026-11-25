using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElMomenNursery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollAttendanceAndInventoryCategoryTree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LateDeduction",
                table: "PayrollRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "LateMinutes",
                table: "PayrollRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ProratedSalary",
                table: "PayrollRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ParentCategoryId",
                table: "InventoryCategories",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCategories_ParentCategoryId",
                table: "InventoryCategories",
                column: "ParentCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryCategories_InventoryCategories_ParentCategoryId",
                table: "InventoryCategories",
                column: "ParentCategoryId",
                principalTable: "InventoryCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryCategories_InventoryCategories_ParentCategoryId",
                table: "InventoryCategories");

            migrationBuilder.DropIndex(
                name: "IX_InventoryCategories_ParentCategoryId",
                table: "InventoryCategories");

            migrationBuilder.DropColumn(
                name: "LateDeduction",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "LateMinutes",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "ProratedSalary",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "ParentCategoryId",
                table: "InventoryCategories");
        }
    }
}
