// Copyright 2018 Google LLC.
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Fib.Net.Core.Api
{
    /** Represents a port number with a protocol (TCP or UDP). */
    public sealed class Port
    {
        private const string TCP_PROTOCOL = "tcp";
        private const string UDP_PROTOCOL = "udp";

        /**
         * Pattern used for parsing information out of exposed port configurations.
         *
         * <p>Example matches: 100, 200-210, 1000/tcp, 2000/udp, 500-600/tcp
         */
        private static readonly Regex PortPattern = new Regex("^(\\d+)(?:-(\\d+))?(?:/(tcp|udp))?$");

        /**
         * Create a new {@link Port} with TCP protocol.
         *
         * @param port the port number
         * @return the new {@link Port}
         */
        public static Port Tcp(int port)
        {
            return new Port(port, TCP_PROTOCOL);
        }

        /**
         * Create a new {@link Port} with UDP protocol.
         *
         * @param port the port number
         * @return the new {@link Port}
         */
        public static Port Udp(int port)
        {
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
        public static Port ParseProtocol(int port, string protocolString)
        {
            string protocol = UDP_PROTOCOL.Equals(protocolString, StringComparison.OrdinalIgnoreCase) ? UDP_PROTOCOL : TCP_PROTOCOL;
            return new Port(port, protocol);
        }

        private readonly int port;
        private readonly string protocol;

        private Port(int port, string protocol)
        {
            this.port = port;
            this.protocol = protocol;
        }

        /**
         * Gets the port number.
         *
         * @return the port number
         */
        public int GetPort()
        {
            return port;
        }

        /**
         * Gets the protocol.
         *
         * @return the protocol
         */
        public string GetProtocol()
        {
            return protocol;
        }

        public override bool Equals(object other)
        {
            if (other == this)
            {
                return true;
            }
            if (!(other is Port otherPort))
            {
                return false;
            }
            return port == otherPort.port && protocol == otherPort.protocol;
        }

        public override int GetHashCode()
        {
            return Objects.Hash(port, protocol);
        }

        /**
         * Stringifies the port with protocol, in the form {@code <port>/<protocol>}. For example: {@code
         * 1337/TCP}.
         *
         * @return the string form of the port with protocol
         */

        public override string ToString()
        {
            return port + "/" + protocol;
        }

        /**
         * Converts/validates a list of strings representing port ranges to an expanded list of {@link
         * Port}s.
         *
         * <p>For example: ["1000", "2000-2002"] will expand to a list of {@link Port}s with the port
         * numbers [1000, 2000, 2001, 2002]
         *
         * @param ports the list of port numbers/ranges, with an optional protocol separated by a '/'
         *     (defaults to TCP if missing).
         * @return the ports as a list of {@link Port}
         * @throws NumberFormatException if any of the ports are in an invalid format or out of range
         */
        public static ImmutableHashSet<Port> Parse(IEnumerable<string> ports)
        {
            ports = ports ?? throw new ArgumentNullException(nameof(ports));
            ImmutableHashSet<Port>.Builder result = ImmutableHashSet.CreateBuilder<Port>();

            foreach (string port in ports)

            {
                Match matcher = PortPattern.Match(port);

                if (!matcher.Success)
                {
                    throw new FormatException(
                        "Invalid port configuration: '"
                            + port
                            + "'. Make sure the port is a single number or a range of two numbers separated "
                            + "with a '-', with or without protocol specified (e.g. '<portNum>/tcp' or "
                            + "'<portNum>/udp').");
                }

                // Parse protocol
                int min = int.Parse(matcher.Groups[1].Value, CultureInfo.InvariantCulture);
                int max = min;
                if (!string.IsNullOrEmpty(matcher.Groups[2].Value))
                {
                    max = int.Parse(matcher.Groups[2].Value, CultureInfo.InvariantCulture);
                }
                string protocol = matcher.Groups[3].Value;

                // Error if configured as 'max-min' instead of 'min-max'
                if (min > max)
                {
                    throw new FormatException(
                        "Invalid port range '" + port + "'; smaller number must come first.");
                }

                // Warn for possibly invalid port numbers
                if (min < 1 || max > 65535)
                {
                    throw new FormatException(
                        "Port number '" + port + "' is out of usual range (1-65535).");
                }

                for (int portNumber = min; portNumber <= max; portNumber++)
                {
                    result.Add(Port.ParseProtocol(portNumber, protocol));
                }
            }

            return result.ToImmutable();
        }
    }
}
