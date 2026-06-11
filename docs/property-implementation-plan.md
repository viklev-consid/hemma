# Property Module — Backend Agent Plan

**Repo:** `viklev-consid/hemma` · **Stack:** .NET 10, Wolverine, EF Core + PostgreSQL (schema `property`), TickerQ

This plan is self-contained — everything needed to implement the backend is here. (`property-module-architecture.md` exists as a human-facing overview; you do not need it to build.)

You own and publish the OpenAPI contract per phase. The frontend consumes the generated client only — it never reads this repo.

---

## Conventions

- New module `Property`, schema `property`, registered with the modular-monolith host the same way existing modules are. Scaffold with `dotnet new hemma-module --name Property` — it produces the DbContext (schema `property`), installer, Wolverine/endpoint extensions, telemetry, GDPR stubs, and the `.Contracts` project. Phase 0 fills in the rest.
- Every aggregate carries `HouseholdId`. Scoping is **not** a global query filter: endpoints authorize via `IScopedAuthorizationService<HouseholdScope>` and handlers filter explicitly with `.Where(x => x.HouseholdId == query.HouseholdId)`. Follow the pattern in any existing Economy feature handler/endpoint.
- **`Money` is shared.** The domain `Money` type lives in `Hemma.Shared.Kernel/Domain/` (extracted from Economy — see Prerequisites). Internal domain uses the shared `Money` (SEK). **Anything crossing a module boundary or appearing in OpenAPI uses `MoneyDto { decimal Amount, string Currency }`, which lives in `Hemma.Shared.Contracts` so Economy and Property serialise the *same* type — never expose the domain `Money` in a contract.**
- **Access control: all household members can do everything in v1.** Declare `PropertyPermissions { Read = "property.data.read", Write = "property.data.write" }` in `Property.Contracts` and register via `AddPermissions`. There are no owner-only or admin-only operations — every endpoint requires only household membership plus `Read` (queries) or `Write` (mutations), authorized through `IScopedAuthorizationService<HouseholdScope>` exactly like Economy.
- **Audit is event-based.** The global `AuditMiddleware` only *logs* handled messages. Durable audit rows require publishing a `PropertyMutationRecordedV1` integration event from mutating handlers (mirror `EconomyMutationRecordedV1`: `HouseholdId, Action, ResourceType, ResourceId, ActorId?, EventId`) **and** adding an `OnPropertyMutationRecordedHandler` subscriber in the Audit module that writes the `AuditEntry`.
- **No cross-schema FKs.** References into `economy` (and to household members) are `Guid` columns with no FK; cross-module data flows via messages only.

---

## Prerequisites (land before Phase 1 — both need sign-off)

These touch shared/Economy code outside the Property module, so they precede the module work and must be approved.

**Status:** Completed in `3796e31` (`refactor: move money primitives to shared projects`).

1. **Extract `Money` to the shared kernel.** Move the `Money` value object from `Hemma.Modules.Economy/Domain/` to `Hemma.Shared.Kernel/Domain/`, update all Economy references (domain, `MoneyDto` mapping, analytics, EF owned-type config) and Economy tests. No schema change — the EF owned-type columns (`amount numeric(18,2)`, `currency`) are unchanged. Verify Economy build + tests stay green.
2. **Add a shared `MoneyDto`** (`{ decimal Amount, string Currency }`) to `Hemma.Shared.Contracts`, so both Economy and Property reference one contract type for money on the wire.

## Phase 0 — Scaffold

**Status:** Completed in `3696fef` (`feat: scaffold property module foundation`).

- Create the `Property` module project, `property` schema, EF `DbContext`, and initial migration (mostly produced by `dotnet new hemma-module`).
- Wire household scoping, `PropertyPermissions` (`Read`/`Write`, registered via `AddPermissions`), Wolverine handler discovery, the `PropertyMutationRecordedV1` event + Audit subscriber, and GDPR participation (`PropertyPersonalDataEraser` + a household-deletion subscriber; Phase 4 fleshed this out for project/logbook blob deletion and user assignee scrubbing).

## Phase 1 — Project core

**Status:** Completed in `47bcd55` (`feat: add property project domain`), `78a6891`
(`feat: persist property projects`), `6b0f151` (`feat: expose property project operations`),
and `c9f06e8` (`test: cover property project api`).

> **Task breakdown:** `docs/property-phase-1-tasks.md` sequences this phase into ordered work items, locks the implicit micro-decisions (aggregate boundary, reorder semantics, attachment limits), and lists the tests. Generate the equivalent for later heavy phases at their kickoff.

