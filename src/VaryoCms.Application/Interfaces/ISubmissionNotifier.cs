namespace VaryoCms.Application.Interfaces;

// Sends a "new form submission" notification email to the tenant when a content item is created
// via the public API on a content type flagged notify_on_create (e.g. contact-message).
// Best-effort: implementations must never throw — a mail failure must not break the submission.
public interface ISubmissionNotifier
{
    Task NotifyNewSubmissionAsync(
        int tenantId, string contentTypeSlug, string? title,
        IReadOnlyList<(string Label, string Value)> fields, CancellationToken ct = default);
}
