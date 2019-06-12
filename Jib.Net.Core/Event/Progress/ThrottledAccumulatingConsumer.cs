/*
 * Copyright 2019 Google LLC.
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

using Jib.Net.Core;
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using NodaTime;
using System;

namespace com.google.cloud.tools.jib.@event.progress {







/**
 * Wraps a {@code Consumer<Long>} so that multiple consume calls ({@link #accept}) within a short
 * period of time are merged into a single later call with the value accumulated up to that point.
 */
public class ThrottledAccumulatingConsumer : IDisposable {
        public static implicit operator Consumer<long>(ThrottledAccumulatingConsumer c)
        {
            return c.accept;
        }
  private readonly Consumer<long> consumer;

  /** Delay between each call to the underlying {@link #accept}. */
  private readonly Duration delayBetweenCallbacks;

  /** Last time the underlying {@link #accept} was called. */
  private Instant previousCallback;

  /** "Clock" that returns the current {@link Instant}. */
  private readonly Supplier<Instant> getNow;

  private long valueSoFar;

  /**
   * Wraps a consumer with the delay of 100 ms.
   *
   * @param callback {@link Consumer} callback to wrap
   */
  public ThrottledAccumulatingConsumer(Consumer<long> callback) : this(callback, Duration.FromMilliseconds(100), SystemClock.Instance.GetCurrentInstant) {
    
  }

  public ThrottledAccumulatingConsumer(
      Consumer<long> consumer, Duration delayBetweenCallbacks, Supplier<Instant> getNow) {
    this.consumer = consumer;
    this.delayBetweenCallbacks = delayBetweenCallbacks;
    this.getNow = getNow;

    previousCallback = getNow.get();
  }

  public void accept(long value) {
            valueSoFar += value;

    Instant now = getNow.get();
    Instant nextFireTime = previousCallback.plus(delayBetweenCallbacks);
    if (now.isAfter(nextFireTime)) {
      consumer.accept(valueSoFar);
      previousCallback = now;
      valueSoFar = 0;
    }
  }

  public void Dispose() {
      consumer.accept(valueSoFar);
  }
}
}
