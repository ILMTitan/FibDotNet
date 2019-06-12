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
using System.Collections.Generic;
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core;
using Jib.Net.Core.FileSystem;

namespace com.google.cloud.tools.jib.api {


    public struct Optional<T>
    {
        public bool isPresent()
        {
            throw new NotImplementedException();
        }

        internal T orElseGet(Supplier<T> supplier)
        {
            throw new NotImplementedException();
        }

        internal Optional<T> ofNullable(T value)
        {
            throw new NotImplementedException();
        }

        internal void ifPresent(Action<T> action)
        {
            throw new NotImplementedException();
        }

        internal void ifPresent<R>(Func<T, R> func)
        {
            throw new NotImplementedException();
        }

        public T get()
        {
            throw new NotImplementedException();
        }

        public T orElseThrow(Func<Exception> p)
        {
            throw new NotImplementedException();
        }

        public Optional<TResult> map<TResult>(Func<T, TResult> p)
        {
            throw new NotImplementedException();
        }
    }
    public static class Optional
    {
        public static Optional<T> of<T>(T credential)
        {
            throw new NotImplementedException();
        }

        internal static Optional<T> ofNullable<T>(T dockerExecutable)
        {
            throw new NotImplementedException();
        }

        internal static Optional<Credential> empty()
        {
            throw new NotImplementedException();
        }

        public static Optional<T> empty<T>()
        {
            throw new NotImplementedException();
        }

        public static T orElse<T>(this Optional<T> o, T defaultValue) where T : class
        {
            if (o.isPresent())
            {
                return o.get();
            } else
            {
                return defaultValue;
            }
        }

        public static T? asNullable<T>(this Optional<T> o) where T:struct
        {
            throw new NotImplementedException();
        }
    }
}
