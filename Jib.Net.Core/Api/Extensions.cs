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
        public static IDictionary<TKey, TValue> asMap<TKey, TValue>(this IDictionary<TKey, TValue> d)
        {
            return d;
        }

        public static int lastIndexOf(this string s, char c)
        {
            return s.LastIndexOf(c);
        }

        public static int lastIndexOf(this string s, string substring)
        {
            return s.LastIndexOf(substring, StringComparison.Ordinal);
        }

        public static void setLocation(this HttpResponseHeaders h, string location)
        {
            h.Location = new Uri(location);
        }

        public static async Task<string> parseAsStringAsync(this HttpResponseMessage m)
        {
            return await m.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public static async Task writeToAsync(this BlobHttpContent c, Stream s)
        {
            await c.CopyToAsync(s).ConfigureAwait(false);
        }

        public static ErrorCode name(this ErrorCode e)
        {
            return e;
        }

        public static bool isDone(this Task t)
        {
            return t.IsCompleted;
        }

        public static bool containsKey<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key)
        {
            return d.ContainsKey(key);
        }

        public static int intValue(this int? i)
        {
            return i.GetValueOrDefault();
        }

        public static long longValue(this long? l)
        {
            return l.GetValueOrDefault();
        }

        public static long longValue(this long l)
        {
            return l;
        }

        public static bool isEmpty<T>(this Queue<T> queue)
        {
            return queue.Count == 0;
        }

        public static bool offer<T>(this Queue<T> queue, T item)
        {
            queue.Enqueue(item);
            return true;
        }

        public static DateTime getLastModifiedDate(this TarEntry e)
        {
            return e.TarHeader.ModTime;
        }

        public static DateTime getModTime(this TarEntry e)
        {
            return DateTime.SpecifyKind(e.ModTime, DateTimeKind.Utc);
        }

        public static bool isDirectory(this TarEntry e)
        {
            return e.IsDirectory;
        }

        public static char[] toCharArray(this string s)
        {
            return s.ToCharArray();
        }

        public static int release(this SemaphoreSlim s)
        {
            return s.Release();
        }

        public static void acquire(this SemaphoreSlim s)
        {
            s.Wait();
        }

        public static void write(this Stream s, byte[] buffer, int offset, int count)
        {
            s.Write(buffer, offset, count);
        }

        public static void write(this Stream s, byte b)
        {
            s.WriteByte(b);
        }

        public static T remove<T>(this Queue<T> queue)
        {
            return queue.Dequeue();
        }

        public static void add<T>(this Queue<T> queue, T value)
        {
            queue.Enqueue(value);
        }

        public static string getPath(this FileSystemInfo info)
        {
            return info.FullName;
        }

        public static TarEntry getNextEntry(this TarInputStream i)
        {
            return i.GetNextEntry();
        }

        public static TarEntry getNextTarEntry(this TarInputStream i)
        {
            return i.GetNextEntry();
        }

        public static Instant plusSeconds(this Instant i, int seconds)
        {
            return i + Duration.FromSeconds(seconds);
        }

        public static T poll<T>(this Queue<T> q)
        {
            return q.Dequeue();
        }

        public static Instant plusNanos(this Instant i, int nanos)
        {
            return i.PlusNanoseconds(nanos);
        }

        public static Instant plusMillis(this Instant i, int mills)
        {
            return i + Duration.FromMilliseconds(mills);
        }

        public static IList<T> asList<T>(this IEnumerable<T> e)
        {
            return e.ToList();
        }

        public static int groupCount(this Match m)
        {
            return m.Groups.Count;
        }

        public static TValue getOrDefault<TKey, TValue>(this ImmutableDictionary<TKey, TValue> dic, TKey key, TValue defaultValue)
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

        public static bool contains<T>(this ISet<T> set, T value)
        {
            return set.Contains(value);
        }

        public static void run(this Action a)
        {
            a();
        }

        public static Uri getRequestUrl(this HttpResponseMessage message)
        {
            return message.RequestMessage.RequestUri;
        }

        public static int size<T>(this ImmutableArray<T> l)
        {
            return l.Length;
        }

        public static void setSize(this TarEntry e, long size)
        {
            e.TarHeader.Size = size;
        }

        public static SystemPath toPath(this FileSystemInfo fileInfo)
        {
            return new SystemPath(fileInfo);
        }

        public static FileInfo getFile(this TarEntry e)
        {
            return new FileInfo(e.File);
        }

        public static bool isFile(this TarEntry e)
        {
            return e.TarHeader.TypeFlag != TarHeader.LF_DIR;
        }

        public static void closeArchiveEntry(this TarOutputStream s)
        {
            s.CloseEntry();
        }

        public static void putArchiveEntry(this TarOutputStream s, TarEntry e)
        {
            s.PutNextEntry(e);
        }

        public static Uri getLocation(this HttpResponseHeaders h)
        {
            return h.Location;
        }

        public static Uri toURL(this UriBuilder b)
        {
            return b.Uri;
        }

        public static void setScheme(this UriBuilder b, string scheme)
        {
            b.Scheme = scheme;
        }

        public static int getPort(this Uri uri)
        {
            return uri.Port;
        }

        public static string toLowerCase(this string s, CultureInfo ci)
        {
            return s.ToLower(ci);
        }

        public static void setUserAgent(this HttpRequestHeaders h, string input)
        {
            h.UserAgent.Clear();
            h.UserAgent.Add(ProductInfoHeaderValue.Parse(input));
        }

        public static void setAuthorization(this HttpRequestHeaders h, string input)
        {
            h.Authorization = AuthenticationHeaderValue.Parse(input);
        }

        public static void setAccept(this HttpRequestHeaders h, string input)
        {
            h.Accept.Clear();
            h.Accept.ParseAdd(input);
        }

        public static async Task<Stream> getBodyAsync(this HttpResponseMessage m)
        {
            return await m.Content.ReadAsStreamAsync().ConfigureAwait(false);
        }

        public static IList<string> getHeader(this HttpResponseMessage m, string name)
        {
            return m.Headers.GetValues(name).ToList();
        }

        public static T get<T>(this ImmutableArray<T> array, int index)
        {
            return array[index];
        }

        public static T get<T>(this IReadOnlyList<T> c, int index)
        {
            return c[index];
        }

        public static T get<T>(this List<T> c, int index)
        {
            return c[index];
        }

        public static int size<T>(this ImmutableHashSet<T> c)
        {
            return c.Count;
        }

        public static int size<TKey, TValue>(this ImmutableDictionary<TKey, TValue> d)
        {
            return d.Count;
        }

        public static int size<T>(this IReadOnlyCollection<T> c)
        {
            return c.Count;
        }

        public static void close(this IDisposable d)
        {
            d.Dispose();
        }

        public static string replace(this string s, char oldValue, char newValue)
        {
            return s.Replace(oldValue, newValue);
        }

        public static string replace(this string s, string oldValue, string newValue)
        {
            return s.Replace(oldValue, newValue);
        }

        public static byte[] toByteArray(this MemoryStream s)
        {
            return s.ToArray();
        }

        public static void setGroupName(this TarEntry e, string name)
        {
            e.TarHeader.GroupName = name;
        }

        public static void setUserName(this TarEntry e, string name)
        {
            e.TarHeader.UserName = name;
        }

        public static void setUserId(this TarEntry e, int userId)
        {
            e.TarHeader.UserId = userId;
        }

        public static void setGroupId(this TarEntry e, int groupId)
        {
            e.TarHeader.GroupId = groupId;
        }

        public static void setMode(this TarEntry e, PosixFilePermissions mode)
        {
            e.TarHeader.Mode = (int)mode;
        }

        public static PosixFilePermissions getMode(this TarEntry e)
        {
            return (PosixFilePermissions)e.TarHeader.Mode;
        }

        public static void sort<T>(this List<T> l, IComparer<T> o)
        {
            l.Sort(o);
        }

        public static void setModTime(this TarEntry e, long mills)
        {
            e.ModTime = DateTimeOffset.FromUnixTimeMilliseconds(mills).DateTime;
        }

        public static long toEpochMilli(this Instant i)
        {
            return i.ToUnixTimeMilliseconds();
        }

        public static string getName(this TarEntry e)
        {
            return e.Name;
        }

        public static bool isEmpty<T>(this ImmutableArray<T> c)
        {
            return c.Length == 0;
        }

        public static bool isEmpty<T>(this ICollection<T> c)
        {
            return c.Count == 0;
        }

        public static long toNanos(this Duration d)
        {
            return (long)d.TotalNanoseconds;
        }

        public static TResult collect<T, TResult>(this IEnumerable<T> e, Func<IEnumerable<T>, TResult> f)
        {
            return f(e);
        }

        public static bool noneMatch<T>(this IEnumerable<T> e, Func<T, bool> predicate)
        {
            return !e.Any(predicate);
        }

        public static async Task<string> getContentAsync(this HttpResponseMessage message)
        {
            return await message.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public static long? getContentLength(this HttpResponseMessage message)
        {
            return message.Content.Headers.ContentLength;
        }

        public static bool find(this Match m)
        {
            return m.Success;
        }

        public static HttpHeaderValueCollection<AuthenticationHeaderValue> getAuthenticate(this HttpResponseHeaders headers)
        {
            return headers.WwwAuthenticate;
        }

        public static HttpResponseHeaders getHeaders(this HttpResponseMessage message)
        {
            return message.Headers;
        }

        public static HttpStatusCode getStatusCode(this HttpResponseMessage message)
        {
            return message.StatusCode;
        }

        public static bool contains(this string s, string value)
        {
            return s.Contains(value);
        }

        public static void write(this Stream s, byte[] bytes)
        {
            s.Write(bytes, 0, bytes.Length);
        }

        public static int indexOf(this string s, string substring)
        {
            return s.IndexOf(substring, StringComparison.Ordinal);
        }

        public static int indexOf(this string s, char c)
        {
            return s.IndexOf(c);
        }

        public static string substring(this string s, int startIndex, int length)
        {
            return s.Substring(startIndex, length);
        }

        public static StringBuilder append(this StringBuilder b, object value)
        {
            return b.Append(value);
        }

        public static string getProtocol(this Uri uri)
        {
            return uri.Scheme;
        }

        public static bool startsWith(this string s, string prefix)
        {
            return s.StartsWith(prefix, StringComparison.Ordinal);
        }

        public static bool equals(this object o, object other)
        {
            return o.Equals(other);
        }

        public static TValue getValue<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp)
        {
            return kvp.Value;
        }

        public static TKey getKey<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp)
        {
            return kvp.Key;
        }

        public static bool test<T>(this Predicate<T> p, T value)
        {
            return p(value);
        }

        public static Option<T> findFirst<T>(this IEnumerable<Option<T>> e)
        {
            return e.FirstOrDefault();
        }

        public static Option<T> findFirst<T>(this IEnumerable<T> e)
        {
            return e.Select(Option.Of).FirstOrDefault();
        }

        public static IDictionary<TKey, TValue> entrySet<TKey, TValue>(this IDictionary<TKey, TValue> d)
        {
            return d;
        }

        public static bool endsWith(this string s, string suffix)
        {
            return s.EndsWith(suffix, StringComparison.Ordinal);
        }

        public static string trim(this string s)
        {
            return s.Trim();
        }

        public static bool isEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }

        public static Func<T, bool> and<T>(this Func<T, bool> first, Func<T, bool> second)
        {
            return i => first(i) && second(i);
        }

        public static bool isAfter(this Instant i, Instant other)
        {
            return i > other;
        }

        public static Instant plus(this Instant start, Duration end)
        {
            return start + end;
        }

        public static void remove<T>(this ICollection<T> c, T item)
        {
            c.Remove(item);
        }

        public static TValue get<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key)
        {
            if (!d.ContainsKey(key))
            {
                return default;
            }
            return d[key];
        }

        public static IEnumerable<T> stream<T>(this IEnumerable<T> e)
        {
            return e;
        }

        public static IEnumerable<T> filter<T>(this IEnumerable<T> e, Func<T, bool> predicate)
        {
            return e.Where(predicate);
        }

        public static IOrderedEnumerable<T> sorted<T>(this IEnumerable<T> e)
        {
            return e.OrderBy(i => i);
        }

        public static IEnumerable<TOut> map<TIn, TOut>(this IEnumerable<TIn> e, Func<TIn, TOut> f)
        {
            return e.Select(f);
        }

        public static void accept<T1, T2>(this Action<T1, T2> a, T1 arg1, T2 arg2)
        {
            a(arg1, arg2);
        }

        public static TResult apply<T, TResult>(this Func<T, TResult> f, T input)
        {
            return f(input);
        }

        public static void put<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key, TValue value)
        {
            d[key]= value;
        }

        public static void putAll<TKey, TValue>(this IDictionary<TKey, TValue> d, IEnumerable<KeyValuePair<TKey, TValue>> entries)
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

        public static ICollection<TValue> values<TKey, TValue>(this IDictionary<TKey, TValue> d)
        {
            return d.Values;
        }

        public static ICollection<TKey> keySet<TKey, TValues>(this IDictionary<TKey, TValues> d)
        {
            return d.Keys;
        }

        public static T remove<T>(this IList<T> c, int i)
        {
            var temp = c[i];
            c.RemoveAt(i);
            return temp;
        }

        public static T get<T>(this IList<T> l, int i)
        {
            return l[i];
        }

        public static int compareTo<T>(this IComparable<T> comparable, T other)
        {
            return comparable.CompareTo(other);
        }

        public static Instant toInstant(this DateTime d)
        {
            return Instant.FromDateTimeUtc(d);
        }

        public static string toString(this object o)
        {
            return o.ToString();
        }

        public static Instant instant(this IClock c)
        {
            return c.GetCurrentInstant();
        }

        public static void add<T>(this ICollection<T> set, T value)
        {
            set.Add(value);
        }

        public static TCollection add<TCollection, T>(this TCollection set, params T[] values) where TCollection : ICollection<T>
        {
            foreach (T v in values)
            {
                set.Add(v);
            }
            return set;
        }

        public static void addAll<T>(this ISet<T> set, IEnumerable<T> values)
        {
            set.UnionWith(values);
        }

        public static bool contains<T>(this ICollection<T> e, T item)
        {
            return e.Contains(item);
        }

        public static int size<T>(this ICollection<T> c)
        {
            return c.Count;
        }

        public static bool matches(this string s, string regex)
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

        public static string substring(this string s, int l)
        {
            return s.Substring(l);
        }

        public static int length(this string s)
        {
            return s.Length;
        }

        public static int hashCode(this object o)
        {
            return o.GetHashCode();
        }

        public static string getMessage(this Exception e)
        {
            return e.Message;
        }

        public static Match matcher(this Regex regex, string s)
        {
            return regex.Match(s);
        }

        public static bool matches(this Match match)
        {
            return match.Success;
        }

        public static string group(this Match match, int i)
        {
            return match.Groups[i].Value;
        }

        public static string group(this Match match, string i)
        {
            return match.Groups[i].Value;
        }

        public static ImmutableHashSet<T> build<T>(this ImmutableHashSet<T>.Builder b)
        {
            return b.ToImmutable();
        }

        public static bool equalsIgnoreCase(this string s1, string s2)
        {
            return s1.Equals(s2, StringComparison.OrdinalIgnoreCase);
        }

        public static void add<T>(this ImmutableArray<T>.Builder b, T value)
        {
            b.Add(value);
        }

        public static TResult apply<T1, T2, TResult>(this Func<T1, T2, TResult> f, T1 value1, T2 value2)
        {
            return f(value1, value2);
        }

        public static ImmutableArray<T> build<T>(this ImmutableArray<T>.Builder b)
        {
            return b.ToImmutable();
        }

        public static char charAt(this string s, int i)
        {
            return s[i];
        }

        public static Exception getCause(this Exception e)
        {
            return e.InnerException;
        }

        public static bool isEmpty<T>(this IImmutableList<T> l)
        {
            return l.Count == 0;
        }

        public static ImmutableDictionary<TKey, TValue>.Builder put<TKey, TValue>(this ImmutableDictionary<TKey, TValue>.Builder b, TKey key, TValue value)
        {
            b.Add(key, value);
            return b;
        }

        public static ImmutableDictionary<TKey, TValue> build<TKey, TValue>(this ImmutableDictionary<TKey, TValue>.Builder b)
        {
            return b.ToImmutable();
        }

        public static TValue get<TKey, TValue>(this ImmutableDictionary<TKey, TValue> d, TKey key)
        {
            return d[key];
        }

        public static void add<T>(this IList<T> l, T value)
        {
            l.Add(value);
        }

        public static void addAll<T>(this IList<T> l, IEnumerable<T> values)
        {
            foreach (T value in values)
            {
                l.Add(value);
            }
        }

        public static void forEach<T>(this IEnumerable<T> l, Action<T> a)
        {
            foreach (T i in l)
            {
                a(i);
            }
        }

        public static void forEach<T, TResult>(this IEnumerable<T> l, Func<T, TResult> a)
        {
            foreach (T i in l)
            {
                a(i);
            }
        }

        public static byte[] getBytes(this string s, Encoding encoding)
        {
            return encoding.GetBytes(s);
        }
    }
}
