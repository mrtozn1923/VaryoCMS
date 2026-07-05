-- 006_widen_login_code_hash.sql
-- 2FA login codes are now stored HASHED at rest (BCrypt, 60 chars) instead of the 6-digit plaintext.
-- The `code` column was NVARCHAR(10) (sized for the plaintext) — widen it to hold a BCrypt hash.
-- Additive + idempotent: safe to re-run; only alters if still narrower than needed.
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('login_codes')
      AND name = 'code'
      AND max_length < 200   -- NVARCHAR(100) = 200 bytes; NVARCHAR(10) = 20 bytes
)
BEGIN
    ALTER TABLE login_codes ALTER COLUMN code NVARCHAR(100) NOT NULL;
    PRINT 'Widened login_codes.code to NVARCHAR(100) for hashed 2FA codes.';
END
ELSE
    PRINT 'login_codes.code already wide enough — no change.';
GO
