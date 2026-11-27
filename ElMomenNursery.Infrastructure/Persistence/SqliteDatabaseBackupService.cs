using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace ElMomenNursery.Infrastructure.Persistence;

public sealed class SqliteDatabaseBackupService : IDatabaseBackupService
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteDatabaseBackupService> _logger;

    public SqliteDatabaseBackupService(string connectionString, ILogger<SqliteDatabaseBackupService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public Task<Result> CreateBackupAsync(string destinationPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var source = new SqliteConnectionStringBuilder(_connectionString);
            if (string.IsNullOrWhiteSpace(source.DataSource) || source.DataSource == ":memory:")
                return Task.FromResult(Result.Failure(new Error("Backup.UnsupportedDatabase", "قاعدة البيانات الحالية لا تدعم إنشاء ملف نسخة احتياطية.")));

            destinationPath = Path.GetFullPath(destinationPath);
            if (string.Equals(Path.GetFullPath(source.DataSource), destinationPath, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(Result.Failure(new Error("Backup.SamePath", "اختر مكانًا آخر لحفظ النسخة الاحتياطية.")));

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            if (File.Exists(destinationPath)) File.Delete(destinationPath);

            source.Pooling = false;
            using var sourceConnection = new SqliteConnection(source.ToString());
            using var destinationConnection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = destinationPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            }.ToString());
            sourceConnection.Open();
            destinationConnection.Open();
            sourceConnection.BackupDatabase(destinationConnection);

            using var check = destinationConnection.CreateCommand();
            check.CommandText = "PRAGMA integrity_check";
            if (!string.Equals(Convert.ToString(check.ExecuteScalar()), "ok", StringComparison.OrdinalIgnoreCase))
            {
                destinationConnection.Close();
                File.Delete(destinationPath);
                return Task.FromResult(Result.Failure(new Error("Backup.IntegrityCheckFailed", "تعذر التحقق من سلامة ملف النسخة الاحتياطية.")));
            }

            _logger.LogInformation("Database backup created at {BackupPath}", destinationPath);
            return Task.FromResult(Result.Success("تم إنشاء النسخة الاحتياطية بنجاح."));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Database backup creation failed");
            return Task.FromResult(Result.Failure(new Error("Backup.CreateFailed", "تعذر إنشاء النسخة الاحتياطية. تحقق من صلاحية الكتابة في المكان المحدد.")));
        }
    }
}
