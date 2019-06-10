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

namespace com.google.cloud.tools.jib {














/** Testing infrastructure for running code across multiple threads. */
public class MultithreadedExecutor : IDisposable {

  private static readonly Duration MULTITHREADED_TEST_TIMEOUT = Duration.ofSeconds(1);
  private static readonly int THREAD_COUNT = 20;

  private readonly ExecutorService executorService = Executors.newFixedThreadPool(THREAD_COUNT);

  public E invoke<E>(Callable<E> callable) {
    List<E> returnValue = invokeAll(Collections.singletonList(callable));
    return returnValue.get(0);
  }

  public List<E> invokeAll<E>(List<Callable<E>> callables) {
    List<Future<E>> futures =
        executorService.invokeAll(
            callables, MULTITHREADED_TEST_TIMEOUT.getSeconds(), TimeUnit.SECONDS);

    IList<E> returnValues = new List<>();
    foreach (Future<E> future in futures)
    {
      Assert.assertTrue(future.isDone());
      returnValues.add(future.get());
    }

    return returnValues;
  }

  public void Dispose() {
    executorService.shutdown();
    try {
      executorService.awaitTermination(MULTITHREADED_TEST_TIMEOUT.getSeconds(), TimeUnit.SECONDS);

    } catch (OperationCanceledException ex) {
      throw new IOException(ex);
    }
  }
}
}
