/*
 * Copyright 2018 Google LLC.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 */

using System;
using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.image.json;

namespace Jib.Net.Core.Api
{
    public class Class<E> : IClass<E>
    {
        public Type Type => throw new NotImplementedException();

        public static implicit operator Class<E>(Type v)
        {
            throw new NotImplementedException();
        }
        public static bool operator ==(Class<E> c, Class<E> t)
        {
            return c?.GetClassType() == t?.GetClassType();
        }
        public static bool operator !=(Class<E> c, Class<E> t)
        {
            return c?.GetClassType() != t?.GetClassType();
        }

        private Type GetClassType()
        {
            throw new NotImplementedException();
        }

        internal bool isInstance(object jibEvent)
        {
            throw new NotImplementedException();
        }

        internal E cast(object jibEvent)
        {
            throw new NotImplementedException();
        }

        internal Constructor<E> getDeclaredConstructor()
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Class<E> other))
            {
                return false;
            }
            return GetClassType() == other.GetClassType();
        }

        public override int GetHashCode()
        {
            return GetClassType().GetHashCode();
        }

        IConstructor<E> IClass<E>.getDeclaredConstructor()
        {
            throw new NotImplementedException();
        }

        public E cast<T>(T t)
        {
            throw new NotImplementedException();
        }

        public class Constructor<T>
        {
            internal T newInstance()
            {
                throw new NotImplementedException();
            }
        }
    }
}