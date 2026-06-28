using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace VaryoCms.IntegrationTests.Infrastructure;

/// <summary>
/// Applies numbered SQL migration files from the db/migrations directory.
/// Splits on GO batch separators (which SqlCommand cannot handle natively).
/// </summary>
public static class MigrationRunner
{
    // Matches a line that is only "GO" (case-insensitive, optional whitespace)
    private static readonly Regex GoBatchSplitter =
        new(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

    /// <summary>
    /// Runs all *.sql files in <paramref name="migrationsDir"/> in alphabetical order.
    /// </summary>
    public static async Task RunAllAsync(string connectionString, string migrationsDir)
    {
        var files = Directory
            .GetFiles(migrationsDir, "*.sql")
            .OrderBy(f => f)
            .ToList();

        if (files.Count == 0)
            throw new InvalidOperationException($"No migration files found in: {migrationsDir}");

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        foreach (string file in files)
        {
            string sql = await File.ReadAllTextAsync(file);
            string[] batches = GoBatchSplitter.Split(sql);

            foreach (string batch in batches)
            {
                string trimmed = batch.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = trimmed;
                cmd.CommandTimeout = 120;
                try
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (SqlException ex)
                {
                    throw new InvalidOperationException(
                        $"Migration failed in file '{Path.GetFileName(file)}': {ex.Message}", ex);
                }
            }
        }
    }
}
