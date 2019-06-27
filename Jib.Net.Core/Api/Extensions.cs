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

using com.google.cloud.tools.jib.api;
using ICSharpCode.SharpZipLib.Tar;
using Jib.Net.Core.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Jib.Net.Core.Global
{
    internal static class JavaExtensions
    {
        public static TValue GetOrDefault<TKey, TValue>(this ImmutableDictionary<TKey, TValue> dic, TKey key, TValue defaultValue)
        {
            if (dic.TryGetValue(key, out var value))
            {
                return value;
            }
            else
            {
                return defaultValue;
            }
        }

        public static bool Contains<T>(this ISet<T> set, T value)
        {
            return set.Contains(value);
        }

        public static SystemPath ToPath(this FileSystemInfo fileInfo)
        {
            return new SystemPath(fileInfo);
        }

        public static bool IsFile(this TarEntry e)
        {
            return e.TarHeader.TypeFlag != TarHeader.LF_DIR;
        }

        public static int Size<T>(this IReadOnlyCollection<T> c)
        {
            return c.Count;
        }

        public static void Close(this IDisposable d)
        {
            d.Dispose();
        }

        public static string Replace(this string s, char oldValue, char newValue)
        {
            return s.Replace(oldValue, newValue);
        }

        public static void SetMode(this TarEntry e, PosixFilePermissions mode)
        {
            e.TarHeader.Mode = (int)mode;
        }

        public static PosixFilePermissions GetMode(this TarEntry e)
        {
            return (PosixFilePermissions)e.TarHeader.Mode;
        }

        public static bool Contains(this string s, string value)
        {
            return s.Contains(value);
        }

        public static void Write(this Stream s, byte[] bytes)
        {
            s.Write(bytes, 0, bytes.Length);
        }

        public static int IndexOf(this string s, char c)
        {
            return s.IndexOf(c);
        }

        public static string Substring(this string s, int startIndex, int length)
        {
            return s.Substring(startIndex, length);
        }

        public static StringBuilder Append(this StringBuilder b, object value)
        {
            return b.Append(value);
        }

        public static bool StartsWith(this string s, string prefix)
        {
            return s.StartsWith(prefix, StringComparison.Ordinal);
        }

        public static TValue GetValue<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp)
        {
            return kvp.Value;
        }

        public static TKey GetKey<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp)
        {
            return kvp.Key;
        }

        public static bool Test<T>(this Predicate<T> p, T value)
        {
            return p(value);
        }

        public static Maybe<T> FindFirst<T>(this IEnumerable<Maybe<T>> e)
        {
            return e.FirstOrDefault();
        }

        public static Maybe<T> FindFirst<T>(this IEnumerable<T> e)
        {
            return e.Select(Maybe.Of).FirstOrDefault();
        }

        public static bool EndsWith(this string s, string suffix)
        {
            return s.EndsWith(suffix, StringComparison.Ordinal);
        }

        public static string Trim(this string s)
        {
            return s.Trim();
        }

        public static bool IsEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }

        public static Func<T, bool> And<T>(this Func<T, bool> first, Func<T, bool> second)
        {
            return i => first(i) && second(i);
        }

        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key)
        {
            if (!d.ContainsKey(key))
            {
                return default;
            }
            return d[key];
        }

        public static void PutAll<TKey, TValue>(this IDictionary<TKey, TValue> d, IEnumerable<KeyValuePair<TKey, TValue>> entries)
        {
            foreach (var (k, v) in entries)
            {
                d[k]= v;
            }
        }

        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }

        public static T Remove<T>(this IList<T> c, int i)
        {
            var temp = c[i];
            c.RemoveAt(i);
            return temp;
        }

        public static int CompareTo<T>(this IComparable<T> comparable, T other)
        {
            return comparable.CompareTo(other);
        }

        public static string ToString(this object o)
        {
            return o.ToString();
        }

        public static void Add<T>(this ICollection<T> set, T value)
        {
            set.Add(value);
        }

        public static TCollection Add<TCollection, T>(this TCollection set, params T[] values) where TCollection : ICollection<T>
        {
            foreach (T v in values)
            {
                set.Add(v);
            }
            return set;
        }

        public static bool Matches(this string s, string regex)
        {
            regex = ForFullString(regex);
            return Regex.IsMatch(s, regex);
        }

        private static string ForFullString(string regex)
        {
            if (!regex.StartsWith("^", StringComparison.Ordinal) || !regex.EndsWith("$", StringComparison.Ordinal))
            {
                regex = "^(?:" + regex +")$";
            }

            return regex;
        }

        public static string GetMessage(this Exception e)
        {
            return e.Message;
        }

        public static TResult Apply<T1, T2, TResult>(this Func<T1, T2, TResult> f, T1 value1, T2 value2)
        {
            return f(value1, value2);
        }

        public static void Add<T>(this IList<T> l, T value)
        {
            l.Add(value);
        }

        public static void AddAll<T>(this IList<T> l, IEnumerable<T> values)
        {
            foreach (T value in values)
            {
                l.Add(value);
            }
        }
    }
}
