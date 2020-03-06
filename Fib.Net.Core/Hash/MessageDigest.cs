// Copyright 2017 Google LLC.
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
using System.Security.Cryptography;

namespace Fib.Net.Core.Hash
{
    public class MessageDigest
    {
        private readonly HashAlgorithm hashAlgorithm;
        private readonly Lazy<byte[]> hashLazy;

        public MessageDigest(HashAlgorithm hashAlgorithm)
        {
            this.hashAlgorithm = hashAlgorithm;
            hashLazy = new Lazy<byte[]>(ComputeHash);
        }

        internal static MessageDigest GetInstance(string algorithmName)
        {
            switch (algorithmName)
            {
                case CountingDigestOutputStream.Sha256Algorithm:
                    return new MessageDigest(SHA256.Create());
                default:
                    throw new ArgumentException($"unknown name {algorithmName}", nameof(algorithmName));
            }
        }

        internal byte[] Digest()
        {
            return hashLazy.Value;
        }

        private byte[] ComputeHash()
        {
            hashAlgorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return hashAlgorithm.Hash;
        }

        internal int TransformBlock(
            byte[] inputBuffer,
            int inputOffset,
            int inputCount,
            byte[] outputBuffer,
            int outputOffset)
        {
            return hashAlgorithm.TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
        }
    }
}