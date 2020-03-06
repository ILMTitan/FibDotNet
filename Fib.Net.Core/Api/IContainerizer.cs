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