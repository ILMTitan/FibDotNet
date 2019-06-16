using System.IO;

namespace com.google.cloud.tools.jib.docker
{
    public interface IProcess
    {
        Stream getOutputStream();
        int waitFor();
        Stream getErrorStream();
        Stream getInputStream();
        TextReader GetErrorReader();
    }
}