using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElMomenNursery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAbsenceFineIdempotencyAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AppSettings",
                columns: new[] { "Key", "Value", "Description" },
                values: new object[,]
                {
                    { "Attendance.SubscriptionAlertAfterDays", "3", "عدد الأيام بعد بداية الاشتراك لإظهار تنبيه المتبقي." },
                    { "Attendance.AbsenceFineAfterDays", "15", "عدد أيام الغياب لعرض تنبيه الغرامة." },
                    { "Attendance.AbsenceFineAmount", "50", "قيمة غرامة الغياب الافتراضية بالجنيه المصري." }
                });

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "ManualIncomes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManualIncomes_IdempotencyKey",
                table: "ManualIncomes",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "AppSettings", keyColumn: "Key", keyValue: "Attendance.SubscriptionAlertAfterDays");
            migrationBuilder.DeleteData(table: "AppSettings", keyColumn: "Key", keyValue: "Attendance.AbsenceFineAfterDays");
            migrationBuilder.DeleteData(table: "AppSettings", keyColumn: "Key", keyValue: "Attendance.AbsenceFineAmount");

            migrationBuilder.DropIndex(
                name: "IX_ManualIncomes_IdempotencyKey",
                table: "ManualIncomes");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "ManualIncomes");
        }
    }
}
