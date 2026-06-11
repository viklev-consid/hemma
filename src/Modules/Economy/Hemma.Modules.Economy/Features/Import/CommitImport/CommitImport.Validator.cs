using FluentValidation;

namespace Hemma.Modules.Economy.Features.Import.CommitImport;

internal sealed class CommitImportValidator : AbstractValidator<CommitImportRequest>
{
    public CommitImportValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.PreviewFingerprint).NotEmpty().Length(64);
        RuleFor(x => x.Rows).NotEmpty().Must(x => x.Count <= 1000);
        RuleForEach(x => x.Rows).ChildRules(row =>
        {
            row.RuleFor(x => x.Description).MaximumLength(500);
            row.RuleFor(x => x.Counterparty).MaximumLength(200);
            row.RuleFor(x => x.Reference).MaximumLength(200);
            row.RuleFor(x => x.RawDescription).MaximumLength(1000);
            row.RuleFor(x => x.Currency).MaximumLength(3);
        });
    }
}
