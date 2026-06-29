using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.UpdateHistoryEntry;

internal sealed class UpdateHistoryEntryValidator : AbstractValidator<HistoryEntryRequest>
{
    public UpdateHistoryEntryValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Type).NotEmpty().Must(type => Enum.TryParse<Domain.HistoryEntryType>(type, ignoreCase: true, out _));
        RuleForEach(x => x.PhotoRefs).ChildRules(photo =>
        {
            photo.RuleFor(x => x.Container).NotEmpty().MaximumLength(ProjectAttachmentRules.MaxBlobContainerLength);
            photo.RuleFor(x => x.Key).NotEmpty().MaximumLength(ProjectAttachmentRules.MaxBlobKeyLength);
        });
    }
}
