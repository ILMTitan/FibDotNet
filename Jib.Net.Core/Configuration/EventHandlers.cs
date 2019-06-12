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
using com.google.cloud.tools.jib.api;
using Jib.Net.Core.Api;

namespace com.google.cloud.tools.jib.configuration
{
    public class EventHandlers
    {
        public static EventHandlers NONE { get; internal set; }

        public void dispatch(JibEvent @event)
        {
            throw new NotImplementedException();
        }

        public static Builder builder()
        {
            throw new NotImplementedException();
        }

        public class Builder
        {
            private IList<Action<JibEvent>> handlers = new List<Action<JibEvent>>();

            public Builder add<T>(Action<T> action) where T : JibEvent
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

            public Builder add<T>(Type _, Action<T> action) where T : JibEvent
            {
                return add<T>(action);
            }

            public EventHandlers build()
            {
                throw new NotImplementedException();
            }
        }

        internal object getHandlers()
        {
            throw new NotImplementedException();
        }
    }
}