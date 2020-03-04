using System.IO;
using System.Threading.Tasks;

namespace Jib.Net.Core.Docker
{
    public interface IImageTarball
    {
        Task WriteToAsync(Stream stream);
    }
}