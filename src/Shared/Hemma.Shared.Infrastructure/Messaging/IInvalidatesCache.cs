namespace Hemma.Shared.Infrastructure.Messaging;

public interface IInvalidatesCache
{
    string[] CacheKeys { get; }
}
