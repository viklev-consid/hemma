using Hemma.Modules.Economy.Features.ChangeRecurringBillOccurrence;
using Microsoft.AspNetCore.Routing;

namespace Hemma.Modules.Economy.Features.PauseOccurrence;

internal static class PauseOccurrenceEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        ChangeRecurringBillOccurrenceEndpoint.Map(
            app,
            $"{EconomyRoutes.Prefix}/recurring-bills/{{recurringBillId:guid}}/pause",
            "PauseEconomyRecurringBillOccurrence",
            "Pause one recurring bill occurrence.",
            RecurringBillOccurrenceAction.Pause);
}
