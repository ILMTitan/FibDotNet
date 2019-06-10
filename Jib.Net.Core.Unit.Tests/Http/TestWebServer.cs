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

namespace com.google.cloud.tools.jib.http {






















/** Simple local web server for testing. */
public class TestWebServer : IDisposable {

  private readonly bool https;
  private readonly ServerSocket serverSocket;
  private readonly ExecutorService executorService = Executors.newSingleThreadExecutor();
  private readonly Semaphore threadStarted = new Semaphore(0);
  private readonly StringBuilder inputRead = new StringBuilder();

  public TestWebServer(bool https)
      {
    this.https = https;
    serverSocket = createServerSocket(https);
    ignoreReturn(executorService.submit(this.serve200));
    threadStarted.acquire();
  }

  public string getEndpoint() {
    string host = serverSocket.getInetAddress().getHostAddress();
    return (https ? "https" : "http") + "://" + host + ":" + serverSocket.getLocalPort();
  }

  public void Dispose() {
    serverSocket.close();
    executorService.shutdown();
  }

  private ServerSocket createServerSocket(bool https)
      {
    if (https) {
      KeyStore keyStore = KeyStore.getInstance("JKS");
      // generated with: keytool -genkey -keyalg RSA -keystore ./TestWebServer-keystore
      SystemPath keyStoreFile = Paths.get(Resources.getResource("core/TestWebServer-keystore").toURI());
      using (Stream in = Files.newInputStream(keyStoreFile)) {
        keyStore.load(in, "password".toCharArray());
      }

      KeyManagerFactory keyManagerFactory =
          KeyManagerFactory.getInstance(KeyManagerFactory.getDefaultAlgorithm());
      keyManagerFactory.init(keyStore, "password".toCharArray());

      SSLContext sslContext = SSLContext.getInstance("TLS");
      sslContext.init(keyManagerFactory.getKeyManagers(), null, null);
      return sslContext.getServerSocketFactory().createServerSocket(0);
    } else {
      return new ServerSocket(0);
    }
  }

  private Void serve200() {
    threadStarted.release();
    using (Socket socket = serverSocket.accept()) {

      Stream in = socket.getInputStream();
      BufferedReader reader = new BufferedReader(new StreamReader(in, StandardCharsets.UTF_8));
      for (string line = reader.readLine();
          line != null && !line.isEmpty(); // An empty line marks the end of an HTTP request.
          line = reader.readLine()) {
        inputRead.append(line + "\n");
      }

      string response = "HTTP/1.1 200 OK\nContent-Length:12\n\nHello World!";
      socket.getOutputStream().write(response.getBytes(StandardCharsets.UTF_8));
      socket.getOutputStream().flush();
    }
    return null;
  }

  private void ignoreReturn(Future<Void> future) {
    // do nothing; to make Error Prone happy
  }

  public string getInputRead() {
    return inputRead.toString();
  }
}
}
