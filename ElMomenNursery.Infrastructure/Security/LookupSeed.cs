using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElMomenNursery.Infrastructure.Security;

public static class LookupSeed
{
    public static async Task InitializeAsync(NurseryDbContext db, CancellationToken ct = default)
    {
        if (await db.AppSettings.AnyAsync(x => x.Key == "Lookups.Seeded.V1", ct)) return;
        var seeds = new (string Category, string[] Names)[]
        {
            (LookupCategories.PaymentMethod, ["نقدًا", "تحويل بنكي", "أخرى"]),
            (LookupCategories.IncomeSource, ["تبرع", "منحة", "دعم خارجي", "نشاط ترفيهي إضافي", "رحلة", "إيراد متنوع", "أخرى"]),
            (LookupCategories.ExpenseCategory, ["كهرباء", "مياه", "إيجار", "نظافة", "صيانة", "مستلزمات", "نقل", "ضيافة", "أخرى"]),
            (LookupCategories.JobDesignation, ["معلمة", "مساعدة معلمة", "مشرفة", "محاسب", "إدارية", "عامل خدمات"]),
            (LookupCategories.FinanceTransactionType, ["اشتراكات", "مبيعات مخزن", "إيرادات يدوية", "رواتب", "مصروفات يدوية", "غرامة غياب"])
        };
        foreach (var (category, names) in seeds)
            for (var i = 0; i < names.Length; i++)
                if (!await db.LookupItems.AnyAsync(x => x.Category == category && x.Name == names[i], ct))
                    db.LookupItems.Add(new LookupItem { Category = category, Name = names[i], SortOrder = i, IsSystem = true });
        db.AppSettings.Add(new AppSetting { Key = "Lookups.Seeded.V1", Value = "true", Description = "Initial lookup values." });
        await db.SaveChangesAsync(ct);
    }
}
