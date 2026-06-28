using System.Data.Common;
using Dapper;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

public class ContentFieldValueRepository : BaseRepository, IContentFieldValueRepository
{
    private const string Columns =
        "id, tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, value_date_end, value_media_id, created_at, updated_at";

    private readonly ITenantContext _tenantContext;

    public ContentFieldValueRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
        : base(connectionFactory)
        => _tenantContext = tenantContext;

    public async Task<IReadOnlyList<ContentFieldValue>> GetByItemAsync(
        int contentItemId, string languageCode, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {Columns} FROM content_field_values
               WHERE content_item_id = @ItemId AND tenant_id = @TenantId
                 AND language_code IN (@Lang, 'all')",
            new { ItemId = contentItemId, TenantId = _tenantContext.TenantId, Lang = languageCode },
            cancellationToken: ct);
        var rows = await conn.QueryAsync<ContentFieldValue>(command);
        return rows.AsList();
    }

    public async Task SaveValuesAsync(
        int contentItemId, IReadOnlyList<ContentFieldValue> values, CancellationToken ct = default)
    {
        if (values.Count == 0) return;

        using var conn = (DbConnection)CreateConnection();
        await conn.OpenAsync(ct);
        using var tx = await conn.BeginTransactionAsync(ct);

        foreach (var v in values)
        {
            var command = new CommandDefinition(
                @"MERGE content_field_values AS t
                  USING (SELECT @ItemId AS content_item_id, @FieldId AS content_field_id, @Lang AS language_code) AS s
                  ON t.content_item_id = s.content_item_id
                     AND t.content_field_id = s.content_field_id
                     AND t.language_code = s.language_code
                  WHEN MATCHED THEN UPDATE SET
                      value_text = @ValueText, value_number = @ValueNumber, value_bool = @ValueBool,
                      value_date = @ValueDate, value_date_end = @ValueDateEnd, value_media_id = @ValueMediaId,
                      updated_at = GETUTCDATE()
                  WHEN NOT MATCHED THEN INSERT
                      (tenant_id, content_item_id, content_field_id, language_code,
                       value_text, value_number, value_bool, value_date, value_date_end, value_media_id)
                      VALUES (@TenantId, @ItemId, @FieldId, @Lang,
                       @ValueText, @ValueNumber, @ValueBool, @ValueDate, @ValueDateEnd, @ValueMediaId);",
                new
                {
                    TenantId = _tenantContext.TenantId,
                    ItemId = contentItemId,
                    FieldId = v.ContentFieldId,
                    Lang = v.LanguageCode,
                    v.ValueText,
                    v.ValueNumber,
                    v.ValueBool,
                    v.ValueDate,
                    v.ValueDateEnd,
                    v.ValueMediaId
                },
                transaction: tx, cancellationToken: ct);
            await conn.ExecuteAsync(command);
        }

        await tx.CommitAsync(ct);
    }
}
