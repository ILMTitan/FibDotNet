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

namespace com.google.cloud.tools.jib.global
{
    internal static class Range
    {
        internal static Range<T> atLeast<T>(T v)
        {
            return new Range<T>(v, default, RangeType.ClosedLower);
        }

        internal static Range<T> closed<T>(T v1, T v2)
        {
            return new Range<T>(v1, v2, RangeType.Closed);
        }

        public enum RangeType
        {
            Closed,
            ClosedLower
        }
    }

    internal class Range<T>
    {
        private T lowerBound;
        private T upperBound;
        private Range.RangeType type;

        public Range(T lowerBound, T upperBound, Range.RangeType closed)
        {
            this.lowerBound = lowerBound;
            this.upperBound = upperBound;
            this.type = closed;
        }

        internal bool hasLowerBound()
        {
            switch (type)
            {
                case Range.RangeType.Closed:
                case Range.RangeType.ClosedLower:
                    return true;
                default:
                    throw new InvalidOperationException($"Unsupported Range Type {type:G}");
            }
        }

        internal T lowerEndpoint()
        {
            return this.lowerBound;
        }

        internal bool hasUpperBound()
        {
            switch (type)
            {
                case Range.RangeType.Closed:
                    return true;
                case Range.RangeType.ClosedLower:
                    return false;
                default:
                    throw new InvalidOperationException($"Unsupported Range Type {type:G}");
            }
        }

        internal T upperEndpoint()
        {
            return this.upperBound;
        }
    }
}