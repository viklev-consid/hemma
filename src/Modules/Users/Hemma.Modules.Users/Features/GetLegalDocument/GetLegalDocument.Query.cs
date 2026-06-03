using Hemma.Modules.Users.Domain;

namespace Hemma.Modules.Users.Features.GetLegalDocument;

public sealed record GetLegalDocumentQuery(LegalDocumentType DocumentType, string Version);
