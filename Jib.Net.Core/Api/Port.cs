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

using Jib.Net.Core.Global;

namespace com.google.cloud.tools.jib.api {


/** Represents a port number with a protocol (TCP or UDP). */
public class Port {

  private static readonly string TCP_PROTOCOL = "tcp";
  private static readonly string UDP_PROTOCOL = "udp";

  /**
   * Create a new {@link Port} with TCP protocol.
   *
   * @param port the port number
   * @return the new {@link Port}
   */
  public static Port tcp(int port) {
    return new Port(port, TCP_PROTOCOL);
  }

  /**
   * Create a new {@link Port} with UDP protocol.
   *
   * @param port the port number
   * @return the new {@link Port}
   */
  public static Port udp(int port) {
    return new Port(port, UDP_PROTOCOL);
  }

  /**
   * Gets a {@link Port} with protocol parsed from the string form {@code protocolString}. Unknown
   * protocols will default to TCP.
   *
   * @param port the port number
   * @param protocolString the case insensitive string (e.g. "tcp", "udp")
   * @return the {@link Port}
   */
  public static Port parseProtocol(int port, string protocolString) {
    string protocol = UDP_PROTOCOL.equalsIgnoreCase(protocolString) ? UDP_PROTOCOL : TCP_PROTOCOL;
    return new Port(port, protocol);
  }

  private readonly int port;
  private readonly string protocol;

  private Port(int port, string protocol) {
    this.port = port;
    this.protocol = protocol;
  }

  /**
   * Gets the port number.
   *
   * @return the port number
   */
  public int getPort() {
    return port;
  }

  /**
   * Gets the protocol.
   *
   * @return the protocol
   */
  public string getProtocol() {
    return protocol;
  }

  public override bool Equals(object other) {
    if (other == this) {
      return true;
    }
    if (!(other is Port)) {
      return false;
    }
    Port otherPort = (Port) other;
    return port == otherPort.port && protocol.Equals(otherPort.protocol);
  }

  public override int GetHashCode() {
    return Objects.hash(port, protocol);
  }

  /**
   * Stringifies the port with protocol, in the form {@code <port>/<protocol>}. For example: {@code
   * 1337/TCP}.
   *
   * @return the string form of the port with protocol
   */

  public override string ToString() {
    return port + "/" + protocol;
  }
}
}
