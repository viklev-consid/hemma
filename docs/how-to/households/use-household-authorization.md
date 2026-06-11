# Use Household Authorization

Household authorization checks a permission against a specific household.

This is different from global RBAC:

```text
Global: user has users.users.read
Scoped: user has households.members.manage in Household A
```

## Concepts

Generic scoped authorization abstractions live in Shared. The Households module provides the implementation for:

```csharp
HouseholdScope
```

The authorization input is:

```text
current user + household ID + permission + options
```

The result includes whether access succeeded and how it was granted:

```text
ScopedPermission
PlatformOverride
None
```

Audit code should preserve that access mode for household-related actions.

When a user has both membership permission and global platform override, the evaluator reports `ScopedPermission`. `PlatformOverride` means the caller was allowed without active household membership.

## Platform override

Global admins may bypass household membership only when the endpoint explicitly opts in.

Use the explicit option:

```csharp
ScopedAuthorizationOptions.WithPlatformOverride
```

Do not model platform admins as hidden household members. They must not appear in member lists, owner counts, membership history, or household notifications unless they are real members.

## Permission ownership

Permissions belong to the module that owns the capability.

For Households-owned capabilities, use:

```text
households.households.read
households.households.write
households.members.read
households.members.manage
households.invitations.manage
households.audit.read
households.platform.override
```

For another module's household-owned resources, declare permission constants in that module's `.Contracts/Authorization` folder, then evaluate them against `HouseholdScope`.

## Frontend contract

Do not add household-scoped permissions to the flat `/v1/users/me.permissions` list.

Use the Households-owned current-user endpoint to hydrate household memberships, roles, scoped permissions, and scoped permission versions.
