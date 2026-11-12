using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElMomenNursery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignCurrentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ChildId",
                table: "FileAttachments",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeId",
                table: "FileAttachments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UploadedByUserId",
                table: "FileAttachments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceDevices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceType = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    ConnectionDetails = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<long>(type: "INTEGER", nullable: false),
                    UserAccountId = table.Column<string>(type: "TEXT", nullable: true),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", nullable: true),
                    ChangeSummary = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_UserAccounts_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChildAttendances",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ChildId = table.Column<string>(type: "TEXT", nullable: false),
                    AttendanceDate = table.Column<string>(type: "TEXT", nullable: false),
                    CheckInTime = table.Column<long>(type: "INTEGER", nullable: true),
                    CheckOutTime = table.Column<long>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    EntryMethod = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    RecordedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildAttendances_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChildAttendances_UserAccounts_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAttendances",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<string>(type: "TEXT", nullable: false),
                    AttendanceDate = table.Column<string>(type: "TEXT", nullable: false),
                    CheckInTime = table.Column<long>(type: "INTEGER", nullable: true),
                    CheckOutTime = table.Column<long>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    EntryMethod = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    RecordedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAttendances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeAttendances_UserAccounts_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ExpenseDate = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PaymentReference = table.Column<string>(type: "TEXT", nullable: true),
                    RecordedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    IsVoided = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                    table.CheckConstraint("CK_Expenses_AmountNonNegative", "\"Amount\" >= 0");
                    table.ForeignKey(
                        name: "FK_Expenses_UserAccounts_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "TEXT", nullable: false),
                    InvoiceDate = table.Column<string>(type: "TEXT", nullable: false),
                    ChildId = table.Column<string>(type: "TEXT", nullable: true),
                    CustomerName = table.Column<string>(type: "TEXT", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.CheckConstraint("CK_Invoices_TotalAmountNonNegative", "\"TotalAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_Invoices_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_UserAccounts_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ChildId = table.Column<string>(type: "TEXT", nullable: false),
                    SubscriptionId = table.Column<string>(type: "TEXT", nullable: true),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PaidAt = table.Column<long>(type: "INTEGER", nullable: false),
                    PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: true),
                    ReceivedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CK_Payments_AmountNonNegative", "\"Amount\" >= 0");
                    table.ForeignKey(
                        name: "FK_Payments_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_UserAccounts_ReceivedByUserId",
                        column: x => x.ReceivedByUserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRecords",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<string>(type: "TEXT", nullable: false),
                    PeriodStart = table.Column<string>(type: "TEXT", nullable: false),
                    PeriodEnd = table.Column<string>(type: "TEXT", nullable: false),
                    AgreedSalary = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Additions = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Deductions = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PaymentDate = table.Column<string>(type: "TEXT", nullable: true),
                    IsPaid = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRecords", x => x.Id);
                    table.CheckConstraint("CK_PayrollRecords_PeriodRange", "\"PeriodEnd\" >= \"PeriodStart\"");
                    table.CheckConstraint("CK_PayrollRecords_SalaryNonNegative", "\"AgreedSalary\" >= 0 AND \"AmountPaid\" >= 0");
                    table.ForeignKey(
                        name: "FK_PayrollRecords_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeviceAttendanceEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceEventIdentifier = table.Column<string>(type: "TEXT", nullable: false),
                    AttendanceDeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    ObservedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ChildId = table.Column<string>(type: "TEXT", nullable: true),
                    EmployeeId = table.Column<string>(type: "TEXT", nullable: true),
                    IsProcessed = table.Column<bool>(type: "INTEGER", nullable: false),
                    LinkedAttendanceId = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceAttendanceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceAttendanceEvents_AttendanceDevices_AttendanceDeviceId",
                        column: x => x.AttendanceDeviceId,
                        principalTable: "AttendanceDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceAttendanceEvents_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DeviceAttendanceEvents_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    InvoiceId = table.Column<string>(type: "TEXT", nullable: false),
                    InventoryItemId = table.Column<string>(type: "TEXT", nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "TEXT", nullable: false),
                    UnitPriceSnapshot = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    LineTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.CheckConstraint("CK_InvoiceLines_LineTotalNonNegative", "\"LineTotal\" >= 0");
                    table.CheckConstraint("CK_InvoiceLines_QuantityPositive", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_InvoiceLines_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PaymentId = table.Column<string>(type: "TEXT", nullable: false),
                    SubscriptionId = table.Column<string>(type: "TEXT", nullable: false),
                    AmountApplied = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAllocations", x => x.Id);
                    table.CheckConstraint("CK_PaymentAllocations_AmountAppliedNonNegative", "\"AmountApplied\" >= 0");
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    InventoryItemId = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    MovementType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    OccurredAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UnitCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    InvoiceLineId = table.Column<string>(type: "TEXT", nullable: true),
                    RecordedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_InvoiceLines_InvoiceLineId",
                        column: x => x.InvoiceLineId,
                        principalTable: "InvoiceLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StockMovements_UserAccounts_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_Employee_Type",
                table: "FileAttachments",
                columns: new[] { "EmployeeId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_UploadedByUserId",
                table: "FileAttachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserAccountId",
                table: "AuditLogs",
                column: "UserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildAttendances_RecordedByUserId",
                table: "ChildAttendances",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_ChildAttendances_Child_Date",
                table: "ChildAttendances",
                columns: new[] { "ChildId", "AttendanceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAttendanceEvents_ChildId",
                table: "DeviceAttendanceEvents",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAttendanceEvents_EmployeeId",
                table: "DeviceAttendanceEvents",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "UX_DeviceAttendanceEvents_Device_Identifier",
                table: "DeviceAttendanceEvents",
                columns: new[] { "AttendanceDeviceId", "DeviceEventIdentifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendances_RecordedByUserId",
                table: "EmployeeAttendances",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_EmployeeAttendances_Employee_Date",
                table: "EmployeeAttendances",
                columns: new[] { "EmployeeId", "AttendanceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExpenseDate",
                table: "Expenses",
                column: "ExpenseDate");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_RecordedByUserId",
                table: "Expenses",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InventoryItemId",
                table: "InvoiceLines",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId",
                table: "InvoiceLines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ChildId",
                table: "Invoices",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CreatedByUserId",
                table: "Invoices",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceDate",
                table: "Invoices",
                column: "InvoiceDate");

            migrationBuilder.CreateIndex(
                name: "UX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_PaymentId",
                table: "PaymentAllocations",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_SubscriptionId",
                table: "PaymentAllocations",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ChildId",
                table: "Payments",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaidAt",
                table: "Payments",
                column: "PaidAt");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReceivedByUserId",
                table: "Payments",
                column: "ReceivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SubscriptionId",
                table: "Payments",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRecords_Employee_PeriodStart",
                table: "PayrollRecords",
                columns: new[] { "EmployeeId", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_InvoiceLineId",
                table: "StockMovements",
                column: "InvoiceLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_Item_OccurredAt",
                table: "StockMovements",
                columns: new[] { "InventoryItemId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_RecordedByUserId",
                table: "StockMovements",
                column: "RecordedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FileAttachments_Employees_EmployeeId",
                table: "FileAttachments",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FileAttachments_UserAccounts_UploadedByUserId",
                table: "FileAttachments",
                column: "UploadedByUserId",
                principalTable: "UserAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileAttachments_Employees_EmployeeId",
                table: "FileAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_FileAttachments_UserAccounts_UploadedByUserId",
                table: "FileAttachments");

            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ChildAttendances");

            migrationBuilder.DropTable(
                name: "DeviceAttendanceEvents");

            migrationBuilder.DropTable(
                name: "EmployeeAttendances");

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "PaymentAllocations");

            migrationBuilder.DropTable(
                name: "PayrollRecords");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "AttendanceDevices");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "InvoiceLines");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_FileAttachments_Employee_Type",
                table: "FileAttachments");

            migrationBuilder.DropIndex(
                name: "IX_FileAttachments_UploadedByUserId",
                table: "FileAttachments");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "FileAttachments");

            migrationBuilder.DropColumn(
                name: "UploadedByUserId",
                table: "FileAttachments");

            migrationBuilder.AlterColumn<string>(
                name: "ChildId",
                table: "FileAttachments",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
