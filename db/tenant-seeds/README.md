# Tenant seeds

Put environment-specific content for one real tenant here (e.g. `<tenant>-seed.sql`).
These files are **git-ignored**: they are not part of the migration chain, are not applied by
the test suite, and should never be committed (they usually contain real, non-public data).

Apply a tenant seed manually against the target database after `001`–`004` (and optionally `003`).
