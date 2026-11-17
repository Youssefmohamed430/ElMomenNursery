# تغييرات Phase 8 — الحضور

## ما تغيّر

- أصبحت صفحة **الحضور** تستضيف تبويبي حضور الأطفال وحضور الموظفين، ويظهر كل تبويب وفق صلاحية العرض المناسبة.
- أضيفت خدمة حضور الأطفال لعرض اليوم حسب تاريخ/فصل، والتسجيل اليدوي والسريع، والانصراف، والتصحيح بسبب إلزامي، وتسجيل كل الأطفال غير المسجلين داخل معاملة واحدة.
- تحقّق تطبيق الحضور من التاريخ والحالة وأوقات الحضور والانصراف والتكرار، وأعاد أخطاء عربية typed بدل أخطاء قواعد العمل كنصوص استثناءات.
- صلاحيات الأطفال هي `Attendance.View/Record/Edit`. صلاحيات الموظفين الجديدة هي `EmployeeAttendance.View/Record/Edit`، ويضيف Seed V2 هذه الصلاحيات إلى Manager تدريجيًا مع الحفاظ على تعديلات المسؤول.
- يأتي `RecordedByUserId` من الجلسة داخل الخدمة. تُسجّل التصحيحات القيم قبل/بعد والسبب واسم المستخدم في `AuditLog.ChangeSummary`.
- أضيف البحث العربي المتسامح بالتشكيل وأشكال الألف والياء والتاء المربوطة، وفلاتر الفصل والحالة للأطفال، وملخص الأعداد، وحفظ التحديد بعد إعادة التحميل.
- أضيفت نافذة «سجل طفل لفترة» لاختيار الطفل وتاريخي البداية والنهاية، وعرض السجلات المسجلة مرتبة حسب اليوم مع ملخص الحضور والغياب. الأيام بلا سجل لا تُصنّف تلقائيًا كغياب لأن تقويم أيام العمل غير محدد.
- يحصل كل تحميل أو حفظ في صفحات الحضور على DI scope منفصل. تستخدم الصفحتان رقم إصدار للتحميل لتجاهل النتائج القديمة، و`F5` للتحديث، وقيود صلاحيات على الخدمة نفسها.
- لا توجد هجرة قاعدة جديدة؛ جدولَا الحضور والفهارس الفريدة كانا موجودين. فحص `dotnet ef migrations has-pending-model-changes` لم يجد تغييرات في النموذج.

## الملفات الرئيسية

- `ElMomenNursery.Domain/Abstractions/AttendanceErrors.cs`
- `ElMomenNursery.Application/Services/AttendanceRules.cs`
- `ElMomenNursery.Application/Services/EmployeeAttendanceService.cs`
- `ElMomenNursery.Application/Services/ChildAttendanceService.cs`
- `ElMomenNursery.Application/DTOs/ChildAttendanceDtos.cs` و`ChildDailyAttendanceDto.cs`
- `ElMomenNursery.Application/IServices/IChildAttendanceService.cs`
- `ElMomenNursery.Application/ApplicationServiceCollectionExtensions.cs`
- `ElMomenNursery.Infrastructure/Security/AuthSeed.cs`
- `ElMomenNursery/Forms/AttendancePage.cs` و`AttendanceEditorForm.cs` و`AttendanceLabels.cs` و`ArabicTextNormalizer.cs`
- `ElMomenNursery/Forms/MainForm.cs` و`Program.cs`
- `ElMomenNursery.Tests/AttendanceTests.cs`
- `docs/IMPLEMENTATION-ROADMAP.md` و`docs/ARCHITECTURE.md` و`docs/OFFLINE_AUTHENTICATION.md`

## التحقق

- `dotnet build ElMomenNursery.sln`: ناجح، دون أخطاء أو تحذيرات.
- `dotnet test ElMomenNursery.Tests/ElMomenNursery.Tests.csproj`: ناجح، 13 اختبارًا، دون فشل أو تخطي.
- الاختبارات تغطي قواعد التاريخ والأوقات والسبب، حفظ تعديل الطفل، صلاحية القراءة، مصدر المستخدم المسجل، منع التكرار، تسجيل الكل دون تكرار، استبعاد تسجيل الطفل المستقبلي وفلتر الفصل، استبعاد الموظف الذي لم يحن تاريخ تعيينه، وIdempotency لخطوة Seed V2.
- `dotnet ef migrations has-pending-model-changes --project ElMomenNursery.Infrastructure --startup-project ElMomenNursery`: لا توجد تغييرات معلقة.

## لم يتم التحقق منه بعد

- لم تُجرَ مراجعة بصرية على جهاز Windows الفعلي عند 1024×700 أو اختبار عملي لتغيير التاريخ والخروج أثناء تحميل بطيء.
- لم يُتحقق من سجل تشغيل التطبيق لتأكيد عدم وجود FTL عند تسجيل الخروج والإغلاق.
- اختبار تسجيل الحضور والتحقق من الصلاحيات مغطى جزئيًا؛ لا تغطي مجموعة الاختبارات جميع مسارات خدمات الحضور أو فشلًا مصطنعًا في منتصف معاملة `MarkAllPresent`.
- لم تُختبر عملية Seed على قاعدة تشغيل حقيقية، ولم تُنفذ تجربة واجهة كاملة مع حسابات Manager وTeacher.

## بنود مفتوحة

- تعريف حد التأخير ومواعيد بداية/نهاية الدوام والورديات والإجازات، إن احتاجتها الحضانة؛ تظل حالة «متأخر» يدوية.
- هل ترى المعلمة جميع الفصول أم الفصول المسندة إليها فقط؟ لم يُفترض نطاق صلاحية للفصول.
- الطباعة والتصدير من الحضور ضمن Phase 11.
- أجهزة البصمة والباركود وبروتوكولاتها ما زالت غير محددة؛ الإدخال اليدوي يظل متاحًا.
