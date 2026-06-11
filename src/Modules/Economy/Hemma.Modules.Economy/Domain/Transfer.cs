using ErrorOr;
using Hemma.Modules.Economy.Errors;
using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Economy.Domain;

public sealed class Transfer : AggregateRoot<TransferId>
{
    private Transfer(
        TransferId id,
        Guid householdId,
        TransactionId outflowTransactionId,
        TransactionId inflowTransactionId,
        TransferMode mode) : base(id)
    {
        HouseholdId = householdId;
        OutflowTransactionId = outflowTransactionId;
        InflowTransactionId = inflowTransactionId;
        Mode = mode;
    }

    private Transfer() : base(default!) { }

    public Guid HouseholdId { get; private set; }
    public TransactionId OutflowTransactionId { get; private set; } = null!;
    public TransactionId InflowTransactionId { get; private set; } = null!;
    public TransferMode Mode { get; private set; } = null!;

    public static ErrorOr<Transfer> Create(
        Guid householdId,
        Transaction outflow,
        Transaction inflow,
        TransferMode mode)
    {
        if (outflow.HouseholdId != householdId || inflow.HouseholdId != householdId)
        {
            return EconomyErrors.TransferInvalid;
        }

        if (outflow.Kind != TransactionKind.Transfer || inflow.Kind != TransactionKind.Transfer)
        {
            return EconomyErrors.TransferInvalid;
        }

        if (!outflow.IsTransferOutflow || inflow.IsTransferOutflow)
        {
            return EconomyErrors.TransferInvalid;
        }

        if (outflow.Amount.Amount != inflow.Amount.Amount ||
            !string.Equals(outflow.Amount.Currency, inflow.Amount.Currency, StringComparison.Ordinal))
        {
            return EconomyErrors.TransferInvalid;
        }

        return new Transfer(outflow.TransferId!, householdId, outflow.Id, inflow.Id, mode);
    }
}
