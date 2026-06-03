using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Catalog.Domain.Events;

internal sealed record ProductCreated(ProductId ProductId, string Sku, string Name) : DomainEvent;
