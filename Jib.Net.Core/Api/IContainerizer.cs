using System;
using System.Collections.Generic;
using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.configuration;
using Jib.Net.Core.FileSystem;

namespace Jib.Net.Core.Api
{
    public interface IContainerizer
    {
        event Action<IJibEvent> JibEvents;

        Containerizer AddEventHandler<T>(Action<T> eventConsumer) where T : IJibEvent;
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
        Containerizer SetAllowInsecureRegistries(bool allowInsecureRegistries);
        Containerizer SetApplicationLayersCache(SystemPath cacheDirectory);
        Containerizer SetBaseImageLayersCache(SystemPath cacheDirectory);
        Containerizer SetOfflineMode(bool offline);
        Containerizer SetToolName(string toolName);
        Containerizer WithAdditionalTag(string tag);
    }
}