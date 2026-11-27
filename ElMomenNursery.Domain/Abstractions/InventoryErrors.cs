namespace ElMomenNursery.Domain.Abstractions;

public static class InventoryErrors
{
    public static readonly Error PermissionDenied = new("Inventory.PermissionDenied", "لا تملك الصلاحية اللازمة لإدارة المخزن.");
    public static readonly Error CategoryNotFound = new("Inventory.CategoryNotFound", "لم يتم العثور على الفئة.");
    public static readonly Error ItemNotFound = new("Inventory.ItemNotFound", "لم يتم العثور على الصنف.");
    public static readonly Error InvalidData = new("Inventory.InvalidData", "البيانات المدخلة غير مكتملة أو غير صحيحة.");
    public static readonly Error InvalidParentCategory = new("Inventory.InvalidParentCategory", "الفئة الأب غير صالحة.");
public static readonly Error InvalidPrice = new("Inventory.InvalidPrice", "يجب أن يكون السعر صفرًا أو أكثر وبحد أقصى منزلتين عشريتين.");
    public static readonly Error InvalidQuantity = new("Inventory.InvalidQuantity", "يجب أن تكون الكمية عددًا صحيحًا يساوي صفرًا أو أكثر.");
    public static readonly Error QuantityChanged = new("Inventory.QuantityChanged", "تغيرت كمية الصنف منذ فتح الشاشة. أعد تحميل البيانات ثم حاول مجددًا.");
    public static readonly Error QuantityAdjustmentReasonRequired = new("Inventory.QuantityAdjustmentReasonRequired", "أدخل سبب تعديل كمية الصنف.");
    public static readonly Error DuplicateCategoryName = new("Inventory.DuplicateCategoryName", "توجد فئة أخرى بالاسم نفسه.");
    public static readonly Error DuplicateItemName = new("Inventory.DuplicateItemName", "يوجد صنف آخر بالاسم نفسه ضمن هذه الفئة.");
    public static readonly Error CategoryInactive = new("Inventory.CategoryInactive", "لا يمكن إضافة الصنف إلى فئة موقوفة.");
    public static readonly Error CategoryDeleted = new("Inventory.CategoryDeleted", "لا يمكن استعادة الصنف لأن فئته محذوفة.");
    public static readonly Error CategoryHasItems = new("Inventory.CategoryHasItems", "لا يمكن حذف فئة تحتوي على أصناف. يمكنك إيقافها بدلًا من ذلك.");
    public static readonly Error CategoryHasSubcategories = new("Inventory.CategoryHasSubcategories", "لا يمكن حذف فئة تحتوي على فئات فرعية.");
    public static readonly Error CategoryMustBeLeafForItems = new("Inventory.CategoryMustBeLeafForItems", "اختر فئة فرعية أخيرة لإضافة الصنف.");
    public static readonly Error PersistenceFailure = new("Inventory.PersistenceFailure", "تعذر حفظ بيانات المخزن. أعد المحاولة.");
    public static readonly Error CategoryCreated = new("Inventory.CategoryCreated", "تم إنشاء الفئة.");
    public static readonly Error CategoryUpdated = new("Inventory.CategoryUpdated", "تم تحديث الفئة.");
    public static readonly Error CategoryDeletedMessage = new("Inventory.CategoryDeleted", "تم نقل الفئة إلى المحذوفات.");
    public static readonly Error CategoryRestored = new("Inventory.CategoryRestored", "تمت استعادة الفئة.");
    public static readonly Error ItemCreated = new("Inventory.ItemCreated", "تم إنشاء الصنف.");
    public static readonly Error ItemUpdated = new("Inventory.ItemUpdated", "تم تحديث الصنف.");
    public static readonly Error ItemDeleted = new("Inventory.ItemDeleted", "تم نقل الصنف إلى المحذوفات.");
    public static readonly Error ItemRestored = new("Inventory.ItemRestored", "تمت استعادة الصنف.");
    public static readonly Error StatusUpdated = new("Inventory.StatusUpdated", "تم تحديث الحالة.");
}
