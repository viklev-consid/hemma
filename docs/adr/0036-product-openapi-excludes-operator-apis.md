# ADR-0036: Product OpenAPI Excludes Operator APIs

## Status

Accepted

## Context

Hemma hosts product APIs and operator tools in the same ASP.NET Core process. The frontend generates its TypeScript client from the published OpenAPI document, so that document is a product contract, not a complete inventory of every route mounted by the host.

TickerQ exposes the recurring job dashboard under `/admin/jobs`. Those routes are operational, admin-only, and not part of the browser product surface. Including them in the product OpenAPI document causes generated frontend clients to include scheduler APIs that the product should not call.

## Decision

`/openapi/v1.json` is the product OpenAPI document. It includes customer-facing API endpoints and excludes operator-only scheduler dashboard paths such as `/admin/jobs`.

Operator tools remain mounted in the API host and keep their own authorization policy. They are documented in operational docs, not in the product OpenAPI contract used by frontend code generation.

## Consequences

Positive:

- Generated frontend clients stay focused on supported product endpoints.
- Scheduler implementation details do not leak into product API tooling.
- Operator routes can change independently from the product contract.

Negative:

- Operators cannot rely on `/openapi/v1.json` as a full route inventory for host-level tools.
- If an operator API later needs machine-readable documentation, it should get a separate internal document rather than being added back to the product OpenAPI document.

## Related

- ADR-0024 (Scalar for OpenAPI Documentation)
- ADR-0032 (TickerQ for Recurring Scheduled Jobs)
