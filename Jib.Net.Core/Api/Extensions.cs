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
using com.google.cloud.tools.jib.http;
using com.google.cloud.tools.jib.registry;
using ICSharpCode.SharpZipLib.Tar;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using NodaTime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jib.Net.Core.Global
{
    internal static class JavaExtensions
    {

        public static void SetLocation(this HttpResponseHeaders h, string location)
        {
            h.Location = new Uri(location);
        }

        public static async Task<string> ParseAsStringAsync(this HttpResponseMessage m)
        {
            return await m.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public static async Task WriteToAsync(this BlobHttpContent c, Stream s)
        {
            await c.CopyToAsync(s).ConfigureAwait(false);
        }

        public static ErrorCode Name(this ErrorCode e)
        {
            return e;
        }

        public static bool IsDone(this Task t)
        {
            return t.IsCompleted;
        }

        public static bool ContainsKey<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key)
        {
            return d.ContainsKey(key);
        }

        public static int IntValue(this int? i)
        {
            return i.GetValueOrDefault();
        }

        public static long LongValue(this long? l)
        {
            return l.GetValueOrDefault();
        }

        public static long LongValue(this long l)
        {
            return l;
        }

        public static bool IsEmpty<T>(this Queue<T> queue)
        {
            return queue.Count == 0;
        }

        public static bool Offer<T>(this Queue<T> queue, T item)
        {
            queue.Enqueue(item);
            return true;
        }

        public static DateTime GetLastModifiedDate(this TarEntry e)
        {
            return e.TarHeader.ModTime;
        }

        public static DateTime GetModTime(this TarEntry e)
        {
            return DateTime.SpecifyKind(e.ModTime, DateTimeKind.Utc);
        }

        public static bool IsDirectory(this TarEntry e)
        {
            return e.IsDirectory;
        }

        public static char[] ToCharArray(this string s)
        {
            return s.ToCharArray();
        }

        public static int Release(this SemaphoreSlim s)
        {
            return s.Release();
        }

        public static void Acquire(this SemaphoreSlim s)
        {
            s.Wait();
        }

        public static void Write(this Stream s, byte[] buffer, int offset, int count)
        {
            s.Write(buffer, offset, count);
        }

        public static void Write(this Stream s, byte b)
        {
            s.WriteByte(b);
        }

        public static T Remove<T>(this Queue<T> queue)
        {
            return queue.Dequeue();
        }

        public static void Add<T>(this Queue<T> queue, T value)
        {
            queue.Enqueue(value);
        }

        public static string GetPath(this FileSystemInfo info)
        {
            return info.FullName;
        }

        public static TarEntry GetNextEntry(this TarInputStream i)
        {
            return i.GetNextEntry();
        }

        public static TarEntry GetNextTarEntry(this TarInputStream i)
        {
            return i.GetNextEntry();
        }

        public static Instant PlusSeconds(this Instant i, int seconds)
        {
            return i + Duration.FromSeconds(seconds);
        }

        public static T Poll<T>(this Queue<T> q)
        {
            return q.Dequeue();
        }

        public static Instant PlusNanos(this Instant i, int nanos)
        {
            return i.PlusNanoseconds(nanos);
        }

        public static Instant PlusMillis(this Instant i, int mills)
        {
            return i + Duration.FromMilliseconds(mills);
        }

        public static IList<T> AsList<T>(this IEnumerable<T> e)
        {
            return e.ToList();
        }

        public static int GroupCount(this Match m)
        {
            return m.Groups.Count;
        }

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

        public static void Run(this Action a)
        {
            a();
        }

        public static Uri GetRequestUrl(this HttpResponseMessage message)
        {
            return message.RequestMessage.RequestUri;
        }

        public static int Size<T>(this ImmutableArray<T> l)
        {
            return l.Length;
        }

        public static void SetSize(this TarEntry e, long size)
        {
            e.TarHeader.Size = size;
        }

        public static SystemPath ToPath(this FileSystemInfo fileInfo)
        {
            return new SystemPath(fileInfo);
        }

        public static FileInfo GetFile(this TarEntry e)
        {
            return new FileInfo(e.File);
        }

        public static bool IsFile(this TarEntry e)
        {
            return e.TarHeader.TypeFlag != TarHeader.LF_DIR;
        }

        public static void CloseArchiveEntry(this TarOutputStream s)
        {
            s.CloseEntry();
        }

        public static void PutArchiveEntry(this TarOutputStream s, TarEntry e)
        {
            s.PutNextEntry(e);
        }

        public static Uri GetLocation(this HttpResponseHeaders h)
        {
            return h.Location;
        }

        public static Uri ToURL(this UriBuilder b)
        {
            return b.Uri;
        }

        public static void SetScheme(this UriBuilder b, string scheme)
        {
            b.Scheme = scheme;
        }

        public static int GetPort(this Uri uri)
        {
            return uri.Port;
        }

        public static string ToLowerCase(this string s, CultureInfo ci)
        {
            return s.ToLower(ci);
        }

        public static void SetUserAgent(this HttpRequestHeaders h, string input)
        {
            h.UserAgent.Clear();
            h.UserAgent.Add(ProductInfoHeaderValue.Parse(input));
        }

        public static void SetAuthorization(this HttpRequestHeaders h, string input)
        {
            h.Authorization = AuthenticationHeaderValue.Parse(input);
        }

        public static void SetAccept(this HttpRequestHeaders h, string input)
        {
            h.Accept.Clear();
            h.Accept.ParseAdd(input);
        }

        public static async Task<Stream> GetBodyAsync(this HttpResponseMessage m)
        {
            return await m.Content.ReadAsStreamAsync().ConfigureAwait(false);
        }

        public static IList<string> GetHeader(this HttpResponseMessage m, string name)
        {
            return m.Headers.GetValues(name).ToList();
        }

        public static T Get<T>(this ImmutableArray<T> array, int index)
        {
            return array[index];
        }

        public static T Get<T>(this IReadOnlyList<T> c, int index)
        {
            return c[index];
        }

        public static T Get<T>(this List<T> c, int index)
        {
            return c[index];
        }

        public static int Size<T>(this ImmutableHashSet<T> c)
        {
            return c.Count;
        }

        public static int Size<TKey, TValue>(this ImmutableDictionary<TKey, TValue> d)
        {
            return d.Count;
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

        public static string Replace(this string s, string oldValue, string newValue)
        {
            return s.Replace(oldValue, newValue);
        }

        public static byte[] ToByteArray(this MemoryStream s)
        {
            return s.ToArray();
        }

        public static void SetGroupName(this TarEntry e, string name)
        {
            e.TarHeader.GroupName = name;
        }

        public static void SetUserName(this TarEntry e, string name)
        {
            e.TarHeader.UserName = name;
        }

        public static void SetUserId(this TarEntry e, int userId)
        {
            e.TarHeader.UserId = userId;
        }

        public static void SetGroupId(this TarEntry e, int groupId)
        {
            e.TarHeader.GroupId = groupId;
        }

        public static void SetMode(this TarEntry e, PosixFilePermissions mode)
        {
            e.TarHeader.Mode = (int)mode;
        }

        public static PosixFilePermissions GetMode(this TarEntry e)
        {
            return (PosixFilePermissions)e.TarHeader.Mode;
        }

        public static async Task<string> GetContentAsync(this HttpResponseMessage message)
        {
            return await message.Content.ReadAsStringAsync().ConfigureAwait(false);
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

        public static Option<T> FindFirst<T>(this IEnumerable<Option<T>> e)
        {
            return e.FirstOrDefault();
        }

        public static Option<T> FindFirst<T>(this IEnumerable<T> e)
        {
            return e.Select(Option.Of).FirstOrDefault();
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
