# CLAUDE.md - Households Module

This module owns the application-level workspace/account primitive: households, slugs, memberships, invitations, and household-scoped authorization. It is not full infrastructure-level multi-tenancy; modules opt into household ownership by storing an `HouseholdId` and authorizing against an household scope.

For general module conventions, see [`../../CLAUDE.md`](../../CLAUDE.md). For the architectural decision, see [`../../../../docs/adr/0035-households-and-scoped-authorization.md`](../../../../docs/adr/0035-households-and-scoped-authorization.md).

---

## Domain vocabulary

- **Household** - the root aggregate. Identified by `HouseholdId` (typed Guid).
- **HouseholdSlug** - globally unique URL-friendly reference. Routes accept either slug or household ID as `householdRef`.
- **HouseholdMembership** - a user's active or historical role in an household.
- **HouseholdRole** - `owner` or `member`; owners manage the household, members can read household/member information.
- **HouseholdInvitation** - email-scoped, single-use invitation into one household with one requested role. Raw token exists only in transit; the stored token is a SHA-256 hash.
- **HouseholdScope** - public scoped-authorization contract used by modules that need household-aware permissions.

---

## Shipped flows

- Create, read, update, soft-delete households.
- List the current user's households with role, scoped permissions, and permission version.
- List members, change member roles, remove members, and allow self-leave.
- Create, list, revoke, validate, and accept household invitations.
- Accept household invitations during Users-owned registration.
- Household-scoped audit lookup.
- GDPR erasure checks and membership anonymization for deleted users.

---

## Invariants

1. Households are application-level collaboration boundaries, not separate databases, schemas, caches, queues, or deployments.
2. Active households must have at least one owner. The last owner cannot leave, be removed, or be demoted.
3. Full household deletion is allowed for owners and is soft-delete in v1.
4. Platform/global admins are never hidden household members. Platform override is explicit per authorization call.
5. A user with both real membership access and platform override is reported as `ScopedPermission`; `PlatformOverride` means membership was bypassed.
6. Role changes and invitations cannot escalate above the actor's active household role rank.
7. Household invitation tokens are stored hashed, are single-use, expire, and require an email match.
8. Pending invitations are unique by household and normalized email.
9. Commands, queries, persisted references, and integration events use durable `HouseholdId`, not slugs.
10. Retained membership history for erased users clears `UserId`; anonymized rows are no longer queryable as that user.

---

## Access control

Household permissions live in `Hemma.Modules.Households.Contracts/Authorization/HouseholdsPermissions.cs`:

```text
households.households.read
households.households.write
households.households.delete
households.members.read
households.members.manage
households.invitations.manage
households.audit.read
households.platform.override
```

Use `IScopedAuthorizationService<HouseholdScope>` at the endpoint boundary after resolving `householdRef`. Endpoints may pass `ScopedAuthorizationOptions.WithPlatformOverride` only when global admin bypass is intended.

Do not add household-scoped permissions to global JWT claims or `/v1/users/me.permissions`. Clients hydrate scoped permissions through `/v1/households/my`.

---

## Route and boundary rules

Household-scoped routes use:

```text
/v1/households/{householdRef}/...
```

Resolve `householdRef` with `IHouseholdRefResolver` in the endpoint. Handlers should receive `HouseholdId`.

Other modules may reference only `Hemma.Modules.Households.Contracts`. They must not reference this internal project, query Households tables, join across schemas, or add cross-schema foreign keys.

---

## Integration events and cross-module contracts

Public contracts live under `Hemma.Modules.Households.Contracts`.

Important cross-module interactions:

- Users invokes household invitation validation and acceptance during registration.
- Users invokes the erasure guard before account deletion.
- Notifications consumes household invitation events to send invite links.
- Audit consumes household events and stores household-scoped audit entries.

Raw invitation tokens may appear in `HouseholdInvitationCreatedV1` so Notifications can build the email. They are marked sensitive, must never be logged, and must not be persisted in Audit payloads.

---

## GDPR behavior

Household business records may outlive a user account, but personal identity must not.

- Sole owners block account deletion until ownership is transferred or the household is deleted.
- Non-owner account deletion removes active membership and anonymizes retained membership history.
- Membership anonymization clears `UserId` and `RemovedByUserId`.
- Invitation user references (`InvitedByUserId`, `AcceptedUserId`, `RevokedByUserId`) are cleared when they point to the erased user.
- Audit payloads must not include email addresses, display names, raw tokens, or household names/slugs.

---

## Configuration

Options are bound from `Modules:Households` through `HouseholdsOptions`.

- `InvitationLifetimeDays` controls household invitation expiry and defaults to 14 days.

Use `IOptions<HouseholdsOptions>`. Do not inject raw `IConfiguration` outside module registration.

---

## Known footguns

- Do not compare or authorize by slug after endpoint resolution. Slugs are user-facing references; `HouseholdId` is the durable identity.
- Do not model platform override as membership. It must not affect member lists, owner counts, notifications, or membership history.
- Do not grant owner-level actions through generic write/manage permissions. Deletion uses `households.households.delete`, and role rank checks protect role changes and invitations.
- Do not discard `ErrorOr` results from cross-module invitation acceptance. Registration must fail or compensate if membership creation fails.
- Do not put raw invitation tokens or invited emails in audit payloads.
- Do not reintroduce non-null `HouseholdMembership.UserId`; GDPR erasure depends on clearing it for retained history.
- Wolverine handlers and subscribers must be public and registered in `HouseholdsModule.AddHouseholdsHandlers`.
- Integration tests that track household events may need migrated Audit, Notifications, Catalog, and Users schemas because subscribers in those modules react to the same events.
