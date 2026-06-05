namespace Hemma.Modules.Economy.Errors;

internal static class EconomyErrors
{
    public static readonly ErrorOr.Error SettingsAlreadyExist =
        ErrorOr.Error.Conflict("Economy.Settings.AlreadyExist", "Economy settings already exist for this household.");

    public static readonly ErrorOr.Error SettingsNotFound =
        ErrorOr.Error.NotFound("Economy.Settings.NotFound", "Economy settings were not found for this household.");

    public static readonly ErrorOr.Error CycleStartDayInvalid =
        ErrorOr.Error.Validation("Economy.Settings.CycleStartDayInvalid", "Cycle start day must be between 1 and 28.");

    public static readonly ErrorOr.Error CurrencyInvalid =
        ErrorOr.Error.Validation("Economy.Money.CurrencyInvalid", "Currency must be a three-letter ISO code.");

    public static readonly ErrorOr.Error AmountNegative =
        ErrorOr.Error.Validation("Economy.Money.AmountNegative", "Money amount cannot be negative.");

    public static readonly ErrorOr.Error CurrencyMismatch =
        ErrorOr.Error.Validation("Economy.Money.CurrencyMismatch", "Money values must use the same currency.");

    public static readonly ErrorOr.Error AccountNotFound =
        ErrorOr.Error.NotFound("Economy.Account.NotFound", "Account was not found.");

    public static readonly ErrorOr.Error AccountNameInvalid =
        ErrorOr.Error.Validation("Economy.Account.NameInvalid", "Account name is required and cannot exceed 100 characters.");

    public static readonly ErrorOr.Error AccountTypeInvalid =
        ErrorOr.Error.Validation("Economy.Account.TypeInvalid", "Account type is invalid.");

    public static readonly ErrorOr.Error CategoryNotFound =
        ErrorOr.Error.NotFound("Economy.Category.NotFound", "Category was not found.");

    public static readonly ErrorOr.Error CategoryNameInvalid =
        ErrorOr.Error.Validation("Economy.Category.NameInvalid", "Category name is required and cannot exceed 100 characters.");

    public static readonly ErrorOr.Error CategoryDepthExceeded =
        ErrorOr.Error.Validation("Economy.Category.DepthExceeded", "Categories can be nested at most two levels deep.");

    public static readonly ErrorOr.Error BudgetNotFound =
        ErrorOr.Error.NotFound("Economy.Budget.NotFound", "Budget was not found.");

    public static readonly ErrorOr.Error BudgetAlreadyExists =
        ErrorOr.Error.Conflict("Economy.Budget.AlreadyExists", "Budget already exists for this household and period.");

    public static readonly ErrorOr.Error BudgetLineNotAllowed =
        ErrorOr.Error.Validation("Economy.Budget.LineNotAllowed", "Category is not budgetable.");

    public static readonly ErrorOr.Error TransactionNotFound =
        ErrorOr.Error.NotFound("Economy.Transaction.NotFound", "Transaction was not found.");

    public static readonly ErrorOr.Error TransactionKindInvalid =
        ErrorOr.Error.Validation("Economy.Transaction.KindInvalid", "Transaction kind must be Expense or Income.");

    public static readonly ErrorOr.Error ReceiptBlobInvalid =
        ErrorOr.Error.Validation("Economy.Transaction.ReceiptBlobInvalid", "Receipt blob reference is invalid.");

    public static readonly ErrorOr.Error ReceiptFileInvalid =
        ErrorOr.Error.Validation("Economy.Transaction.ReceiptFileInvalid", "Receipt file must be a supported image or PDF and within the allowed size.");

    public static readonly ErrorOr.Error TransferModeInvalid =
        ErrorOr.Error.Validation("Economy.Transfer.ModeInvalid", "Transfer mode must be Neutral or Savings.");

    public static readonly ErrorOr.Error TransferInvalid =
        ErrorOr.Error.Validation("Economy.Transfer.Invalid", "Transfer must have two reconciling legs in the same household and currency.");
}
