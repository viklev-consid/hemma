using Hemma.Modules.Economy.Features.ChangeRecurringBillOccurrence;
using Microsoft.AspNetCore.Routing;

namespace Hemma.Modules.Economy.Features.ResumeOccurrence;

internal static class ResumeOccurrenceEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        ChangeRecurringBillOccurrenceEndpoint.Map(
            app,
            $"{EconomyRoutes.Prefix}/recurring-bills/{{recurringBillId:guid}}/resume",
            "ResumeEconomyRecurringBillOccurrence",
            "Resume one paused recurring bill occurrence.",
            RecurringBillOccurrenceAction.Resume);
}
