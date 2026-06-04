# Examples

Worked patterns extracted from the real modules. Each file shows one pattern end-to-end with annotations pointing at the key decisions.

| File | Pattern | Source module |
|---|---|---|
| [`cross-module-subscriber.md`](cross-module-subscriber.md) | Integration event subscriber with idempotency and consent | Notifications / `OnUserRegisteredHandler` |
| [`scheduled-job.md`](scheduled-job.md) | TickerQ cron trigger dispatching a Wolverine command | Users / `SweepExpiredTokens` |
| [`security-sensitive-slice.md`](security-sensitive-slice.md) | Slice with enumeration resistance (always-200) | Users / `ForgotPassword` |

These are the real files, not hypotheticals. If a file path below drifts from the actual codebase, trust the source — the paths are relative to the repo root.
