using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Households.Persistence;

public static class HouseholdsMigrationBootstrap
{
    public static Task EnsureRenamedFromOrganizationsAsync(HouseholdsDbContext db, CancellationToken ct = default) =>
        db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = 'organizations')
                   AND NOT EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = 'households') THEN
                    ALTER SCHEMA organizations RENAME TO households;

                    IF to_regclass('households.organizations') IS NOT NULL THEN
                        ALTER TABLE households.organizations RENAME TO households;
                    END IF;

                    IF to_regclass('households.organization_memberships') IS NOT NULL THEN
                        ALTER TABLE households.organization_memberships RENAME TO household_memberships;
                    END IF;

                    IF to_regclass('households.organization_invitations') IS NOT NULL THEN
                        ALTER TABLE households.organization_invitations RENAME TO household_invitations;
                    END IF;
                END IF;

                IF to_regclass('households.__ef_migrations_history') IS NOT NULL
                   AND EXISTS (
                       SELECT 1
                       FROM information_schema.columns
                       WHERE table_schema = 'households'
                         AND table_name = '__ef_migrations_history'
                         AND column_name = 'migration_id') THEN
                    UPDATE households.__ef_migrations_history
                    SET migration_id = CASE migration_id
                        WHEN '20260521121917_AddOrganizations' THEN '20260521121917_AddHouseholds'
                        WHEN '20260521211618_AnonymizeOrganizationUserReferences' THEN '20260521211618_AnonymizeHouseholdUserReferences'
                        WHEN '20260602120000_AddOrganizationOwnerMutationConcurrency' THEN '20260602120000_AddHouseholdOwnerMutationConcurrency'
                        ELSE migration_id
                    END
                    WHERE migration_id IN (
                        '20260521121917_AddOrganizations',
                        '20260521211618_AnonymizeOrganizationUserReferences',
                        '20260602120000_AddOrganizationOwnerMutationConcurrency');
                END IF;
            END $$;
            """, ct);
}
