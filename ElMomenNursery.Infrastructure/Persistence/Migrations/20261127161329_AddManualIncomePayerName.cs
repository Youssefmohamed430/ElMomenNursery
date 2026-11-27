using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElMomenNursery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManualIncomePayerName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayerName",
                table: "ManualIncomes",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayerName",
                table: "ManualIncomes");
        }
    }
}
