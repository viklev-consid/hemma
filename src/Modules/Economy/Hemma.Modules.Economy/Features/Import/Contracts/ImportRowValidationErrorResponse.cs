namespace Hemma.Modules.Economy.Features.Import.Contracts;

public sealed record ImportRowValidationErrorResponse(string Field, string Message);
