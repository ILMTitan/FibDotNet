namespace Jib.Net.Core.Api
{
    public interface IConstructor<out T>
    {
        T NewInstance();
    }
}