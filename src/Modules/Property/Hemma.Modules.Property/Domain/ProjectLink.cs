using ErrorOr;
using Hemma.Modules.Property.Errors;
using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Property.Domain;

public sealed class ProjectLink : Entity<ProjectLinkId>
{
    private ProjectLink(ProjectLinkId id, ProjectId projectId, string label, string url) : base(id)
    {
        ProjectId = projectId;
        Label = label;
        Url = url;
    }

    private ProjectLink() : base(default!) { }

    public ProjectId ProjectId { get; private set; } = null!;
    public string Label { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;

    public static ErrorOr<ProjectLink> Create(ProjectId projectId, string label, string url)
    {
        var normalizedLabel = NormalizeRequired(label, 160);
        if (normalizedLabel is null)
        {
            return PropertyErrors.LinkLabelInvalid;
        }

        var normalizedUrl = NormalizeRequired(url, 2048);
        if (normalizedUrl is null || !IsAllowedUrl(normalizedUrl))
        {
            return PropertyErrors.LinkUrlInvalid;
        }

        return new ProjectLink(ProjectLinkId.New(), projectId, normalizedLabel, normalizedUrl);
    }

    private static string? NormalizeRequired(string value, int maxLength)
    {
        var normalized = value.Trim();
        return normalized.Length is 0 || normalized.Length > maxLength ? null : normalized;
    }

    private static bool IsAllowedUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
}
