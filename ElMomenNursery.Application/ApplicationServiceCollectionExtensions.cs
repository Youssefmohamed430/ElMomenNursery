using ElMomenNursery.Application.IServices;
using ElMomenNursery.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddNurseryApplication(this IServiceCollection services)
    {
        services.AddScoped<IChildService, ChildService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IEmployeeAttendanceService, EmployeeAttendanceService>();
        services.AddScoped<IChildAttendanceService, ChildAttendanceService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IInventoryItemService, InventoryItemService>();
        services.AddScoped<IInventoryCategoryService, InventoryCategoryService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IFinanceService, FinanceService>();
        services.AddScoped<IPayRollRecordService, PayRollRecordService>();
        return services;
    }
}
