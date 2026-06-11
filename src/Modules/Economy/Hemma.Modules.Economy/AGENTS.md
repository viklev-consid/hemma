# AGENTS.md - Economy Module

This module owns household economy data: settings, accounts, categories, budgets, and later transactions/subscriptions/analytics.

Follow the repo-wide `/AGENTS.md` and `/src/Modules/AGENTS.md` rules. Economy is household-scoped and stores durable `HouseholdId` values as `Guid` columns in the `economy` schema. Do not query Households tables or add cross-schema foreign keys.

Phase 1 rules:

- Money is always represented by the `Money` value object in domain code.
- HTTP DTOs expose money as `{ amount, currency }`.
- v1 supports SEK by default, but currency must stay explicit.
- Categories are at most two levels deep.
- Budgets are per household and period.
- Settings are one per household and use `CycleStartDay` in the inclusive range 1-28.
