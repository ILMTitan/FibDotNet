// Copyright 2018 Google LLC.
// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// NOTICE: This file was modified by James Przybylinski to be C#.

using System;
using System.Collections.Generic;

namespace Fib.Net.Core.Api
{
    public struct Maybe<T> : IEquatable<Maybe<T>>
    {
        private readonly bool present;
        private readonly T value;

        public Maybe(T value)
        {
            this.value = value;
            present = true;
        }

        public bool IsPresent()
        {
            return present;
        }

        internal void IfPresent(Action<T> action)
        {
            if (present)
            {
                action(value);
            }
        }

        internal Maybe<R> IfPresent<R>(Func<T, R> func)
        {
            if (present)
            {
                return new Maybe<R>(func(value));
            }
            else
            {
                return new Maybe<R>();
            }
        }

        public T Get()
        {
            return value;
        }

        public T OrElseThrow(Func<Exception> p)
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
            return obj is Maybe<T> optional && Equals(optional);
        }

        public bool Equals(Maybe<T> other)
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

        public static bool operator ==(Maybe<T> left, Maybe<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Maybe<T> left, Maybe<T> right)
        {
            return !(left == right);
        }
    }

    public static class Maybe
    {
        public static Maybe<T> Of<T>(T value)
        {
            return new Maybe<T>(value);
        }

        internal static Maybe<T> OfNullable<T>(T value)
        {
            if (value == null)
            {
                return Empty<T>();
            }
            else
            {
                return Of(value);
            }
        }

        public static Maybe<T> Empty<T>()
        {
            return new Maybe<T>();
        }

        public static T OrElse<T>(this Maybe<T> o, T defaultValue) where T : class
        {
            if (o.IsPresent())
            {
                return o.Get();
            }
            else
            {
                return defaultValue;
            }
        }

        public static T? AsNullable<T>(this Maybe<T> o) where T : struct
        {
            if (o.IsPresent())
            {
                return o.Get();
            }
            else
            {
                return null;
            }
        }
    }
}
