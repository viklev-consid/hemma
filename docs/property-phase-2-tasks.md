# Property — Phase 2 Task Checklist

> Companion to `docs/property-implementation-plan.md` (the design spec). This is the **sequenced task breakdown** for Phase 2 (Economy coupling), generated at phase kickoff. It orders the work, names the files, and lists the tests, and locks the micro-decisions the master plan left implicit.

**Status:** Completed in `e8dcdd0` (Economy side) and `b4183d6` (Property side). All work items below are done; migrations applied, audit emitted on the assign mutation, and integration tests cover assign/clear, spend aggregation, project-deleted unlink, and budget combination.

> **Before starting:** Phase 1 must be merged. Phase 2 changes **both** the `Economy` and `Property` modules. Dependency directions: **Property (internal) references `Economy.Contracts`** to invoke the spend query; **Economy (internal) references `Property.Contracts`** to subscribe to `ProjectDeletedV1`. Neither module's `.Contracts` references the other's (enforced by `ModuleBoundaryTests`).

---

## Micro-decisions locked for this phase

1. **Only cross-boundary types go in `.Contracts`.** Per `src/Modules/CLAUDE.md` ("Queries/ — if other modules can query this one"), only `GetProjectSpendSummaryQuery` (+ its result DTOs) lives in `Economy.Contracts.Queries`, because **Property** invokes it via `IMessageBus`. `AssignTransactionToProject` and `ListTransactionsForProject` are **frontend-driven against Economy's own endpoints** — Property never invokes them — so they stay as internal `Features/` slices and reuse the existing `TransactionResponse` ("existing transaction row shape"). This keeps the published cross-module surface minimal and matches the existing `RecordTransaction`/`ListTransactions` slices. (The master plan's "contract types live in Economy.Contracts" is read as "the cross-boundary ones"; all three are still published in OpenAPI via their endpoints.)
2. **`Transaction.ProjectId` is a bare nullable `Guid`, no FK, no cross-module validation** — mirrors `PayerId`. `AssignToProject(Guid?)` sets/clears it on the aggregate. `null` clears.
3. **Index `(HouseholdId, ProjectId)`** on `economy.transactions` serves both the spend summary (filter by household + project set) and `ListTransactionsForProject`.
4. **`LinkedTotal` = sum of `Amount.Amount` over all transactions linked to the project** (household-scoped); `TransactionCount` = count of those rows; currency is SEK (v1, the only supported currency). No expense/income filtering in v1 — assigning a transaction to a project is the user's deliberate declaration that it is project spend.
5. **`GetProjectBudget.Remaining`** = `Estimate − LinkedTotal` when an estimate exists (same currency, SEK); `null` when there is no estimate. `LinkedTotal` defaults to `0 SEK` when no transactions are linked.
6. **`ProjectDeletedV1` subscriber** in Economy nulls `ProjectId` on all matching transactions via `ExecuteUpdateAsync` (efficient bulk clear; the link carries no audit value).

---

## Work items (in order)

### Economy — persistence
- [x] `Transaction.ProjectId` (`Guid?`) + `AssignToProject(Guid?)` method.
- [x] `TransactionConfiguration`: map `ProjectId`, add index `(HouseholdId, ProjectId)`.
- [x] Migration `Phase2ProjectLinking` (EconomyDbContext).

### Economy — slices
- [x] `Features/AssignTransactionToProject/` (Request/Command/Handler/Validator/Endpoint) — `POST /v1/economy/transactions/{transactionId}/project`, write-permission, publishes mutation audit `economy.transaction.project_assigned`.
- [x] `Features/ListTransactionsForProject/` (Query/Response/Handler/Endpoint) — `GET /v1/economy/projects/{projectId}/transactions`, read-permission, paged, reuses `TransactionResponse`.
- [x] `Economy.Contracts/Queries/GetProjectSpendSummary.cs` — `GetProjectSpendSummaryQuery`, `ProjectSpendSummary`, `GetProjectSpendSummaryResult`.
- [x] `Features/GetProjectSpendSummary/` handler (internal) returning `GetProjectSpendSummaryResult`.

### Economy — subscriber
- [x] `Economy.csproj` references `Property.Contracts`.
- [x] `Integration/Subscribers/OnProjectDeletedHandler.cs` — nulls `ProjectId` on matching transactions.
- [x] Register the three handlers in `AddEconomyHandlers`; map the two endpoints in `MapEconomyEndpoints`.

### Property — budget
- [x] `Property.csproj` references `Economy.Contracts`.
- [x] `GetProjectBudgetQuery` + `GetProjectBudgetResponse` (Projects slice).
- [x] `ProjectHandler.Handle(GetProjectBudgetQuery)` — load project (household-scoped) for the estimate, invoke `GetProjectSpendSummaryQuery` cross-module, combine.
- [x] Endpoint `GET /v1/property/projects/{projectId}/budget`, read-permission.

### Tests
- **Economy (integration):** assign+clear round-trip; spend summary aggregates linked totals/counts; list-for-project paging; `ProjectDeletedV1` nulls links.
- **Property (integration):** `GetProjectBudget` combines stored estimate with Economy spend (requires the Economy schema migrated in `PropertyApiFixture`); remaining computed; cross-household 403.

---

## Definition of done
Migrations applied · handlers + queries covered by tests · Audit emitted on the assign mutation · OpenAPI published (assign command + spend queries + project budget) · all tests green.
