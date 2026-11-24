using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElMomenNursery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CastDecimalCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AppSettings",
                columns: new[] { "Key", "Value", "Description" },
                values: new object[] { "Payroll.PaidDaysPerMonth", "22", "عدد أيام العمل المدفوعة الافتراضي لكل شهر راتب." });

            migrationBuilder.DropCheckConstraint(
                name: "CK_Subscriptions_AmountDueNonNegative",
                table: "Subscriptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SubscriptionPlans_DefaultFeeNonNegative",
                table: "SubscriptionPlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PayrollRecords_SalaryNonNegative",
                table: "PayrollRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_AmountNonNegative",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PaymentAllocations_AmountAppliedNonNegative",
                table: "PaymentAllocations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ManualIncomes_AmountPositive",
                table: "ManualIncomes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Invoices_TotalAmountNonNegative",
                table: "Invoices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InvoiceLines_LineTotalNonNegative",
                table: "InvoiceLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryItems_UnitPriceNonNegative",
                table: "InventoryItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Expenses_AmountNonNegative",
                table: "Expenses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Employees_SalaryNonNegative",
                table: "Employees");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmployeeAdvanceRepayments_Amount",
                table: "EmployeeAdvanceRepayments");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Subscriptions_AmountDueNonNegative",
                table: "Subscriptions",
                sql: "CAST(\"AmountDue\" AS REAL) >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SubscriptionPlans_DefaultFeeNonNegative",
                table: "SubscriptionPlans",
                sql: "CAST(\"DefaultFee\" AS REAL) >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PayrollRecords_SalaryNonNegative",
                table: "PayrollRecords",
                sql: "CAST(\"AgreedSalary\" AS REAL) >= 0 AND CAST(\"AmountPaid\" AS REAL) >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_AmountNonNegative",
                table: "Payments",
                sql: "CAST(\"Amount\" AS REAL) >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PaymentAllocations_AmountAppliedNonNegative",
                table: "PaymentAllocations",
                sql: "CAST(\"AmountApplied\" AS REAL) >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ManualIncomes_AmountPositive",
                table: "ManualIncomes",
                sql: "CAST(\"Amount\" AS REAL) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Invoices_TotalAmountNonNegative",
                table: "Invoices",
                sql: "CAST(\"TotalAmount\" AS REAL) >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InvoiceLines_LineTotalNonNegative",
                table: "InvoiceLines",
                sql: "CAST(\"LineTotal\" AS REAL) >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryItems_UnitPriceNonNegative",
                table: "InventoryItems",
                sql: "CAST(\"UnitPrice\" AS REAL) >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Expenses_AmountNonNegative",
                table: "Expenses",
                sql: "CAST(\"Amount\" AS REAL) >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Employees_SalaryNonNegative",
                table: "Employees",
                sql: "CAST(\"Salary\" AS REAL) >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmployeeAdvanceRepayments_Amount",
                table: "EmployeeAdvanceRepayments",
                sql: "CAST(\"Amount\" AS REAL) > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Key",
                keyValue: "Payroll.PaidDaysPerMonth");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Subscriptions_AmountDueNonNegative",
                table: "Subscriptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SubscriptionPlans_DefaultFeeNonNegative",
                table: "SubscriptionPlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PayrollRecords_SalaryNonNegative",
                table: "PayrollRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_AmountNonNegative",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PaymentAllocations_AmountAppliedNonNegative",
                table: "PaymentAllocations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ManualIncomes_AmountPositive",
                table: "ManualIncomes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Invoices_TotalAmountNonNegative",
                table: "Invoices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InvoiceLines_LineTotalNonNegative",
                table: "InvoiceLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryItems_UnitPriceNonNegative",
                table: "InventoryItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Expenses_AmountNonNegative",
                table: "Expenses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Employees_SalaryNonNegative",
                table: "Employees");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmployeeAdvanceRepayments_Amount",
                table: "EmployeeAdvanceRepayments");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Subscriptions_AmountDueNonNegative",
                table: "Subscriptions",
                sql: "\"AmountDue\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SubscriptionPlans_DefaultFeeNonNegative",
                table: "SubscriptionPlans",
                sql: "\"DefaultFee\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PayrollRecords_SalaryNonNegative",
                table: "PayrollRecords",
                sql: "\"AgreedSalary\" >= 0 AND \"AmountPaid\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_AmountNonNegative",
                table: "Payments",
                sql: "\"Amount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PaymentAllocations_AmountAppliedNonNegative",
                table: "PaymentAllocations",
                sql: "\"AmountApplied\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ManualIncomes_AmountPositive",
                table: "ManualIncomes",
                sql: "\"Amount\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Invoices_TotalAmountNonNegative",
                table: "Invoices",
                sql: "\"TotalAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InvoiceLines_LineTotalNonNegative",
                table: "InvoiceLines",
                sql: "\"LineTotal\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryItems_UnitPriceNonNegative",
                table: "InventoryItems",
                sql: "\"UnitPrice\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Expenses_AmountNonNegative",
                table: "Expenses",
                sql: "\"Amount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Employees_SalaryNonNegative",
                table: "Employees",
                sql: "\"Salary\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmployeeAdvanceRepayments_Amount",
                table: "EmployeeAdvanceRepayments",
                sql: "\"Amount\" > 0");
        }
    }
}
