namespace Hemma.Modules.Economy.Features.Import.Contracts;

public sealed record ImportRuleSuggestionResponse(string Pattern, string Match, Guid TargetCategoryId);
