using Fib.Net.Core.Api;

namespace Fib.Net.Core.Configuration
{
    public interface IEventHandlers
    {
        void Dispatch(IFibEvent fibEvent);
    }
}