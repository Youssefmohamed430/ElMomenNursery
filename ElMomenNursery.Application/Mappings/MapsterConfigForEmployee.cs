using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Entities;

namespace ElMomenNursery.Application.Mappings;

public static class MapsterConfigForEmployee
{
    public static readonly TypeAdapterConfig Config = CreateConfig();

    private static TypeAdapterConfig CreateConfig()
    {
        var config = new TypeAdapterConfig();

        config.NewConfig<UpdateEmployeeDto, Employee>()
            .IgnoreNullValues(true);

        return config;
    }
}
