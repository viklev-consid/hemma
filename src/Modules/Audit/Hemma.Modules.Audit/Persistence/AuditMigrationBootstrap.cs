using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Audit.Persistence;

public static class AuditMigrationBootstrap
{
    public static Task EnsureRenamedHouseholdMigrationHistoryAsync(AuditDbContext db, CancellationToken ct = default) =>
        db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF to_regclass('audit.__ef_migrations_history') IS NOT NULL
                   AND EXISTS (
                       SELECT 1
                       FROM information_schema.columns
                       WHERE table_schema = 'audit'
                         AND table_name = '__ef_migrations_history'
                         AND column_name = 'migration_id') THEN
                    UPDATE audit.__ef_migrations_history
                    SET migration_id = '20260521122540_AddHouseholdScopeToAudit'
                    WHERE migration_id = '20260521122540_AddOrganizationScopeToAudit';
                END IF;
            END $$;
            """, ct);
}
