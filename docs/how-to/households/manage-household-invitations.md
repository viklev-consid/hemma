# Manage Household Invitations

Household invitations invite an email address into one household with one requested household role.

They are separate from global account invitations. The global user role created through an household invite is still `user` unless a platform admin changes it.

Invitation lifetime is configured through `Modules:Households:InvitationLifetimeDays`.

## Flow

1. An household owner/admin creates an invitation for an email and role.
2. The raw token is returned to the caller and sent through Notifications.
3. If the email belongs to an existing user, accepting the token creates membership.
4. If the email does not belong to a user, the client sends the invite token through the Users-owned registration flow.
5. Users creates the account, then the household invite is consumed and membership is created.

Households owns membership. Users owns account creation.

The default frontend link for household invitations is:

```text
/invite?token={rawToken}&email={email}
```

Signed-in clients accept with `POST /v1/households/invitations/accept`. Signed-out clients should route through registration using `householdInvitationToken`. If registration cannot consume the household invitation, the request fails and the newly created user record is compensated instead of silently returning a user without membership.

## Invariants

- Tokens are stored as hashes.
- Raw tokens are marked sensitive, omitted from audit payloads, and should only be used for the invite link or the one-time HTTP response.
- Pending invites are unique by household and email.
- Accepted, revoked, expired, or deleted-household invites cannot be accepted.
- Invite acceptance requires the accepting/registered email to match the invitation email.
- The requested role must be a known household role.
- The requested role cannot outrank the inviter's active household role.
- A deleted household cannot issue or accept invites.

## Authorization

Creating, listing, and revoking invitations requires:

```text
households.invitations.manage
```

Platform override may be allowed for operator endpoints, but must be explicit.
