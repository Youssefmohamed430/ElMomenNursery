using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Infrastructure.Persistence;
using ElMomenNursery.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.Identity;

if (args.Length == 0 || args[0] is not ("clear" or "seed"))
{
    Console.WriteLine("Usage: dotnet run --project ElMomenNursery.DataSeeding -- [clear|seed] [database-path]");
    return 2;
}

var dbPath = args.Length > 1
    ? Path.GetFullPath(args[1])
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "dev-data", "nursery-mockup.db"));
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
var options = new DbContextOptionsBuilder<NurseryDbContext>()
    .UseSqlite($"Data Source={dbPath};Foreign Keys=True;Default Timeout=30")
    .Options;
await using var db = new NurseryDbContext(options);
await db.Database.MigrateAsync();

if (args[0] == "clear")
{
    await ClearAsync(db);
    Console.WriteLine($"Cleared application data from {dbPath}");
    return 0;
}

await ClearAsync(db);
await AuthSeed.InitializeAsync(db);
await SeedMockupAsync(db);
Console.WriteLine($"Applied mockup data to {dbPath}");
return 0;

static async Task ClearAsync(NurseryDbContext db)
{
    await using var tx = await db.Database.BeginTransactionAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA defer_foreign_keys = ON;");
    foreach (var entity in db.Model.GetEntityTypes().Where(e => !e.IsOwned()).OrderBy(e => e.GetTableName(), StringComparer.Ordinal))
    {
        var table = entity.GetTableName();
        if (table is null || table.StartsWith("__", StringComparison.Ordinal)) continue;
        var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = $"DELETE FROM \"{table.Replace("\"", "\"\"", StringComparison.Ordinal)}\";";
        await command.ExecuteNonQueryAsync();
    }
    await tx.CommitAsync();
}

