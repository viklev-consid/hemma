using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Hemma.Api.Infrastructure.OpenApi;

/// <summary>
/// Refines generated product DTO schemas where the runtime model intentionally
/// uses strings but the HTTP contract has a closed value set.
/// </summary>
internal sealed class ProductContractSchemaTransformer : IOpenApiDocumentTransformer
{
    private static readonly IReadOnlyDictionary<string, string[]> enumSchemas =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["HouseholdRole"] = ["owner", "member"],
            ["PlatformRole"] = ["admin", "user"],
            ["AccountType"] = ["Spending", "Savings"],
            ["TransferMode"] = ["Neutral", "Savings"],
            ["SubscriptionLifecycleState"] = ["Trial", "Active", "Paused", "Cancelled"],
            ["RecurringBillType"] = ["Fixed", "Estimated"],
            ["RecurringBillDirection"] = ["Expense", "Income"],
            ["RecurringBillOccurrenceState"] = ["Pending", "Posted", "Confirmed", "Skipped", "Paused"],
            ["CadenceFrequency"] = ["Monthly"],
            ["ImportDuplicateState"] = ["None", "Exact", "Possible"],
            ["CategorizationRuleMatch"] = ["Contains", "Regex"],
            ["TransactionKind"] = ["Expense", "Income", "Transfer"],
            ["SubscriptionMatchState"] = ["actual", "predicted", "suggested"],
            ["Currency"] = ["SEK"],
            ["HouseholdAccessMode"] = ["ScopedPermission", "PlatformOverride"],
        };

    private static readonly (string SchemaName, string PropertyName, string EnumSchemaName)[] enumProperties =
    [
        ("AcceptHouseholdInvitationResponse", "role", "HouseholdRole"),
        ("ChangeHouseholdMemberRoleRequest", "role", "HouseholdRole"),
        ("ChangeHouseholdMemberRoleResponse", "role", "HouseholdRole"),
        ("CreateHouseholdInvitationRequest", "role", "HouseholdRole"),
        ("CreateHouseholdInvitationResponse", "role", "HouseholdRole"),
        ("CreateHouseholdResponse", "role", "HouseholdRole"),
        ("HouseholdInvitationItem", "role", "HouseholdRole"),
        ("HouseholdMemberItem", "role", "HouseholdRole"),
        ("MyHouseholdItem", "role", "HouseholdRole"),

        ("ChangeUserRoleRequest", "role", "PlatformRole"),
        ("ChangeUserRoleResponse", "newRole", "PlatformRole"),
        ("GetCurrentUserResponse", "role", "PlatformRole"),
        ("GetUserByIdResponse", "role", "PlatformRole"),
        ("ListUsersUserDto", "role", "PlatformRole"),

        ("AccountBalanceResponse", "type", "AccountType"),
        ("AccountResponse", "type", "AccountType"),
        ("CreateAccountRequest", "type", "AccountType"),

        ("CreateTransferRequest", "mode", "TransferMode"),
        ("CreateTransferResponse", "mode", "TransferMode"),

        ("CreateSubscriptionRequest", "lifecycleState", "SubscriptionLifecycleState"),
        ("ChangeLifecycleStateRequest", "lifecycleState", "SubscriptionLifecycleState"),
        ("SubscriptionResponse", "lifecycleState", "SubscriptionLifecycleState"),

        ("CreateRecurringBillRequest", "type", "RecurringBillType"),
        ("RecurringBillResponse", "type", "RecurringBillType"),
        ("CreateRecurringBillRequest", "direction", "RecurringBillDirection"),
        ("RecurringBillResponse", "direction", "RecurringBillDirection"),
        ("RecurringBillOccurrenceResponse", "state", "RecurringBillOccurrenceState"),

        ("CreateRecurringBillRequest", "cadenceFrequency", "CadenceFrequency"),
        ("RecurringBillResponse", "cadenceFrequency", "CadenceFrequency"),
        ("CreateSubscriptionRequest", "cadenceFrequency", "CadenceFrequency"),
        ("SubscriptionResponse", "cadenceFrequency", "CadenceFrequency"),

        ("ImportRowResponse", "duplicateState", "ImportDuplicateState"),
        ("CategorizationRuleRequest", "match", "CategorizationRuleMatch"),
        ("CategorizationRuleResponse", "match", "CategorizationRuleMatch"),
        ("ImportRuleSuggestionResponse", "match", "CategorizationRuleMatch"),

        ("RecordTransactionRequest", "kind", "TransactionKind"),
        ("TopTransactionResponse", "kind", "TransactionKind"),
        ("TransactionResponse", "kind", "TransactionKind"),

        ("ChargeHistoryItemResponse", "matchState", "SubscriptionMatchState"),
        ("MonthChargeResponse", "matchState", "SubscriptionMatchState"),
        ("SubscriptionMatchSuggestionResponse", "matchState", "SubscriptionMatchState"),

        ("CreateEconomySettingsRequest", "defaultCurrency", "Currency"),
        ("CreateEconomySettingsResponse", "defaultCurrency", "Currency"),
        ("ImportRowResponse", "currency", "Currency"),
        ("MoneyRequest", "currency", "Currency"),
        ("MoneyResponse", "currency", "Currency"),
        ("NormalizedImportRowRequest", "currency", "Currency"),

        ("GetHouseholdAuditResponse", "accessMode", "HouseholdAccessMode"),
        ("GetHouseholdResponse", "accessMode", "HouseholdAccessMode"),
    ];

    private static readonly (string SchemaName, string PropertyName)[] integerProperties =
    [
        ("CreateRecurringBillRequest", "cadenceInterval"),
        ("CreateRecurringBillRequest", "cadenceDayOfMonth"),
        ("RecurringBillResponse", "cadenceInterval"),
        ("RecurringBillResponse", "cadenceDayOfMonth"),
        ("CreateSubscriptionRequest", "cadenceInterval"),
        ("SubscriptionResponse", "cadenceInterval"),
    ];

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);

        foreach (var (name, values) in enumSchemas)
        {
            document.Components.Schemas[name] = CreateStringEnumSchema(values);
        }

        foreach (var (schemaName, propertyName, enumSchemaName) in enumProperties)
        {
            ReplaceProperty(document, schemaName, propertyName, new OpenApiSchemaReference(enumSchemaName, document));
        }

        foreach (var (schemaName, propertyName) in integerProperties)
        {
            ReplaceProperty(document, schemaName, propertyName, new OpenApiSchema
            {
                Type = JsonSchemaType.Integer,
                Format = "int32",
            });
        }

        ReplaceProperty(document, "MoneyRequest", "amount", CreateDecimalStringSchema());
        ReplaceProperty(document, "MoneyResponse", "amount", CreateDecimalStringSchema());

        return Task.CompletedTask;
    }

    private static OpenApiSchema CreateStringEnumSchema(IEnumerable<string> values) =>
        new()
        {
            Type = JsonSchemaType.String,
            Enum = [.. values.Select(value => JsonValue.Create(value)!)]
        };

    private static OpenApiSchema CreateDecimalStringSchema() =>
        new()
        {
            Type = JsonSchemaType.String,
            Pattern = "^-?(?:0|[1-9]\\d*)(?:\\.\\d+)?$",
        };

    private static void ReplaceProperty(
        OpenApiDocument document,
        string schemaName,
        string propertyName,
        IOpenApiSchema propertySchema)
    {
        if (document.Components?.Schemas is null ||
            !document.Components.Schemas.TryGetValue(schemaName, out var schema) ||
            schema.Properties is null ||
            !schema.Properties.ContainsKey(propertyName))
        {
            return;
        }

        schema.Properties[propertyName] = propertySchema;
    }
}
