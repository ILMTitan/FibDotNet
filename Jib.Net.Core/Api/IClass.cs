using System;
using com.google.cloud.tools.jib.json;

namespace Jib.Net.Core.Api
{
    public interface IClass<out E>
    {
        Type Type { get; }

        bool Equals(object obj);
        int GetHashCode();
        IConstructor<E> getDeclaredConstructor();
        E cast<T>(T t);
    }
}