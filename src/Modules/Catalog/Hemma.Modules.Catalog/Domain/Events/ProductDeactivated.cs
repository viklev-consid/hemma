using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Catalog.Domain.Events;

internal sealed record ProductDeactivated(ProductId ProductId) : DomainEvent;
