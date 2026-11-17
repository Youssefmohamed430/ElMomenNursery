using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElMomenNursery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_SubscriptionPlans_Name",
                table: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "UX_Roles_Name",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "UX_InventoryCategories_Name",
                table: "InventoryCategories");

            migrationBuilder.DropIndex(
                name: "UX_Employees_NationalIdNumber",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "UX_EmployeeAttendances_Employee_Date",
                table: "EmployeeAttendances");

            migrationBuilder.DropIndex(
                name: "UX_ClassTeacherAssignments_Employee_Class_StartDate",
                table: "ClassTeacherAssignments");

            migrationBuilder.DropIndex(
                name: "UX_Classrooms_Name",
                table: "Classrooms");

            migrationBuilder.DropIndex(
                name: "UX_ChildClassEnrollments_Child_Class_StartDate",
                table: "ChildClassEnrollments");

            migrationBuilder.DropIndex(
                name: "UX_ChildAttendances_Child_Date",
                table: "ChildAttendances");

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "Subscriptions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "Subscriptions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Subscriptions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "SubscriptionPlans",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "SubscriptionPlans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SubscriptionPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "Roles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "Roles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Roles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "PayrollRecords",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "PayrollRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PayrollRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "InventoryItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "InventoryItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "InventoryItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "InventoryCategories",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "InventoryCategories",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "InventoryCategories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "FileAttachments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "FileAttachments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FileAttachments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "Employees",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "Employees",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Employees",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "EmployeeAttendances",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "EmployeeAttendances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "EmployeeAttendances",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "ClassTeacherAssignments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "ClassTeacherAssignments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ClassTeacherAssignments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "Classrooms",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "Classrooms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Classrooms",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "Children",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "Children",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Children",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "ChildClassEnrollments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "ChildClassEnrollments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ChildClassEnrollments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "ChildAttendances",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "ChildAttendances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ChildAttendances",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "AttendanceDevices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "AttendanceDevices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AttendanceDevices",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "UX_SubscriptionPlans_Name",
                table: "SubscriptionPlans",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = 0");

            migrationBuilder.CreateIndex(
                name: "UX_InventoryCategories_Name",
                table: "InventoryCategories",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Employees_NationalIdNumber",
                table: "Employees",
                column: "NationalIdNumber",
                unique: true,
                filter: "\"IsDeleted\" = 0");

            migrationBuilder.CreateIndex(
                name: "UX_EmployeeAttendances_Employee_Date",
                table: "EmployeeAttendances",
                columns: new[] { "EmployeeId", "AttendanceDate" },
                unique: true,
                filter: "\"IsDeleted\" = 0");

            migrationBuilder.CreateIndex(
                name: "UX_ClassTeacherAssignments_Employee_Class_StartDate",
                table: "ClassTeacherAssignments",
                columns: new[] { "EmployeeId", "ClassroomId", "StartDate" },
                unique: true,
                filter: "\"IsDeleted\" = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Classrooms_Name",
                table: "Classrooms",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = 0");

            migrationBuilder.CreateIndex(
                name: "UX_ChildClassEnrollments_Child_Class_StartDate",
                table: "ChildClassEnrollments",
                columns: new[] { "ChildId", "ClassroomId", "StartDate" },
                unique: true,
                filter: "\"IsDeleted\" = 0");

            migrationBuilder.CreateIndex(
                name: "UX_ChildAttendances_Child_Date",
                table: "ChildAttendances",
                columns: new[] { "ChildId", "AttendanceDate" },
                unique: true,
                filter: "\"IsDeleted\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_SubscriptionPlans_Name",
                table: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "UX_Roles_Name",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "UX_InventoryCategories_Name",
                table: "InventoryCategories");

            migrationBuilder.DropIndex(
                name: "UX_Employees_NationalIdNumber",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "UX_EmployeeAttendances_Employee_Date",
                table: "EmployeeAttendances");

            migrationBuilder.DropIndex(
                name: "UX_ClassTeacherAssignments_Employee_Class_StartDate",
                table: "ClassTeacherAssignments");

            migrationBuilder.DropIndex(
                name: "UX_Classrooms_Name",
                table: "Classrooms");

            migrationBuilder.DropIndex(
                name: "UX_ChildClassEnrollments_Child_Class_StartDate",
                table: "ChildClassEnrollments");

            migrationBuilder.DropIndex(
                name: "UX_ChildAttendances_Child_Date",
                table: "ChildAttendances");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "InventoryCategories");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "InventoryCategories");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "InventoryCategories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "FileAttachments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "FileAttachments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "FileAttachments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EmployeeAttendances");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "EmployeeAttendances");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "EmployeeAttendances");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ClassTeacherAssignments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ClassTeacherAssignments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ClassTeacherAssignments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ChildClassEnrollments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ChildClassEnrollments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ChildClassEnrollments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ChildAttendances");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ChildAttendances");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ChildAttendances");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AttendanceDevices");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "AttendanceDevices");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AttendanceDevices");

            migrationBuilder.CreateIndex(
                name: "UX_SubscriptionPlans_Name",
                table: "SubscriptionPlans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_InventoryCategories_Name",
                table: "InventoryCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Employees_NationalIdNumber",
                table: "Employees",
                column: "NationalIdNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_EmployeeAttendances_Employee_Date",
                table: "EmployeeAttendances",
                columns: new[] { "EmployeeId", "AttendanceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ClassTeacherAssignments_Employee_Class_StartDate",
                table: "ClassTeacherAssignments",
                columns: new[] { "EmployeeId", "ClassroomId", "StartDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Classrooms_Name",
                table: "Classrooms",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ChildClassEnrollments_Child_Class_StartDate",
                table: "ChildClassEnrollments",
                columns: new[] { "ChildId", "ClassroomId", "StartDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ChildAttendances_Child_Date",
                table: "ChildAttendances",
                columns: new[] { "ChildId", "AttendanceDate" },
                unique: true);
        }
    }
}
