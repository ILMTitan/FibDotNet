using System;
using System.Collections.Generic;
using com.google.cloud.tools.jib.configuration;
using Jib.Net.Core.BuildSteps;
using Jib.Net.Core.FileSystem;

namespace Jib.Net.Core.Api
{
    public interface IContainerizer
    {
        event Action<IJibEvent> JibEvents;

        IContainerizer AddEventHandler<T>(Action<T> eventConsumer) where T : IJibEvent;
        EventHandlers BuildEventHandlers();
        IStepsRunner CreateStepsRunner(BuildConfiguration buildConfiguration);
        void Dispose();
        ISet<string> GetAdditionalTags();
        bool GetAllowInsecureRegistries();
        SystemPath GetApplicationLayersCacheDirectory();
        SystemPath GetBaseImageLayersCacheDirectory();
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