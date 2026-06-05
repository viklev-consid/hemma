using FluentValidation;

namespace Hemma.Modules.Economy.Features.Import.PreviewImport;

internal sealed class PreviewImportValidator : AbstractValidator<PreviewImportRequest>
{
    public PreviewImportValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Rows).NotEmpty().Must(x => x.Count <= 1000);
    }
}
