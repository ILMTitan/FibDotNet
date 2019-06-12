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

namespace com.google.cloud.tools.jib.api
{
    /** Log message event. */
    public sealed class LogEvent : JibEvent
    {
        /** Log levels, in order of verbosity. */
        public enum Level
        {

            /** Something went wrong. */
            ERROR,

            /** Something might not work as intended. */
            WARN,

            /** Default. */
            LIFECYCLE,

            /** Same as {@link #LIFECYCLE}, except represents progress updates. */
            PROGRESS,

            /**
             * Details that can be ignored.
             *
             * <p>Use {@link #LIFECYCLE} for progress-indicating messages.
             */
            INFO,

            /** Useful for debugging. */
            DEBUG
        }

        public static LogEvent error(string message)
        {
            return new LogEvent(Level.ERROR, message);
        }

        public static LogEvent lifecycle(string message)
        {
            return new LogEvent(Level.LIFECYCLE, message);
        }

        public static LogEvent progress(string message)
        {
            return new LogEvent(Level.PROGRESS, message);
        }

        public static LogEvent warn(string message)
        {
            return new LogEvent(Level.WARN, message);
        }

        public static LogEvent info(string message)
        {
            return new LogEvent(Level.INFO, message);
        }

        public static LogEvent debug(string message)
        {
            return new LogEvent(Level.DEBUG, message);
        }

        private readonly Level level;
        private readonly string message;

        private LogEvent(Level level, string message)
        {
            this.level = level;
            this.message = message;
        }

        /**
         * Gets the log level to log at.
         *
         * @return the log level
         */
        public Level getLevel()
        {
            return level;
        }

        /**
         * Gets the log message.
         *
         * @return the log message
         */
        public string getMessage()
        {
            return message;
        }

        public override bool Equals(object other)
        {
            if (other == this)
            {
                return true;
            }
            if (!(other is LogEvent))
            {
                return false;
            }

            LogEvent otherLogEvent = (LogEvent)other;
            return level == otherLogEvent.level && message.Equals(otherLogEvent.message);
        }

        public override int GetHashCode()
        {
            return Objects.hash(level, message);
        }

        public override string ToString()
        {
            return "LogEvent [level=" + level + ", message=" + message + "]";
        }
    }
}
