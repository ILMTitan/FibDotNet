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

using Fib.Net.Core.Api;

namespace Fib.Net.Core.Events
{
    /** Log message event. */
    public sealed class LogEvent : IFibEvent
    {
        /** Log levels, in order of verbosity. */
        public enum Level
        {

            /** Something went wrong. */
            Error,

            /** Something might not work as intended. */
            Warn,

            /** Default. */
            Lifecycle,

            /** Same as {@link #LIFECYCLE}, except represents progress updates. */
            Progress,

            /**
             * Details that can be ignored.
             *
             * <p>Use {@link #LIFECYCLE} for progress-indicating messages.
             */
            Info,

            /** Useful for debugging. */
            Debug
        }

        public static LogEvent Error(string message)
        {
            return new LogEvent(Level.Error, message);
        }

        public static LogEvent Lifecycle(string message)
        {
            return new LogEvent(Level.Lifecycle, message);
        }

        public static LogEvent Progress(string message)
        {
            return new LogEvent(Level.Progress, message);
        }

        public static LogEvent Warn(string message)
        {
            return new LogEvent(Level.Warn, message);
        }

        public static LogEvent Info(string message)
        {
            return new LogEvent(Level.Info, message);
        }

        public static LogEvent Debug(string message)
        {
            return new LogEvent(Level.Debug, message);
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
        public Level GetLevel()
        {
            return level;
        }

        /**
         * Gets the log message.
         *
         * @return the log message
         */
        public string GetMessage()
        {
            return message;
        }

        public override bool Equals(object other)
        {
            if (other == this)
            {
                return true;
            }
            if (!(other is LogEvent logEvent))
            {
                return false;
            }
            return Equals(level, logEvent.level) && Equals(message, logEvent.message);
        }

        public override int GetHashCode()
        {
            return Objects.Hash(level, message);
        }

        public override string ToString()
        {
            return $"LogEvent:{level}:{message}";
        }
    }
}
