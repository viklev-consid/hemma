using Hemma.Modules.Users.Domain;

namespace Hemma.Modules.Users.Persistence;

internal static class TwoFactorCredentialQueries
{
    public static IQueryable<TwoFactorCredential> WhereActive(this IQueryable<TwoFactorCredential> source) =>
        source.Where(c => c.ConfirmedAt != null && c.DisabledAt == null);
}
