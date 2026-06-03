using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Users.Gdpr;

public sealed class PersonalDataOrchestrator(
    IEnumerable<IPersonalDataExporter> exporters,
    IEnumerable<IPersonalDataEraser> erasers)
{
    public IEnumerable<IPersonalDataExporter> Exporters => exporters;
    public IEnumerable<IPersonalDataEraser> Erasers => erasers;
}
