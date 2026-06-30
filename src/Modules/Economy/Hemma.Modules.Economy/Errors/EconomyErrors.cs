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
        ErrorOr.Error.Validation("Economy.Money.CurrencyInvalid", "Currency must be SEK.");

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

    public static readonly ErrorOr.Error ProjectNotFound =
        ErrorOr.Error.NotFound("Economy.Project.NotFound", "Project was not found.");

    public static readonly ErrorOr.Error TransactionKindInvalid =
        ErrorOr.Error.Validation("Economy.Transaction.KindInvalid", "Transaction kind must be Expense or Income.");

    public static readonly ErrorOr.Error TransactionTransferMutationNotAllowed =
        ErrorOr.Error.Conflict("Economy.Transaction.TransferMutationNotAllowed", "Transfer transactions cannot be edited or deleted through transaction endpoints.");

    public static readonly ErrorOr.Error TransactionNotPending =
        ErrorOr.Error.Conflict("Economy.Transaction.NotPending", "Transaction is not pending confirmation.");

    public static readonly ErrorOr.Error ReceiptBlobInvalid =
        ErrorOr.Error.Validation("Economy.Transaction.ReceiptBlobInvalid", "Receipt blob reference is invalid.");

    public static readonly ErrorOr.Error ReceiptFileInvalid =
        ErrorOr.Error.Validation("Economy.Transaction.ReceiptFileInvalid", "Receipt file must be a supported image or PDF and within the allowed size.");

    public static readonly ErrorOr.Error TransferModeInvalid =
        ErrorOr.Error.Validation("Economy.Transfer.ModeInvalid", "Transfer mode must be Neutral or Savings.");

    public static readonly ErrorOr.Error TransferInvalid =
        ErrorOr.Error.Validation("Economy.Transfer.Invalid", "Transfer must have two reconciling legs in the same household and currency.");

    public static readonly ErrorOr.Error RecurringBillNotFound =
        ErrorOr.Error.NotFound("Economy.RecurringBill.NotFound", "Recurring bill was not found.");

    public static readonly ErrorOr.Error RecurringBillInvalid =
        ErrorOr.Error.Validation("Economy.RecurringBill.Invalid", "Recurring bill is invalid.");

    public static readonly ErrorOr.Error RecurringBillTypeInvalid =
        ErrorOr.Error.Validation("Economy.RecurringBill.TypeInvalid", "Recurring bill type must be Fixed or Estimated.");

    public static readonly ErrorOr.Error RecurringBillDirectionInvalid =
        ErrorOr.Error.Validation("Economy.RecurringBill.DirectionInvalid", "Recurring bill direction must be Expense or Income.");

    public static readonly ErrorOr.Error RecurringBillCadenceInvalid =
        ErrorOr.Error.Validation("Economy.RecurringBill.CadenceInvalid", "Recurring bill cadence must have a valid frequency, interval, and day of month.");

    public static readonly ErrorOr.Error RecurringBillOccurrenceInvalid =
        ErrorOr.Error.Validation("Economy.RecurringBill.OccurrenceInvalid", "Recurring bill occurrence cannot be changed.");

    public static readonly ErrorOr.Error CategorizationRuleNotFound =
        ErrorOr.Error.NotFound("Economy.CategorizationRule.NotFound", "Categorization rule was not found.");

    public static readonly ErrorOr.Error CategorizationRuleMatchInvalid =
        ErrorOr.Error.Validation("Economy.CategorizationRule.MatchInvalid", "Categorization rule match must be Contains or Regex.");

    public static readonly ErrorOr.Error CategorizationRulePatternInvalid =
        ErrorOr.Error.Validation("Economy.CategorizationRule.PatternInvalid", "Categorization rule pattern is required, must be at most 200 characters, and must be valid for regex rules.");

    public static readonly ErrorOr.Error CategorizationRuleLimitExceeded =
        ErrorOr.Error.Conflict("Economy.CategorizationRule.LimitExceeded", "A household can have at most 100 enabled categorization rules.");

    public static readonly ErrorOr.Error ImportFingerprintInvalid =
        ErrorOr.Error.Validation("Economy.Import.FingerprintInvalid", "Import preview fingerprint is invalid.");

    public static readonly ErrorOr.Error ImportFingerprintMismatch =
        ErrorOr.Error.Conflict("Economy.Import.FingerprintMismatch", "Imported rows do not match the preview fingerprint.");

    public static readonly ErrorOr.Error SubscriptionNotFound =
        ErrorOr.Error.NotFound("Economy.Subscription.NotFound", "Subscription was not found.");

    public static readonly ErrorOr.Error SubscriptionInvalid =
        ErrorOr.Error.Validation("Economy.Subscription.Invalid", "Subscription is invalid.");

    public static readonly ErrorOr.Error SubscriptionCadenceInvalid =
        ErrorOr.Error.Validation("Economy.Subscription.CadenceInvalid", "Subscription cadence must have a valid frequency, interval, and charge day.");

    public static readonly ErrorOr.Error SubscriptionLifecycleStateInvalid =
        ErrorOr.Error.Validation("Economy.Subscription.LifecycleStateInvalid", "Subscription lifecycle state must be Trial, Active, Paused, or Cancelled.");

    public static readonly ErrorOr.Error SubscriptionTrialEndRequired =
        ErrorOr.Error.Validation("Economy.Subscription.TrialEndRequired", "Trial subscriptions must include a trial end date.");

    public static readonly ErrorOr.Error TransactionSubscriptionLinkNotFound =
        ErrorOr.Error.NotFound("Economy.Transaction.SubscriptionLinkNotFound", "Transaction is not linked to the subscription.");

    public static readonly ErrorOr.Error TransactionAlreadyLinked =
        ErrorOr.Error.Conflict("Economy.Transaction.AlreadyLinked", "Transaction is already linked to another subscription. Unlink it first.");
}
