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
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

namespace com.google.cloud.tools.jib.api
{
    /** Utility for parsing Docker/OCI ports from text representations. */
    public sealed class Ports
    {
        /**
         * Pattern used for parsing information out of exposed port configurations.
         *
         * <p>Example matches: 100, 200-210, 1000/tcp, 2000/udp, 500-600/tcp
         */
        private static readonly Regex portPattern = new Regex("^(\\d+)(?:-(\\d+))?(?:/(tcp|udp))?$");

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
        public static ImmutableHashSet<Port> parse(IList<string> ports)
        {
            ImmutableHashSet<Port>.Builder result = ImmutableHashSet.CreateBuilder<Port>();

            foreach (string port in ports)

            {
                Match matcher = portPattern.matcher(port);

                if (!matcher.matches())
                {
                    throw new FormatException(
                        "Invalid port configuration: '"
                            + port
                            + "'. Make sure the port is a single number or a range of two numbers separated "
                            + "with a '-', with or without protocol specified (e.g. '<portNum>/tcp' or "
                            + "'<portNum>/udp').");
                }

                // Parse protocol
                int min = int.Parse(matcher.group(1), CultureInfo.InvariantCulture);
                int max = min;
                if (!Strings.isNullOrEmpty(matcher.group(2)))
                {
                    max = int.Parse(matcher.group(2), CultureInfo.InvariantCulture);
                }
                string protocol = matcher.group(3);

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
                    result.add(Port.parseProtocol(portNumber, protocol));
                }
            }

            return result.build();
        }

        private Ports() { }
    }
}
