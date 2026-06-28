namespace VaryoCms.Domain.Interfaces.Repositories;

// A resolved query for the public API list endpoint. Column names are pre-validated by the caller
// (service) against a fixed whitelist; field ids are ints — both are safe to inline. Values are parameterized.
public sealed record ApiItemQuery(
    int TenantId,
    int ContentTypeId,
    string Status,
    string Lang,
    int Skip,
    int Take,
    string? SortItemColumn,   // item column (e.g. created_at) when sorting by a built-in column
    int? SortFieldId,         // content field id when sorting by an EAV field value
    string? SortFieldColumn,  // value_* column for the sort field
    bool SortDesc,
    IReadOnlyList<ApiItemFilter> Filters);

// One equality filter on an EAV field value.
public sealed record ApiItemFilter(int FieldId, string Column, object? Value);
