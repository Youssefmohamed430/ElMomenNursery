using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElMomenNursery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialNurseryModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Children",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    EnrollmentDate = table.Column<string>(type: "TEXT", nullable: false),
                    DateOfBirth = table.Column<string>(type: "TEXT", nullable: false),
                    Gender = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    FatherName = table.Column<string>(type: "TEXT", nullable: false),
                    FatherOccupation = table.Column<string>(type: "TEXT", nullable: true),
                    FatherPhone = table.Column<string>(type: "TEXT", nullable: false),
                    FatherWhatsApp = table.Column<string>(type: "TEXT", nullable: true),
                    MotherName = table.Column<string>(type: "TEXT", nullable: false),
                    MotherOccupation = table.Column<string>(type: "TEXT", nullable: true),
                    MotherPhone = table.Column<string>(type: "TEXT", nullable: false),
                    MotherWhatsApp = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    HasMedicalConditions = table.Column<bool>(type: "INTEGER", nullable: false),
                    PreviouslyAttendedNursery = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Children", x => x.Id);
                    table.CheckConstraint("CK_Children_DateOrder", "\"DateOfBirth\" <= \"EnrollmentDate\"");
                });

            migrationBuilder.CreateTable(
                name: "Classrooms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classrooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    Qualification = table.Column<string>(type: "TEXT", nullable: true),
                    GraduationYear = table.Column<int>(type: "INTEGER", nullable: true),
                    HasExperience = table.Column<bool>(type: "INTEGER", nullable: false),
                    NationalIdNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    MaritalStatus = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: false),
                    AlternatePhone = table.Column<string>(type: "TEXT", nullable: true),
                    JobDesignation = table.Column<string>(type: "TEXT", nullable: false),
                    HireDate = table.Column<string>(type: "TEXT", nullable: false),
                    Salary = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.CheckConstraint("CK_Employees_SalaryNonNegative", "\"Salary\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "InventoryCategories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultFee = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                    table.CheckConstraint("CK_SubscriptionPlans_DefaultFeeNonNegative", "\"DefaultFee\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "FileAttachments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ChildId = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: true),
                    SizeInBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    Checksum = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAttachments", x => x.Id);
                    table.CheckConstraint("CK_FileAttachments_SizeNonNegative", "\"SizeInBytes\" IS NULL OR \"SizeInBytes\" >= 0");
                    table.ForeignKey(
                        name: "FK_FileAttachments_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChildClassEnrollments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ChildId = table.Column<string>(type: "TEXT", nullable: false),
                    ClassroomId = table.Column<string>(type: "TEXT", nullable: false),
                    StartDate = table.Column<string>(type: "TEXT", nullable: false),
                    EndDate = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildClassEnrollments", x => x.Id);
                    table.CheckConstraint("CK_ChildClassEnrollments_DateRange", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                    table.ForeignKey(
                        name: "FK_ChildClassEnrollments_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChildClassEnrollments_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassTeacherAssignments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<string>(type: "TEXT", nullable: false),
                    ClassroomId = table.Column<string>(type: "TEXT", nullable: false),
                    StartDate = table.Column<string>(type: "TEXT", nullable: false),
                    EndDate = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassTeacherAssignments", x => x.Id);
                    table.CheckConstraint("CK_ClassTeacherAssignments_DateRange", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                    table.ForeignKey(
                        name: "FK_ClassTeacherAssignments_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassTeacherAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<string>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.CheckConstraint("CK_InventoryItems_UnitPriceNonNegative", "\"UnitPrice\" >= 0");
                    table.ForeignKey(
                        name: "FK_InventoryItems_InventoryCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "InventoryCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserAccounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<string>(type: "TEXT", nullable: true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    UsernameNormalized = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAccounts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAccounts_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ChildId = table.Column<string>(type: "TEXT", nullable: false),
                    SubscriptionPlanId = table.Column<string>(type: "TEXT", nullable: true),
                    PeriodStart = table.Column<string>(type: "TEXT", nullable: false),
                    PeriodEnd = table.Column<string>(type: "TEXT", nullable: false),
                    AmountDue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.CheckConstraint("CK_Subscriptions_AmountDueNonNegative", "\"AmountDue\" >= 0");
                    table.CheckConstraint("CK_Subscriptions_PeriodRange", "\"PeriodEnd\" >= \"PeriodStart\"");
                    table.ForeignKey(
                        name: "FK_Subscriptions_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChildClassEnrollments_Child_StartDate",
                table: "ChildClassEnrollments",
                columns: new[] { "ChildId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ChildClassEnrollments_Classroom_StartDate",
                table: "ChildClassEnrollments",
                columns: new[] { "ClassroomId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "UX_ChildClassEnrollments_Child_Class_StartDate",
                table: "ChildClassEnrollments",
                columns: new[] { "ChildId", "ClassroomId", "StartDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Children_FullName",
                table: "Children",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "UX_Classrooms_Name",
                table: "Classrooms",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassTeacherAssignments_Classroom_StartDate",
                table: "ClassTeacherAssignments",
                columns: new[] { "ClassroomId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassTeacherAssignments_Employee_StartDate",
                table: "ClassTeacherAssignments",
                columns: new[] { "EmployeeId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "UX_ClassTeacherAssignments_Employee_Class_StartDate",
                table: "ClassTeacherAssignments",
                columns: new[] { "EmployeeId", "ClassroomId", "StartDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_FullName",
                table: "Employees",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "UX_Employees_NationalIdNumber",
                table: "Employees",
                column: "NationalIdNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_Child_Type",
                table: "FileAttachments",
                columns: new[] { "ChildId", "Type" });

            migrationBuilder.CreateIndex(
                name: "UX_InventoryCategories_Name",
                table: "InventoryCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Category_Name",
                table: "InventoryItems",
                columns: new[] { "CategoryId", "Name" });

            migrationBuilder.CreateIndex(
                name: "UX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_SubscriptionPlans_Name",
                table: "SubscriptionPlans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Child_PeriodStart",
                table: "Subscriptions",
                columns: new[] { "ChildId", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanId",
                table: "Subscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_RoleId",
                table: "UserAccounts",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "UX_UserAccounts_EmployeeId",
                table: "UserAccounts",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_UserAccounts_UsernameNormalized",
                table: "UserAccounts",
                column: "UsernameNormalized",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChildClassEnrollments");

            migrationBuilder.DropTable(
                name: "ClassTeacherAssignments");

            migrationBuilder.DropTable(
                name: "FileAttachments");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "UserAccounts");

            migrationBuilder.DropTable(
                name: "Classrooms");

            migrationBuilder.DropTable(
                name: "InventoryCategories");

            migrationBuilder.DropTable(
                name: "Children");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
