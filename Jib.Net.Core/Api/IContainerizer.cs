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

        Containerizer addEventHandler<T>(Action<T> eventConsumer) where T : IJibEvent;
        EventHandlers buildEventHandlers();
        IStepsRunner createStepsRunner(BuildConfiguration buildConfiguration);
        void Dispose();
        ISet<string> getAdditionalTags();
        bool getAllowInsecureRegistries();
        SystemPath getApplicationLayersCacheDirectory();
        SystemPath getBaseImageLayersCacheDirectory();
        string getDescription();
        ImageConfiguration getImageConfiguration();
        string getToolName();
        bool isOfflineMode();
        Containerizer setAllowInsecureRegistries(bool allowInsecureRegistries);
        Containerizer setApplicationLayersCache(SystemPath cacheDirectory);
        Containerizer setBaseImageLayersCache(SystemPath cacheDirectory);
        Containerizer setOfflineMode(bool offline);
        Containerizer setToolName(string toolName);
        Containerizer withAdditionalTag(string tag);
    }
}