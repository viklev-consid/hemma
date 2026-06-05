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
    }
}
