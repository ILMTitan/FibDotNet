using Jib.Net.Core.Api;

namespace Jib.Net.Core.Configuration
{
    public interface IEventHandlers
    {
        void Dispatch(IJibEvent jibEvent);
    }
}