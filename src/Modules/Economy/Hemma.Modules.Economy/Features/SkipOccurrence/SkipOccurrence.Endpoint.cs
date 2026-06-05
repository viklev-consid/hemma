using Hemma.Modules.Economy.Features.ChangeRecurringBillOccurrence;
using Microsoft.AspNetCore.Routing;

namespace Hemma.Modules.Economy.Features.SkipOccurrence;

internal static class SkipOccurrenceEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        ChangeRecurringBillOccurrenceEndpoint.Map(
            app,
            $"{EconomyRoutes.Prefix}/recurring-bills/{{recurringBillId:guid}}/skip",
            "SkipEconomyRecurringBillOccurrence",
            "Skip one recurring bill occurrence.",
            RecurringBillOccurrenceAction.Skip);
}
