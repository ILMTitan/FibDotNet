using System.IO;

namespace com.google.cloud.tools.jib.docker
{
    public interface IProcess
    {
        Stream GetOutputStream();
        int WaitFor();
        Stream GetErrorStream();
        Stream GetInputStream();
        TextReader GetErrorReader();
    }
}