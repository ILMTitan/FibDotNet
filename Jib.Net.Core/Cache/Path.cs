/*
 * Copyright 2018 Google LLC. All rights reserved.
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using com.google.cloud.tools.jib.api;
using Jib.Net.Core.Api;

namespace Jib.Net.Core.FileSystem
{
    public class SystemPath : IEnumerable<SystemPath>
    {
        private string file;

        public SystemPath(string file)
        {
            this.file = file;
        }

        internal object getRoot()
        {
            throw new NotImplementedException();
        }

        internal object getNameCount()
        {
            throw new NotImplementedException();
        }

        public IEnumerator<SystemPath> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        internal SystemPath resolve(SystemPath relativePath)
        {
            throw new NotImplementedException();
        }

        internal SystemPath relativize(SystemPath path)
        {
            throw new NotImplementedException();
        }

        internal SystemPath getParent()
        {
            throw new NotImplementedException();
        }

        internal FileInfo toFile()
        {
            throw new NotImplementedException();
        }

        internal string toString()
        {
            throw new NotImplementedException();
        }

        internal RelativeUnixPath getFileName()
        {
            throw new NotImplementedException();
        }

        internal AbsoluteUnixPath toAbsolutePath()
        {
            throw new NotImplementedException();
        }

        internal SystemPath resolve(string v)
        {
            throw new NotImplementedException();
        }
    }
}