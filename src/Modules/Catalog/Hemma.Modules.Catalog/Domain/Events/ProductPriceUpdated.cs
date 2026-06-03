using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Catalog.Domain.Events;

internal sealed record ProductPriceUpdated(ProductId ProductId, decimal OldAmount, decimal NewAmount, string Currency) : DomainEvent;
