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

namespace Fib.Net.Core
{
    /** Constants relating to the Fib project. */
    public static class ProjectInfo
    {
        /** Link to the GitHub repository. */
        public static readonly string GitHubUrl = "https://github.com/ILMTitan/FibDotNet";

        /** Link to file an issue against the GitHub repository. */
        public static readonly string GitHubNewIssueUrl = GitHubUrl + "/issues/new";

        /** The project version. May be {@code null} if the version cannot be determined. */
        public static readonly string VERSION = "0.0.1-alpha.1";
        internal const string TOOL_NAME = "fib";
    }
}