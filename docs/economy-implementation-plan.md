# Household + Economy — BACKEND Implementation Plan (agent-targeted)

> **Repo:** backend (.NET modular monolith). **Audience:** the backend implementation agent.
> **Counterpart:** a separate frontend repo + agent consumes this module's HTTP API via a generated OpenAPI client. **You own the API contract.** Whenever you add or change an endpoint, request, or response shape, you must keep the OpenAPI document accurate — that document is the only thing the frontend agent sees.
> **How to use:** work phases top-to-bottom. Each phase is independently shippable. Do not start a phase until prerequisites are green. Treat **Invariants** as non-negotiable — violating one is a defect even if tests pass.

---

## 0. Context the agent MUST respect

### Stack
- .NET 10, C#, modular monolith. Wolverine (mediator + messaging + outbox), EF Core (Postgres), FluentValidation, TickerQ (scheduling), HybridCache.
- DB: Postgres, **one schema per module**. Economy uses schema `economy`; Households uses `households`.

### Vertical slice anatomy (per feature)
Each feature is a self-contained folder under `Modules/Economy/Features/<FeatureName>/`:
```
<FeatureName>/
├─ <FeatureName>Request.cs       // inbound DTO (public shape, appears in OpenAPI)
├─ <FeatureName>Response.cs      // outbound DTO (public shape, appears in OpenAPI)
├─ <FeatureName>Command.cs       // or Query — Wolverine message
├─ <FeatureName>Handler.cs       // logic; depends on EconomyDbContext + domain
├─ <FeatureName>Validator.cs     // FluentValidation on the Request/Command
└─ <FeatureName>Endpoint.cs      // maps HTTP → command; returns Response
```
Queries are read-only and must not mutate. Commands mutate through domain aggregates, never via raw SQL.

### Module boundary guardrails (enforced by NetArchTest)
- **No cross-schema joins.** Economy never reads another module's tables.
- **Cross-module communication is via Wolverine integration events only**, through the outbox.
- **Public surface is `Economy.Contracts`** (events + read DTOs other modules may consume). Everything else is internal. Other modules reference only `Economy.Contracts`.

### Naming
- Root namespace: `Hemma.Modules.Economy`.
- **Money is always the `Money` value object** (amount + currency). Never raw `decimal` for money in domain or cross-module contracts. v1 is SEK-only; DTOs keep currency explicit for contract stability, and non-SEK write paths must be rejected.
- HTTP DTOs represent money with the stable JSON shape `{ "amount": 119.00, "currency": "SEK" }`.
- Code identifiers are English; Swedish domain terms (Mat, Boende, Sparande) live only in seed data / user-facing strings.

### Settled invariants (do not violate)
1. **Transaction is the sole source of truth for money moved.** A fixed `RecurringBill` may auto-post a transaction; an estimated one posts a *pending* transaction to confirm. A **`Subscription` never posts a transaction** — it only observes and predicts.
2. **Transfers:** *Neutral* → nets to zero everywhere. *Savings-tagged* → excluded from net worth & income-vs-expense (not consumption) but **counted as allocation** in budget-vs-actual & breakdown. A transfer is always two reconciling legs; never double-counted as both expense and income.
3. **Subscription ↔ transaction link is one-directional evidence:** `Transaction.SubscriptionId?` (nullable FK inside `economy`). Importer auto-matches; user links/unlinks manually. Price history is derived *from* linked transactions.
4. **Analytics adds no aggregates.** Read-only query slices over existing tables; exclude `Kind = Transfer` except savings-allocation views.
5. **Receipts** use the existing `IBlobStore` through a backend-mediated multipart upload endpoint. Never store blobs in Postgres. Direct-to-blob upload reservation is deferred until the blob abstraction supports upload targets.

