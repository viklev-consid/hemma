using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.CreateArea;

public sealed class CreateAreaHandler(AreasTagsOperations operations)
{
    public Task<ErrorOr<PropertyAreaResponse>> Handle(CreateAreaCommand message, CancellationToken ct) => operations.CreateAreaAsync(message, ct);
}
