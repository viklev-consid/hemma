using FluentValidation;

namespace Hemma.Modules.Property.Features.AddLink;

internal sealed class AddLinkValidator : AbstractValidator<ProjectLinkRequest>
{
    public AddLinkValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2048).Must(BeHttpUrl);
    }

    private static bool BeHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
}
