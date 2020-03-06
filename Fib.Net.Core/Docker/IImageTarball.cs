using System.IO;
using System.Threading.Tasks;

namespace Fib.Net.Core.Docker
{
    public interface IImageTarball
    {
        Task WriteToAsync(Stream stream);
    }
}