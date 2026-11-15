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
        services.AddScoped<IClassRoomService, ClassRoomService>();
        services.AddScoped<IEmployeeAttendanceService, EmployeeAttendanceService>();
        services.AddScoped<IChildAttendanceService, ChildAttendanceService>();
        return services;
    }
}
