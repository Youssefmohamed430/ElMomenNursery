using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Entities;

namespace ElMomenNursery.Application.Mappings;

public static class MapsterConfigForChild
{
    public static readonly TypeAdapterConfig Config = CreateConfig();

    private static TypeAdapterConfig CreateConfig()
    {
        var config = new TypeAdapterConfig();

        config.NewConfig<UpdateChildDTO, Child>()
            .Map(destination => destination.FullName, source => source.FullName)
            .Map(destination => destination.Address, source => source.Address);

        return config;
    }
}
