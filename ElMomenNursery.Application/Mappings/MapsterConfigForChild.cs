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
            .IgnoreNullValues(true)
            .IgnoreIf((source, _) => source.EnrollmentDate == default, destination => destination.EnrollmentDate)
            .IgnoreIf((source, _) => source.DateOfBirth == default, destination => destination.DateOfBirth)
            .IgnoreIf((source, _) => source.Gender == default, destination => destination.Gender);

        return config;
    }
}
