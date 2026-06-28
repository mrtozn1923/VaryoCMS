namespace VaryoCms.IntegrationTests.Infrastructure;

/// <summary>
/// Generates unique test data so tests don't collide on the shared DB.
/// All slugs/emails/keys are GUID-suffixed → safe to run in parallel if needed.
/// </summary>
public static class TestDataFactory
{
    public static string UniqueSlug(string prefix = "ct") =>
        $"{prefix}-{Guid.NewGuid():N}"[..Math.Min(60, prefix.Length + 33)];

    public static string UniqueName(string prefix = "Test") =>
        $"{prefix} {Guid.NewGuid():N}"[..Math.Min(100, prefix.Length + 33)];

    public static string UniqueEmail(string prefix = "user") =>
        $"{prefix}.{Guid.NewGuid():N}@test.local";

    public static string UniqueKey(string prefix = "key") =>
        $"{prefix}.{Guid.NewGuid():N}";
}
