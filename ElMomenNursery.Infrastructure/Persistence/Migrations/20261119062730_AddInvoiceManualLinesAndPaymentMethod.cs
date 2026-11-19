using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElMomenNursery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceManualLinesAndPaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Invoices",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "Cash");

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InventoryItemId",
                table: "InvoiceLines",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "InventoryItemId",
                table: "InvoiceLines",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
