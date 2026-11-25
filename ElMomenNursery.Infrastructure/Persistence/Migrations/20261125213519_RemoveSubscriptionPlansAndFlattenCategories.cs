using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElMomenNursery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSubscriptionPlansAndFlattenCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite cannot drop a referenced table while foreign keys are enabled. Rebuild both
            // tables explicitly so plan/category rows and all subscription/category data stay aligned.
            migrationBuilder.Sql("PRAGMA foreign_keys = OFF;", suppressTransaction: true);
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_Subscriptions_PlanId";
                DROP INDEX IF EXISTS "IX_InventoryCategories_ParentCategoryId";

                CREATE TABLE "ef_temp_Subscriptions" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_Subscriptions" PRIMARY KEY,
                    "AmountDue" TEXT NOT NULL,
                    "ChildId" TEXT NOT NULL,
                    "CreatedAt" INTEGER NOT NULL,
                    "DeletedAt" INTEGER NULL,
                    "DeletedByUserId" TEXT NULL,
                    "IsDeleted" INTEGER NOT NULL,
                    "Notes" TEXT NULL,
                    "PeriodEnd" TEXT NOT NULL,
                    "PeriodStart" TEXT NOT NULL,
                    "Status" TEXT NOT NULL,
                    CONSTRAINT "CK_Subscriptions_AmountDueNonNegative" CHECK (CAST("AmountDue" AS REAL) >= 0),
                    CONSTRAINT "CK_Subscriptions_PeriodRange" CHECK ("PeriodEnd" >= "PeriodStart"),
                    CONSTRAINT "FK_Subscriptions_Children_ChildId" FOREIGN KEY ("ChildId") REFERENCES "Children" ("Id") ON DELETE RESTRICT
                );
                INSERT INTO "ef_temp_Subscriptions" ("Id", "AmountDue", "ChildId", "CreatedAt", "DeletedAt", "DeletedByUserId", "IsDeleted", "Notes", "PeriodEnd", "PeriodStart", "Status")
                SELECT "Id", "AmountDue", "ChildId", "CreatedAt", "DeletedAt", "DeletedByUserId", "IsDeleted", "Notes", "PeriodEnd", "PeriodStart", "Status" FROM "Subscriptions";
                DROP TABLE "Subscriptions";
                ALTER TABLE "ef_temp_Subscriptions" RENAME TO "Subscriptions";

                CREATE TABLE "ef_temp_InventoryCategories" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_InventoryCategories" PRIMARY KEY,
                    "DeletedAt" INTEGER NULL,
                    "DeletedByUserId" TEXT NULL,
                    "Description" TEXT NULL,
                    "IsActive" INTEGER NOT NULL,
                    "IsDeleted" INTEGER NOT NULL,
                    "Name" TEXT NOT NULL
                );
                INSERT INTO "ef_temp_InventoryCategories" ("Id", "DeletedAt", "DeletedByUserId", "Description", "IsActive", "IsDeleted", "Name")
                SELECT "Id", "DeletedAt", "DeletedByUserId", "Description", "IsActive", "IsDeleted", "Name" FROM "InventoryCategories";
                DROP TABLE "InventoryCategories";
                ALTER TABLE "ef_temp_InventoryCategories" RENAME TO "InventoryCategories";
                DROP TABLE "SubscriptionPlans";

                CREATE INDEX "IX_Subscriptions_Child_PeriodStart" ON "Subscriptions" ("ChildId", "PeriodStart");
                CREATE UNIQUE INDEX "UX_InventoryCategories_Name" ON "InventoryCategories" ("Name") WHERE "IsDeleted" = 0;
                """);
            migrationBuilder.Sql("PRAGMA foreign_keys = ON;", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubscriptionPlanId",
                table: "Subscriptions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentCategoryId",
                table: "InventoryCategories",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultFee = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DeletedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                    table.CheckConstraint("CK_SubscriptionPlans_DefaultFeeNonNegative", "CAST(\"DefaultFee\" AS REAL) >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanId",
                table: "Subscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCategories_ParentCategoryId",
                table: "InventoryCategories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "UX_SubscriptionPlans_Name",
                table: "SubscriptionPlans",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryCategories_InventoryCategories_ParentCategoryId",
                table: "InventoryCategories",
                column: "ParentCategoryId",
                principalTable: "InventoryCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_SubscriptionPlans_SubscriptionPlanId",
                table: "Subscriptions",
                column: "SubscriptionPlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
