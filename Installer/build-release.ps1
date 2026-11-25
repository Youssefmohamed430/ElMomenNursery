param(
    [string]$Version = "1.0.6",
    [string]$Configuration = "Release"
)
$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$stage = Join-Path $PSScriptRoot "stage"
$out = Join-Path $projectRoot "حزمة تثبيت نظام إدارة حضانة المؤمن"
Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $stage -Force | Out-Null
dotnet publish (Join-Path $projectRoot "ElMomenNursery\ElMomenNursery.csproj") -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -p:Version=$Version -o $stage
if ($LASTEXITCODE -ne 0) { throw "Application publish failed." }
$mockDir = Join-Path $stage "Mockup"
New-Item -ItemType Directory -Path $mockDir -Force | Out-Null
$mockSource = Join-Path $projectRoot "dev-data\nursery-mockup.db"
if (Test-Path $mockSource) {
    $mockTarget = Join-Path $mockDir "nursery-mockup.db"
    & sqlite3 $mockSource ".backup '$($mockTarget.Replace("'", "''"))'"
    if ($LASTEXITCODE -ne 0) { throw "Could not create a consistent mock database snapshot." }
}
$zipPath = Join-Path $PSScriptRoot "payload.zip"
Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zipPath -CompressionLevel Optimal
foreach ($toolName in @("Setup", "Update")) {
    $toolOut = Join-Path $PSScriptRoot "$toolName-publish"
    Remove-Item -LiteralPath $toolOut -Recurse -Force -ErrorAction SilentlyContinue
    dotnet publish (Join-Path $PSScriptRoot "Bootstrapper.csproj") -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -p:ToolName=$toolName -p:PayloadZip=$zipPath -p:Version=$Version -o $toolOut
    if ($LASTEXITCODE -ne 0) { throw "$toolName publish failed." }
    if ($toolName -eq "Setup") { $setupPath = Join-Path $out "Setup.exe"; $source = Join-Path $toolOut "Setup.exe" }
    else { $updatePath = Join-Path $out "Update.exe"; $source = Join-Path $toolOut "Update.exe" }
    New-Item -ItemType Directory -Path $out -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $(if ($toolName -eq "Setup") { $setupPath } else { $updatePath }) -Force
}
Copy-Item (Join-Path $projectRoot "ElMomenNursery\Resources\Branding\AlMoumenNursery.ico") (Join-Path $out "شعار البرنامج.ico") -Force
@"
نظام إدارة حضانة المؤمن — الإصدار $Version

التثبيت لأول مرة: شغّل Setup.exe بصلاحيات المسؤول. ينشئ البرنامج قاعدة بيانات جديدة تلقائيًا، ويطلب إنشاء حساب المسؤول الأول عند أول تشغيل.
التحديث: شغّل Update.exe من أي مكان. سيغلق البرنامج إذا كان مفتوحًا ثم يحدّث نسخة Program Files تلقائيًا.
تبديل قاعدة البيانات: يمكنك اختيار القاعدة من شاشة الدخول قبل تسجيل الدخول، أو التبديل من الشاشة الرئيسية بحساب مدير أو مسؤول. يعاد تشغيل البرنامج على القاعدة المختارة.
بيانات دخول قاعدة العرض فقط: اسم المستخدم admin وكلمة المرور Mockup@123.
البيانات محفوظة خارج مجلد البرنامج في %LOCALAPPDATA%\AlMoumenNursery، لذلك لا يمسها التحديث.
يدعم الإصدار Windows 10/11 x64. تم تضمين .NET 9 وملفات SQLite المطلوبة داخل البرنامج؛ لا يحتاج إلى اتصال إنترنت أو تثبيت SQL Server.
"@ | Set-Content -LiteralPath (Join-Path $out "اقرأني.txt") -Encoding UTF8
$archivePath = Join-Path $projectRoot "حزمة تثبيت نظام إدارة حضانة المؤمن.zip"
Remove-Item -LiteralPath $archivePath -Force -ErrorAction SilentlyContinue
Compress-Archive -Path $out -DestinationPath $archivePath -CompressionLevel Optimal
Write-Host "Created: $archivePath"
Remove-Item -LiteralPath $stage, $zipPath, (Join-Path $PSScriptRoot "Setup-publish"), (Join-Path $PSScriptRoot "Update-publish") -Recurse -Force -ErrorAction SilentlyContinue
