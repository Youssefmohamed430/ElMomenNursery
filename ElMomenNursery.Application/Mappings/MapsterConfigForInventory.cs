using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Entities;

namespace ElMomenNursery.Application.Mappings;

public static class MapsterConfigForInventory
{
    public static readonly TypeAdapterConfig Config = CreateConfig();
    private static TypeAdapterConfig CreateConfig()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<InventoryItem, InventoryItemDto>()
            .Map(x => x.CategoryName, x => x.Category.Name)
            .Map(x => x.CategoryIsActive, x => x.Category.IsActive);
        config.NewConfig<InventoryCategory, InventoryCategoryDto>()
            .Map(x => x.ItemsCount, x => x.Items.Count(i => !i.IsDeleted));
        return config;
    }
}
