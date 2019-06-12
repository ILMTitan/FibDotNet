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
using Jib.Net.Core;
using Jib.Net.Core.Global;
using NodaTime;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib
{














    /** Testing infrastructure for running code across multiple threads. */
    public class MultithreadedExecutor : IDisposable
    {

        private static readonly Duration MULTITHREADED_TEST_TIMEOUT = Duration.FromSeconds(1);

        public E invoke<E>(Callable<E> callable)
        {
            List<E> returnValue = invokeAll(Collections.singletonList(callable));
            return returnValue.get(0);
        }

        public void invoke(Action a)
        {
            invokeAll(Collections.singletonList(a));
        }
        public void invokeAll(IEnumerable<Action> callables)
        {
            List<Task> futures =
                        callables.Select(c => Task.Run(() => c())).ToList();

            foreach (Task future in futures)
            {
                Assert.IsTrue(future.isDone());
            }
        }

        public List<E> invokeAll<E>(IEnumerable<Callable<E>> callables)
        {
            List<Task<E>> futures =
                        callables.Select(c => Task.Run(() => c())).ToList();

            List<E> returnValues = new List<E>();
            foreach (Task<E> future in futures)
            {
                Assert.IsTrue(future.isDone());
                returnValues.add(future.get());
            }

            return returnValues;
        }

        public void Dispose()
        {
        }
    }
}
