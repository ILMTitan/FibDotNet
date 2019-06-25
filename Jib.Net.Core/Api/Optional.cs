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

namespace com.google.cloud.tools.jib.api
{
    public struct Optional<T> : IEquatable<Optional<T>>
    {
        private readonly bool present;
        private readonly T value;

        public Optional(T value)
        {
            this.value = value;
            present = true;
        }

        public bool isPresent()
        {
            return present;
        }

        internal void ifPresent(Action<T> action)
        {
            if (present)
            {
                action(value);
            }
        }

        internal Optional<R> ifPresent<R>(Func<T, R> func)
        {
            if (present)
            {
                return new Optional<R>(func(value));
            } else
            {
                return new Optional<R>();
            }
        }

        public T get()
        {
            return value;
        }

        public T orElseThrow(Func<Exception> p)
        {
            p = p ?? throw new ArgumentNullException(nameof(p));
            if (present)
            {
                return value;
            }
            else
            {
                throw p();
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Optional<T> optional && Equals(optional);
        }

        public bool Equals(Optional<T> other)
        {
            return present == other.present &&
                   EqualityComparer<T>.Default.Equals(value, other.value);
        }

        public override int GetHashCode()
        {
            var hashCode = 1691996566;
            hashCode = (hashCode * -1521134295) + present.GetHashCode();
            hashCode = (hashCode * -1521134295) + EqualityComparer<T>.Default.GetHashCode(value);
            return hashCode;
        }

        public static bool operator ==(Optional<T> left, Optional<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Optional<T> left, Optional<T> right)
        {
            return !(left == right);
        }
    }

    public static class Optional
    {
        public static Optional<T> of<T>(T value)
        {
            return new Optional<T>(value);
        }

        internal static Optional<T> ofNullable<T>(T value)
        {
            if (value == null)
            {
                return Optional.empty<T>();
            } else {
                return Optional.of(value);
            }
        }

        public static Optional<T> empty<T>()
        {
            return new Optional<T>();
        }

        public static T orElse<T>(this Optional<T> o, T defaultValue) where T : class
        {
            if (o.isPresent())
            {
                return o.get();
            }
            else
            {
                return defaultValue;
            }
        }

        public static T? asNullable<T>(this Optional<T> o) where T : struct
        {
            if (o.isPresent())
            {
                return o.get();
            } else
            {
                return null;
            }
        }
    }
}