static async Task SeedMockupAsync(NurseryDbContext db)
{
    var now = DateTimeOffset.UtcNow;
    var today = DateOnly.FromDateTime(DateTime.Today);
    var roles = await db.Roles.ToDictionaryAsync(x => x.Name);
    var managerRole = roles["Manager"];
    var adminRole = roles["Admin"];
    var manager = new UserAccount { Username = "manager", UsernameNormalized = "MANAGER", PasswordHash = "", Role = managerRole, FullName = "مدير تجريبي" };
    var admin = new UserAccount { Username = "admin", UsernameNormalized = "ADMIN", PasswordHash = "", Role = adminRole, FullName = "مسؤول تجريبي" };
    var passwordHasher = new PasswordHasher<UserAccount>();
    manager.PasswordHash = passwordHasher.HashPassword(manager, "Mockup@123");
    admin.PasswordHash = passwordHasher.HashPassword(admin, "Mockup@123");
    db.UserAccounts.AddRange(manager, admin);
    await db.SaveChangesAsync();
    db.UserRoles.AddRange(new UserRole { User = manager, Role = managerRole }, new UserRole { User = admin, Role = adminRole });
    var deletePermission = await db.Permissions.SingleAsync(x => x.Code == "Students.Delete");
    db.RolePermissions.Add(new RolePermission { Role = managerRole, Permission = deletePermission });
    await db.SaveChangesAsync();

    var employees = Enumerable.Range(0, 12).Select(i => new Employee
    {
        FullName = $"{new[] { "سارة", "محمد", "منى", "أحمد", "نور", "عمر", "هبة", "يوسف", "مريم", "علي", "دعاء", "كريم" }[i]} {new[] { "أحمد حسن", "محمد علي", "محمود إبراهيم", "حسن السيد" }[i % 4]}",
        NationalIdNumber = $"MOCK-EMP-{i + 1:0000}", Address = new[] { "مدينة نصر، القاهرة", "المعادي، القاهرة", "الهرم، الجيزة", "شبرا، القاهرة" }[i % 4],
        Phone = $"0102000{i + 1:0000}", AlternatePhone = i % 3 == 0 ? $"0113000{i + 1:0000}" : null,
        JobDesignation = new[] { "معلمة", "مساعدة معلمة", "مشرفة", "محاسب", "إدارية", "عامل خدمات" }[i % 6],
        HireDate = today.AddMonths(-6 - (i * 3)), Salary = 4500 + (i * 250), HasExperience = i % 3 != 0,
        Qualification = new[] { "بكالوريوس تربية", "دبلوم", "بكالوريوس تجارة", "ثانوية عامة" }[i % 4], GraduationYear = 2012 + i % 12,
        MaritalStatus = (ElMomenNursery.Domain.Enums.MaritalStatus)(i % 4), IsDeleted = i == 11, DeletedAt = i == 11 ? now.AddDays(-8) : null
    }).ToList();
    var children = Enumerable.Range(0, 60).Select(i => new Child
    {
        FullName = $"{new[] { "آدم", "ليلى", "يوسف", "مريم", "عمر", "جنى", "سليم", "ملك", "ياسين", "نور" }[i % 10]} {new[] { "محمد علي", "أحمد حسن", "محمود إبراهيم", "كريم السيد", "حسام عادل", "طارق منصور" }[i % 6]}",
        Address = new[] { "مدينة نصر، القاهرة", "المعادي، القاهرة", "الهرم، الجيزة", "شبرا، القاهرة", "مصر الجديدة، القاهرة" }[i % 5],
        EnrollmentDate = today.AddDays(-30 - i * 9), DateOfBirth = today.AddYears(-2 - i % 5).AddDays(-i * 3),
        Gender = i % 2 == 0 ? ElMomenNursery.Domain.Enums.ChildGender.Boy : ElMomenNursery.Domain.Enums.ChildGender.Girl,
        FatherName = $"{new[] { "محمد", "أحمد", "محمود", "كريم", "حسام" }[i % 5]} {new[] { "علي", "حسن", "إبراهيم", "السيد" }[i % 4]}",
        FatherOccupation = new[] { "مهندس", "محاسب", "موظف", "طبيب", null, "تاجر" }[i % 6], FatherPhone = $"010100{i + 1:00000}", FatherWhatsApp = i % 3 == 0 ? $"010100{i + 1:00000}" : null,
        MotherName = $"{new[] { "منى", "هبة", "دعاء", "سماح", "آية" }[i % 5]} {new[] { "محمد", "أحمد", "محمود", "حسن" }[i % 4]}",
        MotherOccupation = new[] { "ربة منزل", "معلمة", null, "موظفة" }[i % 4], MotherPhone = $"011100{i + 1:00000}", MotherWhatsApp = i % 4 == 0 ? $"011100{i + 1:00000}" : null,
        Notes = i % 7 == 0 ? "يحتاج متابعة في بداية اليوم" : null, HasMedicalConditions = i % 13 == 0,
        PreviouslyAttendedNursery = i % 4 == 0, Shift = (Shift)(i % 4),
        IsDeleted = i >= 58, DeletedAt = i >= 58 ? now.AddDays(-5) : null
    }).ToList();
    var rooms = Enumerable.Range(1, 6).Select(i => new Classroom { Name = new[] { "الفراشات", "النجوم", "الزهور", "الأشبال", "الطيور", "الأصدقاء" }[i - 1], Description = $"قاعة تجريبية رقم {i}", IsActive = i != 6 }).ToList();
    var plans = new[] { new SubscriptionPlan { Name = "اشتراك صباحي", DefaultFee = 1800, Description = "الفترة الصباحية" }, new SubscriptionPlan { Name = "اشتراك مسائي", DefaultFee = 1600, Description = "الفترة المسائية" }, new SubscriptionPlan { Name = "اشتراك يوم كامل", DefaultFee = 2500, Description = "اليوم الكامل" }, new SubscriptionPlan { Name = "اشتراك فترة ونصف", DefaultFee = 2200, Description = "فترة ونصف" } };
    var categories = new[] { "أدوات تعليمية", "نظافة", "أغذية", "ألعاب", "قرطاسية" }.Select(n => new InventoryCategory { Name = n, Description = $"تصنيف {n}" }).ToList();
    db.AddRange(employees); db.AddRange(children); db.AddRange(rooms); db.AddRange(plans); db.AddRange(categories);
    await db.SaveChangesAsync();

    var items = Enumerable.Range(0, 40).Select(i => new InventoryItem { Name = $"{new[] { "ألوان خشبية", "كراسات", "مناديل", "صابون", "ألعاب تركيب", "أقلام" }[i % 6]} {i + 1:00}", Category = categories[i % categories.Count], UnitPrice = 15 + i * 3, Quantity = i % 9 == 0 ? 0 : 5 + i, IsActive = i % 11 != 0 }).ToList();
    var device = new AttendanceDevice { DeviceType = "Mock", DisplayName = "جهاز حضور تجريبي", ConnectionDetails = "بيانات محاكاة بلا اتصال فعلي" };
    db.AddRange(items); db.Add(device);
    await db.SaveChangesAsync();

    var enrollments = children.Select((child, i) => new ChildClassEnrollment { Child = child, Classroom = rooms[i % rooms.Count], StartDate = child.EnrollmentDate }).ToList();
    var assignments = employees.SelectMany((employee, i) => new[] { 0, 1 }.Select(offset => new ClassTeacherAssignment { Employee = employee, Classroom = rooms[(i + offset) % rooms.Count], StartDate = today.AddYears(-1).AddDays(i) })).ToList();
    db.AddRange(enrollments); db.AddRange(assignments);

    var subscriptions = new List<Subscription>();
    var payments = new List<Payment>();
    var allocations = new List<PaymentAllocation>();
    var statuses = new[] { ElMomenNursery.Domain.Enums.SubscriptionStatus.Paid, ElMomenNursery.Domain.Enums.SubscriptionStatus.PartiallyPaid, ElMomenNursery.Domain.Enums.SubscriptionStatus.Pending, ElMomenNursery.Domain.Enums.SubscriptionStatus.Cancelled, ElMomenNursery.Domain.Enums.SubscriptionStatus.Waived };
    for (var ci = 0; ci < children.Count - 2; ci++)
    for (var month = 5; month >= 0; month--)
    {
        var start = new DateOnly(today.Year, today.Month, 1).AddMonths(-month);
        var status = statuses[(ci + month) % statuses.Length];
        var plan = plans[ci % plans.Length];
        var sub = new Subscription { Child = children[ci], SubscriptionPlan = plan, PeriodStart = start, PeriodEnd = start.AddMonths(1).AddDays(-1), AmountDue = plan.DefaultFee + (ci % 3) * 100, Status = status, Notes = month == 0 && status == ElMomenNursery.Domain.Enums.SubscriptionStatus.Pending ? "بانتظار التحصيل" : null };
        subscriptions.Add(sub);
        if (status is ElMomenNursery.Domain.Enums.SubscriptionStatus.Paid or ElMomenNursery.Domain.Enums.SubscriptionStatus.PartiallyPaid)
        {
            var amount = status == ElMomenNursery.Domain.Enums.SubscriptionStatus.Paid ? sub.AmountDue : sub.AmountDue / 2;
            var payment = new Payment { Child = children[ci], Subscription = sub, Amount = amount, PaidAt = new DateTimeOffset(start.Year, start.Month, Math.Min(5 + ci % 20, DateTime.DaysInMonth(start.Year, start.Month)), 10, 0, 0, TimeSpan.Zero), PaymentMethod = (ElMomenNursery.Domain.Enums.PaymentMethod)(ci % 3), ReceiptNumber = $"MOCK-RCP-{ci + 1:000}-{month + 1:00}", ReceivedByUserId = manager.Id, Status = (ci + month) % 17 == 0 ? ElMomenNursery.Domain.Enums.PaymentStatus.Voided : ElMomenNursery.Domain.Enums.PaymentStatus.Posted };
            payments.Add(payment);
            allocations.Add(new PaymentAllocation { Payment = payment, Subscription = sub, AmountApplied = amount });
        }
    }
    db.AddRange(subscriptions); await db.SaveChangesAsync();
    db.AddRange(payments); await db.SaveChangesAsync();
    db.AddRange(allocations);

    var childAttendances = new List<ChildAttendance>();
    for (var i = 0; i < children.Count; i++)
    for (var day = 0; day < 30; day++)
    {
        var date = today.AddDays(-day);
        var status = (ElMomenNursery.Domain.Enums.AttendanceStatus)((i + day) % 4);
        childAttendances.Add(new ChildAttendance { Child = children[i], AttendanceDate = date, Status = status, EntryMethod = (ElMomenNursery.Domain.Enums.AttendanceEntryMethod)((i + day) % 3), CheckInTime = status is ElMomenNursery.Domain.Enums.AttendanceStatus.Present or ElMomenNursery.Domain.Enums.AttendanceStatus.Late ? date.ToDateTime(new TimeOnly(7 + i % 2, 30)).ToUniversalTime() : null, CheckOutTime = status == ElMomenNursery.Domain.Enums.AttendanceStatus.Present ? date.ToDateTime(new TimeOnly(14, 0)).ToUniversalTime() : null, RecordedByUserId = manager.Id, Notes = status == ElMomenNursery.Domain.Enums.AttendanceStatus.Late ? "تأخر بسيط - مثال تجريبي" : null });
    }
    var employeeAttendances = new List<EmployeeAttendance>();
    for (var i = 0; i < employees.Count; i++)
    for (var day = 0; day < 20; day++)
    {
        var date = today.AddDays(-day);
        var status = (ElMomenNursery.Domain.Enums.AttendanceStatus)((i + day) % 4);
        employeeAttendances.Add(new EmployeeAttendance { Employee = employees[i], AttendanceDate = date, Status = status, EntryMethod = (ElMomenNursery.Domain.Enums.AttendanceEntryMethod)((i + day) % 3), CheckInTime = status is ElMomenNursery.Domain.Enums.AttendanceStatus.Present or ElMomenNursery.Domain.Enums.AttendanceStatus.Late ? date.ToDateTime(new TimeOnly(7, 15)).ToUniversalTime() : null, CheckOutTime = status == ElMomenNursery.Domain.Enums.AttendanceStatus.Present ? date.ToDateTime(new TimeOnly(15, 0)).ToUniversalTime() : null, RecordedByUserId = manager.Id });
    }
    db.AddRange(childAttendances); db.AddRange(employeeAttendances);

    var payroll = new List<PayrollRecord>();
    for (var i = 0; i < employees.Count; i++)
    for (var month = 0; month < 3; month++)
    {
        var start = new DateOnly(today.Year, today.Month, 1).AddMonths(-month);
        payroll.Add(new PayrollRecord { Employee = employees[i], PeriodStart = start, PeriodEnd = start.AddMonths(1).AddDays(-1), AgreedSalary = employees[i].Salary, Additions = i % 3 == 0 ? 150 : 0, Deductions = i % 4 == 0 ? 100 : 0, AdvanceDeduction = i % 5 == 0 ? 300 : 0, InsuranceFunds = i % 2 == 0 ? 50 : 0, AmountPaid = month == 0 && i % 4 == 0 ? 0 : employees[i].Salary, IsPaid = !(month == 0 && i % 4 == 0), IsCalculated = true, PaymentDate = month == 0 && i % 4 == 0 ? null : start.AddDays(27), Notes = i % 4 == 0 ? "خصم تجريبي" : null });
    }
    db.AddRange(payroll);
    var advances = employees.Take(8).Select((e, i) => new EmployeeAdvance { Employee = e, OriginalAmount = 9000, RemainingAmount = 5000, AdvanceDate = today.AddDays(-20 - i * 4), Notes = "سلفة تجريبية مع رصيد متبقٍ" }).ToList();
    db.AddRange(advances);

    var invoices = Enumerable.Range(0, 45).Select(i => new Invoice { InvoiceNumber = $"MOCK-INV-{i + 1:0000}", InvoiceDate = today.AddDays(-i * 2), Child = children[i % children.Count], CustomerName = children[i % children.Count].FatherName, TotalAmount = items[i % items.Count].UnitPrice * (1 + i % 3), PaymentMethod = (ElMomenNursery.Domain.Enums.PaymentMethod)(i % 3), Status = (ElMomenNursery.Domain.Enums.InvoiceStatus)(i % 15 == 0 ? 2 : i % 9 == 0 ? 0 : 1), CreatedByUserId = manager.Id, Notes = i % 9 == 0 ? "فاتورة مسودة تجريبية" : null }).ToList();
    db.AddRange(invoices); await db.SaveChangesAsync();
    var invoiceLines = invoices.Select((invoice, i) => new InvoiceLine { Invoice = invoice, InventoryItem = items[i % items.Count], ItemNameSnapshot = items[i % items.Count].Name, UnitPriceSnapshot = items[i % items.Count].UnitPrice, Quantity = 1 + i % 3, LineTotal = invoice.TotalAmount }).ToList();
    db.AddRange(invoiceLines);
    var stock = items.SelectMany((item, i) => Enumerable.Range(0, 2).Select(j => new StockMovement { InventoryItem = item, Quantity = j == 0 ? 5 + i : 1 + i % 3, MovementType = j == 0 ? ElMomenNursery.Domain.Enums.StockMovementType.PurchaseIn : ElMomenNursery.Domain.Enums.StockMovementType.Adjustment, OccurredAt = now.AddDays(-i * 3 - j), UnitCost = item.UnitPrice * 0.7m, RecordedByUserId = manager.Id, Notes = j == 0 ? "توريد تجريبي" : "تسوية مخزون تجريبية" })).ToList();
    db.AddRange(stock);
    db.AddRange(advances.Select((advance, i) => new EmployeeAdvanceRepayment { EmployeeAdvance = advance, PayrollRecord = payroll[i * 3], Amount = 4000, RepaymentDate = today.AddDays(-i) }));
    db.AddRange(Enumerable.Range(0, 90).Select(i => new Expense { ExpenseDate = today.AddDays(-i * 2), Category = new[] { "نظافة", "صيانة", "أدوات تعليمية", "مرافق", "ضيافة" }[i % 5], Amount = 75 + i * 11, Description = $"مصروف تجريبي رقم {i + 1}", PaymentReference = i % 3 == 0 ? $"EXP-{i + 1:000}" : null, RecordedByUserId = manager.Id, IsVoided = i % 19 == 0, Notes = i % 19 == 0 ? "ملغي للتجربة" : null }));
    db.AddRange(Enumerable.Range(0, 60).Select(i => new ManualIncome { IncomeDate = today.AddDays(-i * 3), Source = new[] { "نشاط", "تبرع", "بيع مستلزمات", "إيراد متنوع" }[i % 4], Amount = 100 + i * 17, Description = $"إيراد تجريبي رقم {i + 1}", Reference = i % 2 == 0 ? $"INC-{i + 1:000}" : null, RecordedByUserId = manager.Id }));
    db.AddRange(children.Take(25).Select((child, i) => new FileAttachment { Child = child, Type = (ElMomenNursery.Domain.Enums.AttachmentType)(i % 4), OriginalFileName = $"mock-child-{i + 1:000}.txt", RelativePath = $"mockup/children/{i + 1:000}.txt", ContentType = "text/plain", SizeInBytes = 0, UploadedByUserId = manager.Id }));
    db.AddRange(employees.Take(6).Select((employee, i) => new FileAttachment { Employee = employee, Type = ElMomenNursery.Domain.Enums.AttachmentType.Other, OriginalFileName = $"mock-employee-{i + 1:000}.txt", RelativePath = $"mockup/employees/{i + 1:000}.txt", ContentType = "text/plain", SizeInBytes = 0, UploadedByUserId = manager.Id }));
    db.AddRange(Enumerable.Range(0, 36).Select(i => new DeviceAttendanceEvent { DeviceEventIdentifier = $"MOCK-EVT-{i + 1:0000}", AttendanceDevice = device, ObservedAt = now.AddMinutes(-i * 23), Child = i % 3 == 0 ? null : children[i % children.Count], Employee = i % 3 == 0 ? employees[i % employees.Count] : null, IsProcessed = i % 4 != 0, Notes = i % 4 == 0 ? "حدث بانتظار المراجعة" : null }));
    db.AddRange(Enumerable.Range(0, 40).Select(i => new AuditLog { UserAccountId = i % 2 == 0 ? manager.Id : admin.Id, Timestamp = now.AddHours(-i), Action = new[] { "Create", "Update", "Delete", "Restore" }[i % 4], EntityType = new[] { "Child", "Payment", "Employee", "InventoryItem" }[i % 4], EntityId = children[i % children.Count].Id.ToString(), ChangeSummary = $"سجل تدقيق تجريبي رقم {i + 1}" }));
    db.AddRange(Enumerable.Range(0, 24).Select(i => new AuthenticationAuditLog { UserId = i % 4 == 0 ? null : manager.Id, Username = i % 4 == 0 ? "unknown-test" : manager.Username, Action = i % 3 == 0 ? "LoginFailed" : "LoginSucceeded", Description = "محاولة دخول تجريبية", Success = i % 3 != 0, FailureReason = i % 3 == 0 ? "بيانات دخول غير صحيحة (مثال)" : null, Timestamp = now.AddDays(-i) }));
    db.AppSettings.Add(new AppSetting { Key = "MockupData.Enabled", Value = "true", Description = "قاعدة بيانات تطوير ببيانات وهمية فقط" });
    await db.SaveChangesAsync();
}



