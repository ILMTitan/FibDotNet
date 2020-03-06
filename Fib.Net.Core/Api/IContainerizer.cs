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

using System;
using System.Collections.Generic;
using Fib.Net.Core.BuildSteps;
using Fib.Net.Core.Configuration;

namespace Fib.Net.Core.Api
{
    public interface IContainerizer
    {
        event Action<IFibEvent> FibEvents;

        IContainerizer AddEventHandler<T>(Action<T> eventConsumer) where T : IFibEvent;
        EventHandlers BuildEventHandlers();
        IStepsRunner CreateStepsRunner(BuildConfiguration buildConfiguration);
        void Dispose();
        ISet<string> GetAdditionalTags();
        bool GetAllowInsecureRegistries();
        string GetApplicationLayersCacheDirectory();
        string GetBaseImageLayersCacheDirectory();
        string GetDescription();
        ImageConfiguration GetImageConfiguration();
        string GetToolName();
        bool IsOfflineMode();
        IContainerizer SetAllowInsecureRegistries(bool allowInsecureRegistries);
        IContainerizer SetApplicationLayersCache(string cacheDirectory);
        IContainerizer SetBaseImageLayersCache(string cacheDirectory);
        IContainerizer SetOfflineMode(bool offline);
        IContainerizer SetToolName(string toolName);
        IContainerizer WithAdditionalTag(string tag);
        IContainerizer WithAdditionalTags(IEnumerable<string> tags);
    }
}