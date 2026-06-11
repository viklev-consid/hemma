using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.CreateTransfer;

public sealed record CreateTransferResponse(
    Guid TransferId,
    string Mode,
    TransactionResponse Outflow,
    TransactionResponse Inflow);
