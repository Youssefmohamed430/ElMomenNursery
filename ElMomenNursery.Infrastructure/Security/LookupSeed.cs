using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElMomenNursery.Infrastructure.Security;

public static class LookupSeed
{
    public static async Task InitializeAsync(NurseryDbContext db, CancellationToken ct = default)
    {
        var alreadySeeded = await db.AppSettings.AnyAsync(x => x.Key == "Lookups.Seeded.V1", ct);
        var shiftTimesSeeded = await db.AppSettings.AnyAsync(x => x.Key == "Lookups.EmployeeShiftStartTime.Seeded.V1", ct);
        var seeds = new (string Category, string[] Names)[]
        {
            (LookupCategories.PaymentMethod, ["نقدًا", "تحويل بنكي", "أخرى"]),
            (LookupCategories.IncomeSource, ["تبرع", "منحة", "دعم خارجي", "نشاط ترفيهي إضافي", "رحلة", "إيراد متنوع", "أخرى"]),
            (LookupCategories.ExpenseCategory, ["كهرباء", "مياه", "إيجار", "نظافة", "صيانة", "مستلزمات", "نقل", "ضيافة", "أخرى"]),
            (LookupCategories.JobDesignation, ["معلمة", "مساعدة معلمة", "مشرفة", "محاسب", "إدارية", "عامل خدمات"]),
            ("EmployeeShiftStartTime", ["07:30", "12:00"]),
            (LookupCategories.FinanceTransactionType, ["اشتراكات", "مبيعات مخزن", "إيرادات يدوية", "رواتب", "مصروفات يدوية", "غرامة غياب"])
        };
        foreach (var (category, names) in seeds.Where(x => !alreadySeeded || x.Category == "EmployeeShiftStartTime" && !shiftTimesSeeded))
            for (var i = 0; i < names.Length; i++)
                if (!await db.LookupItems.AnyAsync(x => x.Category == category && x.Name == names[i], ct))
                    db.LookupItems.Add(new LookupItem { Category = category, Name = names[i], SortOrder = i, IsSystem = true });
        if (!alreadySeeded) db.AppSettings.Add(new AppSetting { Key = "Lookups.Seeded.V1", Value = "true", Description = "Initial lookup values." });
        if (!shiftTimesSeeded) db.AppSettings.Add(new AppSetting { Key = "Lookups.EmployeeShiftStartTime.Seeded.V1", Value = "true", Description = "Default employee shift start time options." });
        await db.SaveChangesAsync(ct);
    }
}
