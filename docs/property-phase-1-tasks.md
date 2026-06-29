# Property — Phase 1 Task Checklist

> Companion to `docs/property-implementation-plan.md` (the design spec). This is the **sequenced task breakdown** for Phase 1, generated at phase kickoff. It does not re-derive design — it orders the work, names the files, and lists the tests. It also locks the few micro-decisions the master plan left implicit.
>
> **Before starting:** Prerequisites + Phase 0 must be merged. Read `/CLAUDE.md`, `src/Modules/CLAUDE.md`, the `vertical-slice` and `rich-domain-model` skills, and `docs/how-to/add-a-slice.md`. Model blob work on Economy's `AttachReceipt` slice and audit work on `EconomyMutationRecordedV1` + `EconomyAuditPublisher`.

**Status:** Completed. Implemented in the Phase 1 commit series ending at `c9f06e8`
(`test: cover property project api`). Property read/write scoped permissions are granted
to household owners and members through the household role permission map, then enforced
by the Property endpoints with `PropertyPermissions.Read` / `PropertyPermissions.Write`.

---

## Micro-decisions locked for this phase

These were implicit in the master plan; resolve them this way and don't re-litigate mid-build:

1. **`Project` is the aggregate root.** `ProjectTask`, `ProjectLink`, and `ProjectAttachment` are **child entities owned by `Project`** (they carry `ProjectId`, not `HouseholdId`). Mutations load the `Project` aggregate and call methods on it (`project.AddTask(...)`, `project.Reorder(...)`) — handlers orchestrate, the aggregate enforces invariants. EF maps children as owned/related collections under the same `DbContext`.
2. **Typed IDs:** `ProjectId`, `ProjectTaskId`, `ProjectLinkId`, `ProjectAttachmentId` (Guid wrappers, per repo convention).
3. **`ReorderTasks` contract:** client sends the full ordered list of `ProjectTaskId`s; the aggregate reassigns `SortOrder` 0..n. Reject if the set doesn't match the project's current task IDs.
4. **Attachment limits (reuse AttachReceipt's):** allow `application/pdf`, `image/jpeg`, `image/png`, `image/webp`; max 10 MB; reject otherwise with a `PropertyErrors` value.
5. **`SuggestedHistoryEntry.photoRefs`** started as lightweight attachment metadata in Phase 1. Phase 4 changed the wire shape to `{ container, key }` so `CreateHistoryEntry` can copy offered blobs via `IBlobStore.GetAsync` -> `PutAsync` without coupling Logbook creation to live project attachment rows.
6. **`ChangeProjectStatus -> Done`** is the only transition that sets `CompletedAt` and returns the suggested payload. Other transitions just change `Status`. `Done -> *` (reopen) clears `CompletedAt`.

---

## Work items (in order)

Each item: scaffold → fill → test. "Done when" is the acceptance check.

### 1. Domain — `Project` aggregate
- [x] `Domain/ProjectId.cs`, `ProjectTaskId.cs`, `ProjectLinkId.cs`, `ProjectAttachmentId.cs` (typed IDs).
- [x] `Domain/Project.cs` — root: `Create(...)` returns `ErrorOr<Project>`; methods `Rename/UpdateDetails`, `ChangeStatus(status, clock)`, `AddTask/UpdateTask/RemoveTask/Reorder`, `AddLink/RemoveLink`, `AddAttachment/RemoveAttachment`. No public setters.
- [x] `Domain/ProjectStatus.cs` (`Planning/Active/OnHold/Done`), `ProjectTaskStatus.cs` (`Todo/Doing/Done`).
- [x] `Domain/ProjectTask.cs`, `ProjectLink.cs`, `ProjectAttachment.cs` (child entities).
- [x] Uses shared-kernel `Money` for `BudgetEstimate` / task `Estimate`.
- **Done when:** unit tests cover invariants (status transitions, `CompletedAt` set/clear on `Done`, reorder validation, attachment limits) and pass. No infra usings in `Domain/` (arch test).

### 2. Persistence
- [x] `Persistence/Configurations/ProjectConfiguration.cs` (+ owned `Money`, child collections, indexes on `HouseholdId`, `(HouseholdId, Status)`, `(HouseholdId, Area)`).
- [x] Add `DbSet<Project>` to `PropertyDbContext`.
- [x] Migration: `dotnet ef migrations add Phase1ProjectCore` (see `ef-migration` skill).
- **Done when:** migration applies cleanly to a fresh DB; arch tests green.

### 3. Cross-cutting — audit publisher
- [x] `Integration/PropertyAuditPublisher.cs` + `Property.Contracts/Events/PropertyMutationRecordedV1.cs` (mirror Economy). Each mutating handler publishes it.
- **Done when:** an integration test asserts a mutation produces a `PropertyMutationRecordedV1` and the Audit module writes an `AuditEntry` (add the `OnPropertyMutationRecordedHandler` subscriber in Audit if Phase 0 didn't).

### 4. Slices — Project CRUD + status  *(scaffold each with `dotnet new hemma-slice`/`hemma-query-slice`)*
- [x] `CreateProject` (command) — publishes mutation event.
- [x] `GetProject` (query) — household-scoped.
- [x] `ListProjects` (query) — filter by `status`/`area`.
- [x] `UpdateProject` (command).
- [x] `ChangeProjectStatus` (command) — sets `CompletedAt` on `Done`; response includes `SuggestedHistoryEntry` (returned, not persisted — see master plan).
- [x] `DeleteProject` (command) — deletes the project's own attachment blobs via `IBlobStore.DeleteAsync`, then publishes `ProjectDeletedV1 { HouseholdId, ProjectId, EventId }`.
- **Done when:** each endpoint requires `HouseholdScope` + `Read`/`Write`; integration tests cover happy path + cross-household 403 + not-found.

### 5. Slices — Tasks
- [x] `AddTask`, `UpdateTask`, `DeleteTask`, `ReorderTasks` (commands).
- [x] `GetProjectTasks` (query).
- **Done when:** reorder test asserts `SortOrder` reassigned and mismatched-set rejected; assignee stored as bare `Guid` (no validation).

### 6. Slices — Links
- [x] `AddLink`, `RemoveLink` (commands).
- **Done when:** URL validated in `Validator`; integration tests pass.

### 7. Slices — Attachments (blob upload + serve)
- [x] `AddAttachment` (command) — stream → validate (decision #4) → `IBlobStore.PutAsync` → store `BlobRef` + metadata; **compensating `DeleteAsync` if `SaveChanges` fails** (copy AttachReceipt).
- [x] `GetAttachmentContent` (query/endpoint) — **net-new serve pattern**: household-authorized, resolves `(projectId, attachmentId)` → `IBlobStore.GetAsync`, returns file stream with stored content-type. No precedent in repo — this is the reference implementation reused in Phase 4.
- [x] `RemoveAttachment` (command) — `IBlobStore.DeleteAsync` + remove child.
- **Done when:** integration test uploads, fetches back identical bytes, deletes, and 404s after; oversize/wrong-type rejected.

### 8. Publish
- [x] Register all endpoints in `MapPropertyEndpoints`; register all handlers in `AddPropertyHandlers` (Wolverine needs them `public`).
- [x] Confirm OpenAPI reflects the phase through endpoint registration and full build/test verification.
- **Done when:** `dotnet build`, arch tests, unit + integration tests all green; OpenAPI published.

---

## Test ledger (Phase 1 DoD)
- **Unit (Domain):** status transitions incl. `CompletedAt`; reorder; attachment validation; `Money` estimates.
- **Integration (per slice):** implemented coverage currently proves project create/list/complete with suggested history payload, task reorder including mismatched-set rejection, attachment upload/download/delete/404-after-delete, and cross-household 403. Add narrower not-found, invalid file-type/oversize, `ProjectDeletedV1`, and durable Audit-entry assertions when Phase 1 is next expanded.
- **Arch (automatic):** domain purity, endpoints depend only on `IMessageBus`, slice co-location.

---

## How this template generalises
Phases 3 and 4 get the same treatment at their kickoff: lock micro-decisions → order domain-first → slices grouped by aggregate → cross-cutting → publish → test ledger. Generate each one **when you start the phase**, not now — Phase 1 will set the blob-serve and audit-publisher patterns those phases reuse.
