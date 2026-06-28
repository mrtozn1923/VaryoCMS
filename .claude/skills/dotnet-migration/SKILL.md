---
name: dotnet-migration
description: >
  Use when writing SQL Server migration scripts for Varyo CMS.
  Triggers on: "migration yaz", "write migration", "yeni tablo", "alter table",
  "create table", "index ekle", "schema değişikliği", "db/migrations".
  Always produces numbered SQL files following project conventions.
---

# Varyo CMS Migration Writer

## Reference
- Full schema: @docs/database-schema.md
- Migration folder: db/migrations/

## Migration File Template

```sql
-- db/migrations/NNN_description.sql
-- Description: [What this migration does]
-- Created: [date]

-- =============================================
-- [Table / Feature Name]
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'table_name')
BEGIN
    CREATE TABLE table_name (
        id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_table_name PRIMARY KEY,
        tenant_id   INT NOT NULL CONSTRAINT FK_table_name_tenants REFERENCES tenants(id),
        
        -- domain columns here --
        
        created_at  DATETIME2 NOT NULL CONSTRAINT DF_table_name_created_at DEFAULT GETUTCDATE(),
        updated_at  DATETIME2 NOT NULL CONSTRAINT DF_table_name_updated_at DEFAULT GETUTCDATE(),
        is_deleted  BIT NOT NULL CONSTRAINT DF_table_name_is_deleted DEFAULT 0
    );
    PRINT 'Created table: table_name';
END
ELSE
    PRINT 'Table already exists: table_name';
GO

-- Indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_table_name_tenant_id')
    CREATE NONCLUSTERED INDEX IX_table_name_tenant_id ON table_name (tenant_id) WHERE is_deleted = 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_table_name_slug')
    CREATE UNIQUE NONCLUSTERED INDEX IX_table_name_slug ON table_name (tenant_id, slug) WHERE is_deleted = 0;
GO
```

## Required Indexes (always create these)

For every table with `tenant_id`:
```sql
CREATE NONCLUSTERED INDEX IX_{table}_tenant_id ON {table} (tenant_id) WHERE is_deleted = 0;
```

For tables with `slug`:
```sql
CREATE UNIQUE NONCLUSTERED INDEX IX_{table}_slug ON {table} (tenant_id, slug) WHERE is_deleted = 0;
```

For foreign keys:
```sql
CREATE NONCLUSTERED INDEX IX_{table}_{fk_column} ON {table} ({fk_column});
```

## ALTER TABLE Pattern (for adding columns to existing tables)
```sql
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('table_name') AND name = 'new_column')
BEGIN
    ALTER TABLE table_name ADD new_column NVARCHAR(200) NULL;
    PRINT 'Added column new_column to table_name';
END
GO
```

## Migration Runner (simple custom runner)
```csharp
// tools/MigrationRunner/Program.cs
// Reads files from db/migrations/ ordered by name
// Tracks applied migrations in __migrations_history table
// Run: dotnet run --project tools/MigrationRunner -- up
```

## Migration Numbering
```
001_init_tenants_users.sql
002_content_types_fields.sql
003_content_items_values.sql
004_media_assets.sql
005_languages_dictionary.sql
006_api_management.sql
007_indexes_constraints.sql
008_seed_dev_tenant.sql        ← dev-only seed data
```

## Never
- Never modify an already-applied migration → create a new one
- Never use DROP TABLE in migrations (use is_deleted soft flag)
- Never hardcode tenant IDs in migrations (except seed files)
