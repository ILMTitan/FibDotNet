﻿/*
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
using Jib.Net.Core;
using Jib.Net.Core.FileSystem;

namespace com.google.cloud.tools.jib.api
{
    public struct Option<T> : IEquatable<Option<T>>
    {
        private readonly bool present;
        private readonly T value;

        public Option(T value)
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

        internal Option<R> IfPresent<R>(Func<T, R> func)
        {
            if (present)
            {
                return new Option<R>(func(value));
            } else
            {
                return new Option<R>();
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
            return obj is Option<T> optional && Equals(optional);
        }

        public bool Equals(Option<T> other)
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

        public static bool operator ==(Option<T> left, Option<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Option<T> left, Option<T> right)
        {
            return !(left == right);
        }
    }

    public static class Option
    {
        public static Option<T> Of<T>(T value)
        {
            return new Option<T>(value);
        }

        internal static Option<T> OfNullable<T>(T value)
        {
            if (value == null)
            {
                return Option.Empty<T>();
            } else {
                return Option.Of(value);
            }
        }

        public static Option<T> Empty<T>()
        {
            return new Option<T>();
        }

        public static T OrElse<T>(this Option<T> o, T defaultValue) where T : class
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

        public static T? AsNullable<T>(this Option<T> o) where T : struct
        {
            if (o.IsPresent())
            {
                return o.Get();
            } else
            {
                return null;
            }
        }
    }
}
