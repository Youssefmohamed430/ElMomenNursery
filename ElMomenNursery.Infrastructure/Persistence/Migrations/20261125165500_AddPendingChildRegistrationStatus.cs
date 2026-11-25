using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElMomenNursery.Infrastructure.Persistence.Migrations;

public partial class AddPendingChildRegistrationStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<bool>(
        name: "IsRegistrationComplete", table: "Children", type: "INTEGER", nullable: false, defaultValue: true);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
        name: "IsRegistrationComplete", table: "Children");
}
