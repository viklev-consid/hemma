# Property — Phase 3 Task Checklist

> Companion to `docs/property-implementation-plan.md` (the design spec). This is the **sequenced task breakdown** for Phase 3 (Maintenance), generated at phase kickoff. It orders the work, names the files, and lists the tests, and locks the micro-decisions the master plan left implicit.
>
> **Before starting:** Phases 0–2 must be merged. Read `/CLAUDE.md`, `src/Modules/CLAUDE.md`, the `vertical-slice`, `rich-domain-model`, and `wolverine-messaging` skills. Model the recurring job on Economy's `RunDueBillsJob`/`RunDueBillsHandler` and `TrialRenewalReminderJob`; model the recurrence/occurrence shape on Economy's `RecurringBill`; reuse the Phase 1 `PropertyAuditPublisher` and `SuggestedHistoryEntryResponse`.

**Status:** In progress.

> Phase 3 stays **entirely inside the Property module** plus one additive contract on Households. It reuses `PropertyPermissions` (`Read`/`Write`), the `property` schema + `PropertyDbContext`, and `PropertyMutationRecordedV1` + the existing Audit subscriber — it does **not** introduce a Maintenance module, schema, permission set, or audit event. New cross-module edges: **Property → `Notifications.Contracts`** (send `CreateNotificationCommand`) and **Property → `Households.Contracts`** (invoke the new `ListHouseholdMembersQuery`). Property already references `Households.Contracts`; the `Notifications.Contracts` reference is new.

---

## Micro-decisions locked for this phase

These were implicit in the master plan; resolve them this way and don't re-litigate mid-build:

1. **`MaintenancePlan` and `MaintenanceOccurrence` are two separate aggregate roots**, both carrying `HouseholdId`. (Contrast with Phase 1, where `ProjectTask` is a *child* of `Project`.) The plan's table gives `MaintenanceOccurrence` its own `HouseholdId`, `ListUpcomingOccurrences` queries occurrences across *all* plans for a household, and complete/skip/promote act on one occurrence without needing the whole plan loaded — all signals of an independent root. The occurrence references its plan by `MaintenancePlanId` (a `Guid` typed-id column **within the same `property` schema**); there is no navigation collection on the plan.

2. **One-open-occurrence invariant.** Each *active* plan has **exactly one** `Upcoming` occurrence at any time. It is created (a) when the plan is created active, (b) when the current occurrence is completed/skipped/promoted (the next one), and (c) healed by the daily job if missing. A **unique index `(PlanId, DueDate)`** guards against duplicate materialisation; the job swallows the unique-violation like `RunDueBillsHandler` does.

3. **`DueDate` computation is a pure domain method** `MaintenancePlan.NextDueOnOrAfter(DateOnly floor)`: start at `AnchorDate`, step forward by `RecurrenceInterval × RecurrenceUnit` (`AddMonths`/`AddYears`) until the first date `>= floor`. **No backfill** — only the single next date is returned, never one row per missed period. Initial materialisation uses `floor = today`; post-completion uses `floor = max(today, completedDueDate.AddDays(1))` so the next is strictly later and still today-or-later.

4. **Materialisation is eager; `LeadTimeDays` gates *notification*, not *existence*.** The master plan says both "ensure the next Upcoming occurrence exists … within LeadTimeDays" *and* "on complete/skip, schedule the next occurrence … (next future due date)". The only globally consistent reading: an active plan **always** has its next occurrence materialised (so `ListUpcomingOccurrences { Horizon }` is meaningful for any horizon), and the daily job **fires reminders when an existing Upcoming occurrence's `DueDate` is within its plan's `LeadTimeDays` of today**. (Flagged to the user — this resolves an apparent tension in the spec.)

5. **Reminder recipients = all active household members.** The daily job invokes the **new** `Households.Contracts.ListHouseholdMembersQuery { HouseholdId } -> [{ UserId }]` (active, non-anonymised members) and sends **one `CreateNotificationCommand` per member** (`Category = Product`, `Severity = Info`, `Link` to the occurrence). Each command's `IdempotencyKey` is a **deterministic GUID derived from `(OccurrenceId, RecipientUserId)`** (SHA-256, UUIDv5-style) so the daily re-run never double-notifies — dedup happens at the Notifications layer via its existing unique constraint. No `ReminderSent` flag on the occurrence is needed.

6. **Audit reuses `PropertyMutationRecordedV1`.** Resource types `MaintenancePlan` and `MaintenanceOccurrence`; actions `property.maintenance.plan_created` / `_updated` / `_deactivated` / `_deleted`, `property.maintenance.occurrence_completed` / `_skipped` / `_promoted`. No new audit event or subscriber. Job-materialised occurrences and reminders are **not** audited (they are system actions with no actor; mirrors the recurring-bill job).

7. **Promote = completion-with-a-project.** `PromoteOccurrenceToProject` creates a `Project` (via the Phase 1 `Project.Create`) from the supplied CreateProject fields, sets `SpawnedProjectId`, marks the occurrence `Done` (sets `CompletedAt`), and **schedules the next occurrence** (the plan keeps recurring). It returns **no** suggested-Logbook payload — the spawned Project emits its own on completion. The response includes the created `ProjectResponse`.

8. **`CompleteOccurrence` (non-promoted) returns a suggested-Logbook payload** reusing the Phase 1 `SuggestedHistoryEntryResponse` (`Type = "Maintenance"`, `Cost = null`, `Title`/`Area` from the plan, `SourceMaintenanceOccurrenceId` set, `PhotoRefs` empty). The `HistoryEntry` that consumes it still lands in Phase 4; the payload is returned, not persisted.

