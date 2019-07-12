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
using System.Collections.Immutable;
using Jib.Net.Core.Api;

namespace com.google.cloud.tools.jib.configuration
{
    public class EventHandlers : IEventHandlers
    {
        public static readonly EventHandlers NONE = new Builder().Build();
        private ImmutableArray<Action<IJibEvent>> handlers;

        public EventHandlers(ImmutableArray<Action<IJibEvent>> handlers)
        {
            this.handlers = handlers;
        }

        public void Dispatch(IJibEvent @event)
        {
            foreach (var handler in handlers)
            {
                handler(@event);
            }
        }

        public static Builder CreateBuilder()
        {
            return new Builder();
        }

        public class Builder
        {
            private readonly IList<Action<IJibEvent>> handlers = new List<Action<IJibEvent>>();

            public Builder Add<T>(Action<T> action) where T : IJibEvent
            {
                handlers.Add(jibEvent =>
                {
                    if (jibEvent is T e)
                    {
                        action(e);
                    }
                });
                return this;
            }

            public EventHandlers Build()
            {
                return new EventHandlers(handlers.ToImmutableArray());
            }
        }
    }
}