**Entities (`property` schema):**

`Project`

| Field | Type / notes |
|---|---|
| `Id`, `HouseholdId` | uuid |
| `Name` | string |
| `Description?` | string |
| `Status` | enum: `Planning` / `Active` / `OnHold` / `Done` |
| `Area?` | string label (e.g. "Kitchen", "Exterior") |
| `TargetStartDate?`, `TargetEndDate?` | date; open-ended, no cycle/period semantics |
| `BudgetEstimate?` | `Money` (SEK) — the *estimate* only; actuals come from Economy |
| `CompletedAt?` | datetime; set on transition to `Done` |
| `Notes?` | string, freeform |

`ProjectTask` (flat — no sub-tasks)

| Field | Type / notes |
|---|---|
| `Id`, `ProjectId` | uuid |
| `Title` | string |
| `Status` | enum: `Todo` / `Doing` / `Done` |
| `Estimate?` | `Money` |
| `AssigneeId?` | household member uuid — stored as a bare nullable `Guid`, no FK, no cross-module validation (mirrors Economy's `PayerId`) |
| `DueDate?` | date |
| `SortOrder` | int |

`ProjectLink` — `Id`, `ProjectId`, `Label`, `Url`.

`ProjectAttachment` — `Id`, `ProjectId`, `BlobRef` (container + key from `IBlobStore.PutAsync`), plus standard metadata (filename, content type, size).

**Commands / handlers:**
- `CreateProject`, `UpdateProject`, `ChangeProjectStatus`, `DeleteProject` — `DeleteProject` publishes `ProjectDeletedV1 { HouseholdId, ProjectId, EventId }` from `Property.Contracts` so Economy can clear linked transactions (see Phase 2), and deletes the project's own attachment blobs.
- `AddTask`, `UpdateTask`, `ReorderTasks`, `DeleteTask`
- `AddLink` / `RemoveLink`
- `AddAttachment` / `RemoveAttachment` — the upload streams through the API to `IBlobStore.PutAsync` (model on Economy's [AttachReceipt.Handler.cs](src/Modules/Economy/Hemma.Modules.Economy/Features/AttachReceipt/AttachReceipt.Handler.cs): validate content-type + size, `PutAsync`, compensating `DeleteAsync` if the save fails); persist the returned `BlobRef` + metadata on `ProjectAttachment`. `RemoveAttachment` calls `IBlobStore.DeleteAsync`. **`IBlobStore` is single-phase — there is no presigned "issue target then confirm" flow; do not invent one.**

**Reading attachments back:** no module currently serves blobs (`IBlobStore.GetDownloadUrlAsync` is defined but unused). Add a `GetAttachmentContent { ProjectId, AttachmentId }` endpoint that streams the blob through the API via `IBlobStore.GetAsync` (household-authorized, returns the file with its stored content-type). This same serve path is reused for Logbook photos in Phase 4. (A presigned-URL path via `GetDownloadUrlAsync` is a possible later optimisation — out of scope for v1.)

**Completion behaviour:** `ChangeProjectStatus -> Done` sets `CompletedAt` and the response includes a **suggested Logbook payload** (see Phase 4 shape). Phase 4 added the `HistoryEntry` aggregate that consumes this payload. Project completion now snapshots linked Economy spend into `cost` and offers project attachment blob refs for copy-on-claim.

**Queries:** `GetProject`, `ListProjects` (filter by status/area), `GetProjectTasks`.

**Publish:** OpenAPI for the above.

## Phase 2 — Economy coupling

**Status:** Completed in `e8dcdd0` (`feat: link economy transactions to property projects`)
and `b4183d6` (`feat: expose property project budget from economy spend`).

> **Task breakdown:** `docs/property-phase-2-tasks.md` sequenced this phase and locked the
> Contracts-placement and spend-aggregation micro-decisions.

Changes in **both** the `Property` and `Economy` modules. Dependency directions: **Property references `Economy.Contracts`** to invoke the spend queries; **Economy references `Property.Contracts`** only to subscribe to `ProjectDeletedV1`. Neither references the other's internal project.

**In `Economy`:** (new query/command contract types live in `Economy.Contracts`)
- Migration: add `ProjectId uuid null` to `economy.transactions` (no FK), indexed for the project lookups below.
- Command `AssignTransactionToProject { TransactionId, ProjectId? }` (null clears). Driven by the frontend against Economy's own endpoint — Property does not call it.
- Query `GetProjectSpendSummary { HouseholdId, ProjectIds[] } -> [{ ProjectId, LinkedTotal: MoneyDto, TransactionCount }]`.
- Query `ListTransactionsForProject { ProjectId, Paging } -> existing transaction row shape`.
- Subscribe to `Property.Contracts`' `ProjectDeletedV1` (handler in Economy's `Integration/`) and null the `ProjectId` on all matching transactions, so links never dangle after a project is deleted.

**In `Property`:**
- `GetProjectBudget { ProjectId } -> { Estimate: MoneyDto?, LinkedTotal: MoneyDto, Remaining: MoneyDto?, TransactionCount }` — invokes `Economy.Contracts`' `GetProjectSpendSummary` via `IMessageBus.InvokeAsync<…>` and combines with the stored estimate.
- `ProjectDeletedV1` is already published by `DeleteProject` (Phase 1); no extra work here beyond Economy's subscriber.

**Publish:** Economy publishes the assign command + spend queries (in `Economy.Contracts`); Property publishes `GetProjectBudget` and `ProjectDeletedV1`.

## Phase 3 — Maintenance

**Status:** Completed in `1f7a3ab` (`feat: add property maintenance domain`),
`95178d0` (`feat: persist property maintenance plans and occurrences`),
`087e5c4` (`feat: expose household members query from households contracts`),
`91cd4c6` (`feat: add property maintenance plans, occurrences, and scheduling`),
and `34add9f` (`test: cover property maintenance api and scheduling`).

> **Task breakdown:** `docs/property-phase-3-tasks.md` sequenced this phase and locked the
> aggregate-boundary, materialisation, recurrence, and reminder-idempotency micro-decisions.
> Note: `MaintenancePlan` and `MaintenanceOccurrence` are **separate household-scoped aggregate
> roots** (not parent/child); each active plan keeps exactly one `Upcoming` occurrence (created on
> plan creation, on complete/skip/promote, and healed by the daily job), and `LeadTimeDays` gates
> the *reminder*, not the occurrence's *existence*.

**Entities** (schedule/task only — **no cost fields** in v1):

`MaintenancePlan`

| Field | Type / notes |
|---|---|
| `Id`, `HouseholdId` | uuid |
| `Title` | string |
| `Description?` | string |
| `Area?` | string |
| `RecurrenceUnit` | enum: `Month` / `Year` |
| `RecurrenceInterval` | int (e.g. 6 + `Month` = twice a year; 1 + `Year` anchored on a date = "every autumn") |
| `AnchorDate` | date — basis for computing the next due date |
| `LeadTimeDays` | int — how far ahead to materialise/notify (default e.g. 14) |
| `IsActive` | bool |

`MaintenanceOccurrence`

| Field | Type / notes |
|---|---|
| `Id`, `PlanId`, `HouseholdId` | uuid |
| `DueDate` | date |
| `Status` | enum: `Upcoming` / `Done` / `Skipped` |
| `CompletedAt?` | datetime |
| `Notes?` | string |
| `SpawnedProjectId?` | uuid — set when promoted to a Project (no cross-schema FK) |

**Commands / handlers:**
- `CreateMaintenancePlan`, `UpdateMaintenancePlan`, `DeactivatePlan`, `DeletePlan`
- `CompleteOccurrence { OccurrenceId, Notes? }`, `SkipOccurrence`
- `PromoteOccurrenceToProject { OccurrenceId, <CreateProject fields> }` — creates a `Project`, sets `SpawnedProjectId`, marks the occurrence `Done` (handled-via-project, **no** suggested Logbook payload — the Project will emit on its own completion).

**Scheduling (TickerQ):**
- A recurring job ensures the next `Upcoming` occurrence exists within `LeadTimeDays` of each active plan.
- **Compute `DueDate` by stepping `AnchorDate` forward in `RecurrenceInterval × RecurrenceUnit` increments until the first date that is today-or-later — no backfill.** If a plan's anchor (or a lapsed job) means several periods have already passed, materialise only that single next future occurrence; never create a row per missed period.
- On `CompleteOccurrence`/`SkipOccurrence`, schedule the next occurrence the same way (next future due date, not anchored to the completion date).
- Fire reminders through the **Notifications** module as occurrences enter the lead-time window. **Recipients are all household members.** Households does not currently expose a members query (only member events), so **add a `ListHouseholdMembers { HouseholdId } -> [{ UserId, … }]` query to `Households.Contracts`** and invoke it via `IMessageBus`; send one `CreateNotificationCommand` per member (`Category = Product`, `Severity = Info`/`Warning`, optional `Link` to the occurrence). Because the job runs daily, derive each command's `IdempotencyKey` **deterministically** from `(OccurrenceId, RecipientUserId)` so re-runs never double-notify.

**Completion payload:** `CompleteOccurrence` (non-promoted) returns a suggested Logbook payload (type `Maintenance`, no cost, title/area from the plan).

**Queries:** `ListMaintenancePlans`, `GetPlan`, `ListUpcomingOccurrences { HouseholdId, Horizon }`.

**Publish:** OpenAPI for the above.

## Phase 4 — Logbook

**Status:** Completed in `09c06c8` (`feat: add property logbook`).

**Entity:** `HistoryEntry` — a **durable** aggregate, deliberately not a live query over Projects/Maintenance. The resale record must survive edits or deletion of the originating project.

| Field | Type / notes |
|---|---|
| `Id`, `HouseholdId` | uuid |
| `Date` | date — UI groups by year |
| `Title` | string |
| `Area?` | string |
| `Cost?` | `Money` **snapshot** — captured at creation, never recomputed from live transactions |
| `Type` | enum: `Project` / `Maintenance` / `Manual` |
| `SourceProjectId?` | uuid — provenance only; nullable; no cross-schema FK |
| `SourceMaintenanceOccurrenceId?` | uuid — provenance only; nullable |
| `PhotoRefs` | blob references the entry **owns** (snapshotted) |

**Suggested-entry payload (returned by Project & Maintenance completion):**
```
SuggestedHistoryEntry {
  date: Date            // CompletedAt
  title: string         // project/plan name
  area?: string
  cost?: MoneyDto       // project linked-spend snapshot at completion; null for maintenance
  type: "Project" | "Maintenance"
  sourceProjectId?: uuid
  sourceMaintenanceOccurrenceId?: uuid
  photoRefs: BlobRef[]  // project attachments offered for inclusion; empty for maintenance
}
```

**Commands / handlers:**
- `CreateHistoryEntry` — accepts the (possibly edited) suggested payload **or** a fully manual entry (type `Manual`). On create, **copy** each offered blob into a HistoryEntry-owned container via `IBlobStore.GetAsync` → `PutAsync`, and store the *new* `BlobRef`s on the entry.
- `UpdateHistoryEntry`, `DeleteHistoryEntry` (delete also deletes the entry's *own* copied blobs).

**Durability rule:** `CreateHistoryEntry` snapshots `cost` (the `LinkedTotal` value at completion) and **physically copies** `photoRefs` into entry-owned blobs. Because the entry owns independent copies, it never recomputes from live Economy data and is unaffected by later edits/deletion of the source project.

**Queries:** `ListHistory { HouseholdId, Year?, Area?, Type? }` returning entries newest-first (frontend groups by year). Serving a `HistoryEntry` photo reuses the Phase 1 stream-through-API pattern — add a `GetHistoryPhoto { HistoryEntryId, BlobKey }` endpoint backed by `IBlobStore.GetAsync`.

**Blob ownership (why copy, not reference):** `IBlobStore` has no reference-counting or two-phase lifecycle, and deletion is unconditional. Copy-on-claim sidesteps all of it — Project/attachment deletion stays simple and unconditional (it only ever touches its own blobs), and the `HistoryEntry` copies survive independently. **Do not** build ref-aware deletion or a shared retention mechanism.

**Publish:** OpenAPI for the above.

---

## GDPR

Property data is **household-scoped, not user-scoped**, so user erasure and household deletion are handled differently — do not conflate them:

- **User erasure** (`UserErasureRequestedV1`, via `PropertyPersonalDataEraser` — see Economy's `OnUserErasureRequestedHandler`): a single member leaving must **not** delete the household's projects/maintenance/logbook. Only scrub that user's personal footprint — null `ProjectTask.AssigneeId` where it equals the user. (Property stores no other per-user PII; attachments/photos belong to the household, not the uploading user.)
- **Household deletion** (`HouseholdDeletedV1` from `Households.Contracts`, via an `Integration/` subscriber): this is the cascade — delete **all** `property` aggregates for that household and **all blobs they own**, both `ProjectAttachment` blobs *and* the independently-copied `HistoryEntry.PhotoRefs`.

## Definition of done (per phase)

Migrations applied · handlers + queries covered by tests · Audit emitted on mutations · OpenAPI published and versioned.
