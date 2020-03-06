// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Immutable;
using Fib.Net.Core.Api;
using Fib.Net.Core.Caching;
using Fib.Net.Core.Registry;

namespace Fib.Net.Core.Configuration
{
    public interface IBuildConfiguration
    {
        bool GetAllowInsecureRegistries();
        ImmutableHashSet<string> GetAllTargetImageTags();
        LayersCache GetApplicationLayersCache();
        ImageConfiguration GetBaseImageConfiguration();
        LayersCache GetBaseImageLayersCache();
        IContainerConfiguration GetContainerConfiguration();
        IEventHandlers GetEventHandlers();
        ImmutableArray<ILayerConfiguration> GetLayerConfigurations();
        ManifestFormat GetTargetFormat();
        ImageConfiguration GetTargetImageConfiguration();
        string GetToolName();
        bool IsOffline();
        RegistryClient.Factory NewBaseImageRegistryClientFactory();
        RegistryClient.Factory NewTargetImageRegistryClientFactory();
        string GetToolVersion();
    }
}