9. **`DeactivatePlan` vs `DeletePlan`.** Deactivate sets `IsActive = false` (scheduling stops; history kept). Delete removes the plan **and bulk-deletes its occurrences** (handler-managed, within-schema). No reactivation command in v1 (`UpdateMaintenancePlan` edits details only).

---

## Work items (in order)

### 1. Domain — Maintenance aggregates
- [ ] `Domain/MaintenancePlanId.cs`, `MaintenanceOccurrenceId.cs` (typed IDs).
- [ ] `Domain/MaintenanceRecurrenceUnit.cs` (`Month`/`Year`), `MaintenanceOccurrenceStatus.cs` (`Upcoming`/`Done`/`Skipped`).
- [ ] `Domain/MaintenancePlan.cs` — `Create`, `UpdateDetails`, `Deactivate`, pure `NextDueOnOrAfter(floor)`. Validates title (≤160, required), description (≤2000), area (≤100), `RecurrenceInterval` (1–120), `LeadTimeDays` (0–365).
- [ ] `Domain/MaintenanceOccurrence.cs` — `Schedule(plan, dueDate)` factory, `Complete(notes, clock)`, `Skip(notes, clock)`, `PromoteToProject(projectId, clock)`; each rejects unless `Upcoming`.
- [ ] `PropertyErrors` additions (plan not-found/invalid, recurrence invalid, lead-time invalid, occurrence not-found/not-open).
- **Done when:** unit tests cover recurrence stepping (month/year; anchor in past → next future; anchor in future → anchor), validation, and occurrence transitions. No infra usings in `Domain/`.

### 2. Persistence
- [ ] `Persistence/Configurations/MaintenancePlanConfiguration.cs` (indexes `HouseholdId`, `(HouseholdId, IsActive)`).
- [ ] `Persistence/Configurations/MaintenanceOccurrenceConfiguration.cs` (indexes `(HouseholdId, Status, DueDate)`, `(PlanId, Status)`, **unique `(PlanId, DueDate)`**).
- [ ] `DbSet<MaintenancePlan>` + `DbSet<MaintenanceOccurrence>` on `PropertyDbContext`.
- [ ] Migration `Phase3Maintenance` (PropertyDbContext).
- **Done when:** migration applies cleanly; arch tests green.

### 3. Households — members contract query
- [ ] `Households.Contracts/Queries/ListHouseholdMembersQuery.cs` — `ListHouseholdMembersQuery(Guid HouseholdId)`, `ListHouseholdMembersResult(IReadOnlyList<HouseholdMemberInfo>)`, `HouseholdMemberInfo(Guid UserId)`.
- [ ] Handler in Households returning active, non-anonymised member user ids; register in `AddHouseholdsHandlers`.
- **Done when:** Property can invoke it via `IMessageBus`; no Property→Households internal reference.

### 4. Slices — Maintenance (folder `Features/Maintenance/`)
- [ ] `CreateMaintenancePlan` (materialises first occurrence if active), `UpdateMaintenancePlan`, `DeactivatePlan`, `DeletePlan`.
- [ ] `CompleteOccurrence` (returns suggested Logbook payload + schedules next), `SkipOccurrence` (schedules next).
- [ ] `PromoteOccurrenceToProject` (creates Project, sets `SpawnedProjectId`, schedules next, returns project).
- [ ] Queries `ListMaintenancePlans`, `GetPlan`, `ListUpcomingOccurrences { HouseholdId, HorizonDays }`.
- [ ] All endpoints under `/v1/property/maintenance/...`, `HouseholdScope` + `Read`/`Write`, audit on mutations.
- **Done when:** integration tests cover create→materialise→list, complete (payload + next), skip, promote, deactivate/delete, cross-household 403.

### 5. Scheduling job (TickerQ)
- [ ] `Property.csproj` references `Notifications.Contracts`.
- [ ] `Jobs/MaterializeMaintenanceOccurrences.cs` (command), `MaterializeMaintenanceOccurrencesJob.cs` (`[TickerFunction]`, daily), `MaterializeMaintenanceOccurrencesHandler.cs` (heal + notify).
- [ ] Deterministic idempotency-key helper from `(OccurrenceId, UserId)`.
- [ ] `AddPropertyJobs` extension + `PropertyModuleInstaller.ConfigureJobs`; register the handler in `AddPropertyHandlers`.
- **Done when:** integration test runs the command, asserts one notification per member, and asserts a second run produces no duplicates.

### 6. Registration, GDPR, publish
- [ ] Register `MaintenanceHandler` + `MaterializeMaintenanceOccurrencesHandler` in `AddPropertyHandlers`; map `MaintenanceEndpoint`.
- [ ] `PropertyPersonalDataEraser.EraseHouseholdAsync` deletes maintenance plans + occurrences for the household (maintenance owns no blobs).
- [ ] Confirm OpenAPI reflects the new endpoints via full build/test.

---

## Test ledger (Phase 3 DoD)
- **Unit (Domain):** recurrence `NextDueOnOrAfter` (month/year, past/future anchor); plan + occurrence validation; occurrence transitions (complete/skip/promote, reject when not Upcoming).
- **Integration (per slice):** plan create materialises first occurrence; list plans/upcoming; complete returns Maintenance suggested payload and schedules next; skip schedules next; promote creates project + sets `SpawnedProjectId`; materialise job heals + notifies all members and is idempotent across runs; deactivate stops scheduling; delete removes plan + occurrences; cross-household 403.
- **Arch (automatic):** domain purity, endpoints depend only on `IMessageBus`, slice co-location, module boundaries (Property → Notifications/Households `.Contracts` only).

## Definition of done
Migrations applied · handlers + queries covered by tests · Audit emitted on mutations · OpenAPI published · all tests green.