### Definition of Done (every task)
- Slice compiles; domain/value objects have focused unit tests; every slice has ≥1 integration test through the HTTP/API or Wolverine path using real Postgres via the existing harness / Testcontainers.
- FluentValidation covers the listed failure modes.
- EF migration generated, named, applies cleanly on a fresh DB.
- NetArchTest suite green.
- **If an HTTP shape changed: OpenAPI document regenerated/committed** (this is the frontend's view). **If a cross-module message/query/event changed: `Economy.Contracts` updated** with versioned contracts.
- No raw `decimal` money; no cross-schema access; no `Subscription` posting a transaction.

### API contract responsibility
- Endpoints are REST under stable versioned bases: `/v1/economy/...` and `/v1/households/...`.
- Keep request/response DTO names stable and descriptive — the frontend client is generated from them.
- Keep HTTP DTOs module-internal unless another backend module consumes them. Use `.Contracts` only for cross-module commands, queries, events, and shared DTOs used by those contracts.
- After every phase, ensure the committed OpenAPI spec reflects reality. The **"API surface published this phase"** list at the end of each phase is the hand-off to the frontend agent.

---

## Phase 0 — Organizations → Households (foundation rename)

**Goal:** rename the Organizations module to Households across domain, schema, contracts. Collapse household roles to `owner | member`. Blocks everything.

**Prerequisites:** none.

### Tasks
1. Rename domain + namespaces: `Organization` → `Household`, `OrganizationMember` → `HouseholdMember`; update namespaces and folders.
2. Role remap: collapse to `owner | member`. Drop `admin` and `viewer` completely; migrate both to `member`. Remove role-rank and escalation restrictions that only existed to distinguish `admin`/`viewer`, while preserving the owner protections that keep every household with at least one owner.
3. Schema migration: rename `organizations.*` → `households.*`, reversible, preserving rows + FKs.
4. Contracts: rename public events (`OrganizationCreatedV1` → `HouseholdCreatedV1`, `OrganizationMemberRemovedV1` → `HouseholdMemberRemovedV1`, …); update every subscriber. Integration events must keep version suffixes.
5. Invitations: copy/tokens to "household"; no behavioral change.
6. NetArchTest: update boundary rules to `Households`.

### Phase 0 implementation checklist
- Rename projects, folders, namespaces, route constants, options sections, telemetry names, permission constants, tests, and scoped `AGENTS.md`/`CLAUDE.md` references from Organizations to Households.
- Rename database schema and EF migration history schema from `organizations` to `households` with a reversible migration that preserves rows and indexes.
- Migrate persisted household role values: `owner` stays `owner`; `admin`, `viewer`, and `member` become `member`.
- Remove household role-rank/escalation logic that only distinguishes `admin`/`viewer`; keep last-owner and owner-only deletion/demotion protections.
- Rename public contracts and update every subscriber in Users, Audit, and Notifications. Keep version suffixes (`V1`) rather than introducing unversioned events.
- Keep the global/platform user role `admin` separate from household membership roles. Do not remove or rename the Users-owned platform/admin role.
- Update OpenAPI-facing route names and summaries from `/v1/organizations/...` to `/v1/households/...`.
- Run architecture tests, affected unit tests, and affected integration tests before moving to Phase 1.

**Events published:** `HouseholdCreatedV1`, `HouseholdMemberInvitedV1`, `HouseholdMemberJoinedV1`, `HouseholdMemberRemovedV1`.

**Acceptance:**
- Existing org data fully accessible under the household model after migration (round-trip test on seeded DB).
- Old `admin` and `viewer` rows are migrated to `member`; only `owner` keeps owner-only behavior.
- No `Organization*` references remain except migration history.

**API surface published this phase:** household CRUD + membership/invitation endpoints renamed under `/v1/households/...`. Update OpenAPI.

---

## Phase 1 — Economy core (Settings · Accounts · Categories · Budget)

**Goal:** stand up `economy` schema and skeleton aggregates. No money moves yet.

**Prerequisites:** Phase 0 green.

### Domain
- `Money` VO (`Amount` decimal + `Currency`; same-currency arithmetic guards; rejects invalid negatives).
- `EconomySettings` (per household): `CycleStartDay` (1–28), default currency.
- `Account`: `Id`, `Name`, `Type ∈ {Spending, Savings}`, `OpeningBalance : Money`.
- `Category`: hierarchical, **max 2 levels**; `Budgetable : bool`; non-budgetable categories are allowed for classification/reporting without budget lines.
- `Budget` + `BudgetLine`: per category, per period; period derived from `CycleStartDay`. If settings are created mid-cycle, keep the real cycle period but calculate first-period pace/progress from the settings creation date.

### Slices
| Slice | Type | Notes |
|---|---|---|
| `CreateEconomySettings` | Command | one per household; seeds starter categories (Mat, Boende, Transport, …) |
| `UpdateCycleStartDay` | Command | validate 1–28 |
| `CreateAccount` | Command | `Type` required |
| `ListAccounts` | Query | balances (opening only until tx exist) |
| `AddCategory` | Command | reject 3rd level |
| `ListCategories` | Query | tree |
| `CreateBudget` | Command | per period |
| `UpsertBudgetLine` | Command | per category amount |
| `CopyBudgetFromPreviousPeriod` | Command | default for new period |

**Acceptance:**
- Category at depth 3 rejected.
- `CycleStartDay` outside 1–28 rejected.
- Mid-cycle settings creation keeps the normal cycle window but prorates pace/progress from the settings creation date.
- Copy produces lines matching prior period; no prior period → empty budget, no error.
- Cross-currency arithmetic throws; same-currency sums correct.

**API surface published this phase:** `/v1/economy/settings`, `/v1/economy/accounts` (create/list), `/v1/economy/categories` (add/list), `/v1/economy/budgets` (create/upsert-line/copy). Update OpenAPI.

---

## Phase 2 — Transactions · Receipts · Transfers

**Goal:** daily-driver writes/reads — manual entry, receipts, neutral + savings transfers, budget-vs-actual with pace.

**Prerequisites:** Phase 1 green.

### Domain
- `Transaction`: `Id`, `AccountId`, `CategoryId?`, `Amount : Money`, `OccurredOn`, `Note?`, `Kind ∈ {Expense, Income, Transfer}`, `ReceiptBlobId?`, `SubscriptionId?` (populated Phase 5), `PayerId?`.
- `Transfer`: links two `Transaction` legs (out + in); `Mode ∈ {Neutral, Savings}`; if `Savings`, outflow leg carries a category (default Sparande).

### Slices
| Slice | Type | Notes |
|---|---|---|
| `RecordTransaction` | Command | Expense/Income; validates account + optional category |
| `AttachReceipt` | Command | accepts `multipart/form-data`, stores the uploaded file via `IBlobStore`, links `ReceiptBlobId`; delete the blob if DB save fails |
| `ListTransactions` | Query | filters: category, date range, payer, has-receipt, amount range; paged |
| `SearchTransactionNote` | Query | free-text (`ILIKE`/trigram) |
| `CreateTransfer` | Command | `Mode`; both legs atomic |
| `GetAccountBalances` | Query | opening + posted |
| `GetBudgetSummary` | Query | planned vs actual + pace indicator |

### Invariant enforcement (write tests)
- Transfer always two reconciling legs; can't delete one alone.
- Neutral legs excluded from `GetBudgetSummary` and consumption analytics.
- Savings outflow leg included in `GetBudgetSummary` (under its category), excluded from income-vs-expense / net worth.

**Events published:** `ExpenseRecordedV1`, `BudgetExceededV1` (→ Notifications when actual > line).

**Acceptance:**
- Neutral transfer changes both balances, leaves income-vs-expense unchanged.
- 5 000 savings transfer appears as 5 000 under Sparande in budget-vs-actual; net worth unchanged.
- Pace math: 60% elapsed + 80% spent → flagged over-pace.
- `has-receipt=true` returns only transactions with `ReceiptBlobId`.

**Decision:** savings transfer defaults come from both account `Type` and transfer tag. Account type pre-selects the likely behavior, and the transfer carries the final category; backend must accept an explicit category on the transfer to allow override.

**Decision:** receipt uploads are backend-mediated for now. The frontend sends the file to `AttachReceipt` as `multipart/form-data`; the backend validates metadata/size, stores the stream with `IBlobStore`, and persists the blob reference on the transaction. A future direct-upload flow can add a reservation endpoint when `IBlobStore` supports upload targets.

**API surface published this phase:** `/v1/economy/transactions` (record/list/search), `/v1/economy/transactions/{id}/receipt` (multipart upload attach), `/v1/economy/transfers` (create), `/v1/economy/accounts/balances`, `/v1/economy/budget-summary`. Update OpenAPI. **Flag to frontend agent:** transfer create accepts `mode` + optional `categoryId`; receipt attach is multipart file upload, not direct-to-blob reservation.

---

## Phase 3 — Recurring bills

**Goal:** fixed (auto-record) + estimated (pending occurrence → settle) recurring charges; recurring income. Scheduling via TickerQ.

**Prerequisites:** Phase 2 green.

### Domain
- `RecurringBill`: `Amount`/`EstimatedAmount : Money`, `Cadence` ({frequency, interval, dayOfMonth}), `Type ∈ {Fixed, Estimated}`, `Direction ∈ {Expense, Income}`, `AccountId`, `CategoryId?`; per-occurrence skip/pause state.

### Slices
| Slice | Type | Notes |
|---|---|---|
| `CreateRecurringBill` | Command | Fixed/Estimated; Expense/Income |
| `ConfirmEstimatedBill` | Command | real amount → posts actual transaction; settles pending occurrence |
| `SkipOccurrence` | Command | single instance |
| `PauseOccurrence` / `Resume` | Command | single instance |
| `ListRecurringBills` | Query | next due + pending-confirm inbox; must not eager-load historical occurrences |
| `RunDueBills` | Scheduled (TickerQ) | Fixed → auto-post; Estimated → pending + notify |

### Invariant enforcement
- Fixed auto-posts a real transaction on due date.
- Estimated creates a pending occurrence without a transaction; manual confirm creates the real transaction at the entered amount and links it to the occurrence.
- Income recurrence posts `Kind = Income`.

**Events published:** `EstimatedBillPendingV1` (→ Notifications), carrying the pending occurrence id.

**Acceptance:**
- Fixed monthly 119 → exactly one transaction per cycle on the configured day.
- Estimated shows pending, doesn't affect actuals until settled.
- Skipping one occurrence doesn't affect later ones.
- Due-bill processing is per bill and idempotent on `(RecurringBillId, DueOn)` overlap; a bad bill does not abort the batch.

**API surface published this phase:** `/v1/economy/recurring-bills` (create/list/confirm/skip/pause/resume). Update OpenAPI.

---

## Phase 4 — Transaction import + categorization rules

**Goal:** two-phase transaction import with duplicate detection and learn-on-commit rules. The frontend owns CSV parsing and column mapping; the backend owns the canonical normalized import contract, validation, preview, duplicate detection, categorization, and commit. Sub-area `Features/Import/`.

**Prerequisites:** Phase 2 green.

### Domain
- `CategorizationRule`: `Match ∈ {Contains, Regex}`, `Pattern`, `TargetCategoryId`, `Enabled`, household-wide scope.
- Import working state is transient until commit; committed rows become `Transaction`s.
- Enabled categorization rules are capped at 100 per household. Regex matching uses a timeout and timeout failures must not abort an import.

### Contract
- Backend accepts normalized import rows, not bank-specific CSV files.
- Required mapped fields: `OccurredOn`, `Amount`, `Description`.
- Optional mapped fields: `Currency`, `Counterparty`, `Reference`, `BalanceAfter`, `RawDescription`, `CategoryId`, `RecurringBillOccurrenceId`.
- Frontend is responsible for CSV parsing, date/decimal format handling, and mapping source columns to these fields before calling the backend.
- Backend returns row-level validation errors and recurring bill match suggestions so the frontend can let users fix mapped rows and choose whether an imported row settles a pending bill occurrence before commit.
- `PreviewImport` is stateless: it returns enriched rows plus a deterministic `previewFingerprint` derived from the normalized accepted rows and target account.
- `CommitImport` resubmits the accepted normalized rows plus the `previewFingerprint`; the backend recomputes and rejects if the payload changed unexpectedly. Do not persist server-side preview sessions unless a later requirement forces it.

### Slices
| Slice | Type | Notes |
|---|---|---|
| `PreviewImport` | Query | normalized rows + running balance + duplicate flags + row validation + auto-applied categories + pending recurring-bill suggestions + `previewFingerprint` |
| `CommitImport` | Command | accepted normalized rows + `previewFingerprint`; create transactions; apply rules; settle selected recurring-bill occurrences; suggest new rules |
| `ManageCategorizationRules` | Command(s) | CRUD + enable/disable |
| `ListCategorizationRules` | Query | |

### Duplicate detection
- Hash over stable tuple (`date + amount + normalized-description + account`). Choose fields so two legitimately identical same-day charges aren't falsely merged — surface "possible duplicate" for confirm when confidence is low rather than auto-dropping.

**Acceptance:**
- Backend rejects unmapped/invalid normalized rows with row-level errors.
- Commit rejects when the submitted rows do not match the preview fingerprint.
- Re-importing the same file flags every row as duplicate; none double-committed.
- `Contains "ICA"` rule auto-assigns Mat on import.
- Linking an imported row to a pending recurring-bill occurrence settles the occurrence without creating a duplicate budget actual.
- If a selected recurring-bill occurrence is already settled by commit time, the row still imports and the stale link is reported as skipped.
- Hand-categorizing during preview offers a persistable rule on commit.
- Rule count and normalized row field lengths are bounded so a 1000-row import cannot trigger unbounded CPU or memory work.
- Import scoped to exactly one account.

**Decisions:** CSV parsing and field mapping are frontend responsibilities; categorization rules are household-wide; each import is scoped to exactly one account.

**API surface published this phase:** `/v1/economy/import/preview`, `/v1/economy/import/commit`, `/v1/economy/categorization-rules` (CRUD/list). Update OpenAPI. **Flag to frontend agent:** import is a multi-step flow over normalized mapped rows — preview returns rows with `duplicateState`, `suggestedCategoryId`, `suggestedRecurringBillMatches`, row-level validation errors, and `previewFingerprint`; commit resubmits accepted rows plus that fingerprint and may include `recurringBillOccurrenceId` selections.

---

## Phase 5 — Subscriptions (observe-only) + calendars + tx linking

**Goal:** subscription lifecycle as a separate, **observe-only** aggregate; year + month calendar queries; evidence-based linking to transactions.

**Prerequisites:** Phase 2 green; richer after Phase 4.

### Domain
- `Subscription`: `Name`, `Cadence` ({frequency, interval, chargeDay}), `ExpectedAmount : Money`, `LifecycleState ∈ {Trial, Active, Paused, Cancelled}`, `TrialEndsOn?`, `AccountId?`.
- Price history **derived from linked transactions**, not stored as truth.
- **No transaction posting anywhere in this aggregate.**

### Slices
| Slice | Type | Notes |
|---|---|---|
| `CreateSubscription` | Command | |
| `ChangeLifecycleState` | Command | Trial/Active/Paused/Cancelled |
| `GetChargeHistory` | Query | linked transactions + derived price changes |
| `MatchTransactionOnImport` | hook in `CommitImport` | auto-link by pattern + amount + day window; sets `SubscriptionId` |
| `LinkTransaction` / `UnlinkTransaction` | Command | manual override |
| `GetPaymentSchedule` | Query | **year view**: which months each subscription charges |
| `GetMonthChargeCalendar` | Query | **month view**: pick month → day-by-day charges, predicted vs actual |
| `TrialRenewalReminder` | Scheduled (TickerQ) | warn before trial converts |
| `DetectFromImport` | hook | infer candidate subscriptions from recurring patterns |

### Invariant enforcement (write tests)
- `Subscription` has **no code path that creates a `Transaction`** — assert at aggregate/handler level.
- Linking sets `Transaction.SubscriptionId` only; no amount duplicated anywhere.
- Price history computed from linked transaction amounts over time.
- Month calendar marks a day *actual* when a matched transaction exists, else *predicted*.

**Events published:** `TrialRenewalDueV1` (→ Notifications).

**Idempotency:** `TrialRenewalReminder` records the trial end date it has reminded for and publishes at most once per subscription trial end window.

**Acceptance:**
- Creating/charging a subscription posts **zero** transactions on its own.
- Linked history 99 → 119 surfaces a derived price change.
- Auto-match links an imported -119 "Spotify" row; a borderline row surfaces as suggested match.
- Selecting March returns only that month's charges on actual days.

**Decision:** auto-matching must suggest, not silently link. The matcher may return suggested matches based on amount tolerance, description similarity, and day-of-month window, but only explicit user confirmation or an already-linked transaction produces `matchState = actual`. `GetMonthChargeCalendar`/`GetChargeHistory` should return a `matchState` (`actual` | `predicted` | `suggested`) per row.

**API surface published this phase:** `/v1/economy/subscriptions` (CRUD/state), `/v1/economy/subscriptions/{id}/charge-history`, `/v1/economy/subscriptions/payment-schedule` (year), `/v1/economy/subscriptions/month-calendar?month=`, `/v1/economy/subscriptions/{id}/link`/`unlink`. Update OpenAPI.

---

## Phase 6 — Analytics (fully scoped, all six)

**Goal:** six read-only query slices, no new aggregates.

**Prerequisites:** Phase 2; fuller with Phases 3–5.

### Slices (all Query)
| Slice | Notes |
|---|---|
| `GetCategoryTrend` | spend per category over periods |
| `GetSpendBreakdown` | share per category; **includes savings-tagged transfer legs** as an allocation slice |
| `GetPeriodComparison` | this vs previous period |
| `GetIncomeVsExpense` | per period; **excludes all transfers** |
| `GetVarianceHistory` | budget vs actual variance over time |
| `GetTopTransactions` | largest in a period/category |

### Invariant enforcement
- Consumption/spend queries exclude `Kind = Transfer`, except breakdown/budget which include savings outflow legs as allocation.
- `GetIncomeVsExpense` never counts a transfer leg as income/expense.
- Every query returns a valid empty result on an empty dataset (no error).

**Performance (risk):** index on (`householdId`, `OccurredOn`, `CategoryId`, `Kind`); validate HybridCache sufficiency at realistic volume; benchmark before done.

**API surface published this phase:** `/v1/economy/analytics/category-trend`, `/v1/economy/analytics/spend-breakdown`, `/v1/economy/analytics/period-comparison`, `/v1/economy/analytics/income-vs-expense`, `/v1/economy/analytics/variance-history`, `/v1/economy/analytics/top-transactions`. Update OpenAPI. **Flag to frontend agent:** each returns series data shaped for Recharts (label + value arrays); document the exact JSON shape.

---

## Phase 7 — Cross-cutting & privacy (backend portion)

**Goal:** Audit, GDPR, Notifications prefs. Application-level field encryption for sensitive Economy free-text/name fields is a pre-production requirement; per-household key wrap is deferred until crypto-erasure is explicitly required.

### Tasks
1. **GDPR:** export + erase for Economy data; subscribe to `GdprErasureRequestedV1`, `HouseholdMemberRemovedV1`. Erasure cascades to receipt blobs.
2. **Audit:** subscribe write slices to the Audit module.
3. **Notifications:** per-household preferences for budget/bill/trial alerts.
4. **Encryption (Tier 2):** protect sensitive Economy free-text/name fields from raw database disclosure before production. Per-household key wrap is deferred; use application-level field encryption unless an ADR accepts plaintext with compensating controls.

**DataProtection note:** ADR-0021 warns not to use DataProtection for arbitrary configuration secrets. Economy field encryption may use DataProtection for deliberate field protection, consistent with the existing Users module's TOTP-secret protection pattern. Do not include `HouseholdId` in the DataProtection purpose string unless a per-household key-wrap/envelope design is documented.

**Acceptance:**
- GDPR HTTP export is self-scoped to the authenticated data subject. Erasure clears only records attributable to that user; household-member removal must not clear other members' notes, import fingerprints, or receipt blobs.
- Sensitive Economy free-text/name fields are unreadable in a raw database dump before production, or an explicit ADR records why plaintext is accepted.
- Audit entries for every mutating Economy slice.

**API surface published this phase:** `/v1/economy/gdpr/export`, notification-preference endpoints (verify if owned here or by Notifications module). Update OpenAPI.

---

## Cross-cutting test strategy
- **Unit:** Money arithmetic, 2-level category cap, transfer two-leg reconciliation, subscription-never-posts.
- **Integration:** every handler against real Postgres incl. migrations.
- **Architecture:** NetArchTest — no cross-schema access, no inter-module refs outside `*.Contracts`, slices independent.
- **Contract:** events round-trip; OpenAPI matches endpoints.
- **Analytics correctness:** golden-dataset tests — transfers excluded from income-vs-expense; savings present in breakdown.

## Unknowns / risks (investigate, don't guess)
- Categorization suggestion quality — only knowable on real descriptions; ship rule-based suggestions first and measure/manual-review before adding smarter heuristics.
- Analytics performance at volume — benchmark in Phase 6 with a generated household dataset before enabling broad caching.
- Duplicate-hash false merges — mitigate by never silently dropping low-confidence duplicates; return `duplicateState = none | possible | exact` and require confirmation for possible duplicates.
- Subscription detection accuracy — mitigated by the settled decision to suggest, not silently link; validate suggestions post-import with real transaction descriptions.

## Assumptions to verify before starting
- Root namespace `Hemma.Modules.Economy`.
- API base paths `/v1/economy/...` and `/v1/households/...`.
- Existing Postgres integration-test harness — reuse it.
- `IBlobStore`, TickerQ, HybridCache, Notifications/Audit/GDPR modules available and unchanged.
- SEK-only v1. `Money` carries currency for API stability, but non-SEK writes are rejected until analytics and account/transaction invariants are redesigned for multi-currency.

## Hand-off to the frontend agent
After each phase, the **"API surface published this phase"** list + the regenerated OpenAPI document is the contract. The frontend agent never reads this repo's code — only the OpenAPI spec and the behavioral flags noted (transfer `mode`+`categoryId`, import multi-step `isDuplicate`/`suggestedCategoryId`, subscription `matchState`, analytics series shapes).
