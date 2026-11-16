# تغييرات Phase 9 — الاشتراكات والمدفوعات والإيصالات

## ما تغيّر

- أضيفت شاشة عربية للاشتراكات مع بحث وفلترة بالحالة والخطة والفترة والمتبقي، وملخص مالي للقائمة المعروضة، وأزرار محكومة بالصلاحيات.
- استبدلت `SubscriptionService` القديمة بخدمة Application تعيد `Result<T>` وتتحقق من الصلاحيات وقواعد المبالغ والفترة والطفل والخطة. تستخدم الفلاتر SQL قبل تحميل البيانات، ويُحسب مجموع الدفعات في الذاكرة لتجنب تجميع decimal في SQLite.
- أضيف تسجيل دفعة وتخصيصها ورقم إيصال فريد من نمط افتراضي `R-yyyy-000001`. إنشاء الاشتراك مع دفعة أولى وتخصيصها ورقمها وسجل التدقيق يتم داخل معاملة واحدة.
- التعديل يستخدم `subscriptionId` ويستبدل القيم كاملة، ولا يسمح بتخفيض المستحق تحت المدفوع. الإلغاء يتطلب سببًا ويسجل قبل/بعد. الحذف يرفض وجود أي دفعات أو تخصيصات.
- أضيفت صلاحية `Subscriptions.Export` وSeed V4 تدريجي لـ Admin وManager فقط. لا تمنح المحاسب إدارة الاشتراك حتى الآن؛ القرار موثق كمفتوح.
- أضيف إيصال PDF بحجم A5 باستخدام QuestPDF وخط Noto Naskh Arabic المضمن بترخيص OFL، مع دعم RTL وعرض اختياري لعنوان/هاتف محفوظين في AppSettings.
- أضيفت اختبارات SQLite In-Memory للخدمة، التحديث بحسب المعرّف، تقاطع الفترة، حالات الدفع، الإضافة بدفعة أولى، منع حذف سجل مدفوع، وتوليد ملف PDF.

## الملفات الرئيسية

- `ElMomenNursery.Domain/Abstractions/SubscriptionErrors.cs` و`Domain/Entities/Payment.cs`
- `ElMomenNursery.Application/Services/SubscriptionService.cs` و`SubscriptionRules.cs`
- `ElMomenNursery.Application/DTOs/Subscription*` و`SubscriptionOperationDtos.cs`
- `ElMomenNursery.Application/IServices/ISubscriptionService.cs` و`IReceiptPdfService.cs`
- `ElMomenNursery.Infrastructure/Persistence/Migrations/*AddPaymentReceiptNumber*`
- `ElMomenNursery.Infrastructure/Documents/ReceiptPdfService.cs`
- `ElMomenNursery.Infrastructure/Resources/Fonts/` (Noto Naskh Arabic ونسخة OFL)
- `ElMomenNursery/Forms/SubscriptionsPage.cs` و`Forms/MainForm.cs`
- `ElMomenNursery.Tests/SubscriptionTests.cs` و`SubscriptionTestDoubles.cs`
- `docs/decisions/0003-subscriptions-and-receipts.md`

## التحقق

- `dotnet build ElMomenNursery.sln`: ناجح، دون أخطاء أو تحذيرات.
- `dotnet test ElMomenNursery.sln`: ناجح، 28 اختبارًا، دون فشل أو تخطي.
- `dotnet ef migrations has-pending-model-changes --project ElMomenNursery.Infrastructure --startup-project ElMomenNursery`: لا توجد تغييرات نموذج معلقة بعد Migration رقم الإيصال.

## ما لم يُتحقق منه

- لم تراجع الصفحة أو الإيصال بصريًا على جهاز الحضانة عند 1024×700، ولم تختبر الطباعة بالأبيض والأسود.
- اختبار PDF يؤكد ترويسة `%PDF` وحجم الملف، لكنه لا يثبت بصريًا تشكيل العربية أو اتجاه الأرقام.
- لم يُختبر تثبيت التطبيق وتشغيل PDF على جهاز نظيف أو فتح التطبيق الافتراضي للـ PDF فعليًا.
- لا تغطي الاختبارات كل حالات الصلاحيات أو جميع حالات الخطط/البيانات الفارغة، ولا كل التركيبات الممكنة للتعديل مع وجود دفعات.
- يلزم تأكيد أهلية QuestPDF Community قبل النشر الإنتاجي.

## بنود مفتوحة

- نمط رقم الإيصال، أهلية الترخيص، صلاحية إدارة الاشتراكات للمحاسب، طباعة المبلغ كتابةً، وسياسة تقاطع الفترات: راجع ADR 0003.
- تجربة الشاشة على الجهاز الفعلي ومراجعة PDF العربي بصريًا والطباعة الرمادية.
