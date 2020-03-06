using System.IO;
using System.Threading.Tasks;

namespace Fib.Net.Core.Docker
{
    public interface IProcess
    {
        Stream GetOutputStream();
        int WaitFor();
        Task<int> WhenFinishedAsync();
        Stream GetErrorStream();
        Stream GetInputStream();
        TextReader GetErrorReader();
    }
}