using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;

namespace Fib.Net.MSBuild
{
    public class PublishImage : ToolTask
    {
        private string _targetImage;
        private string _baseImage;

        [Required]
        public string PublishType { get; set; }

        [Required]
        public string BaseImage
        {
            get { return _baseImage; }
            set { _baseImage = value?.ToLowerInvariant(); }
        }

        [Required]
        public string TargetImage
        {
            get { return _targetImage; }
            set { _targetImage = value?.ToLowerInvariant(); }
        }

        public ITaskItem[] TargetTags { get; set; }

        public string OutputTarFile { get; set; }

        public ITaskItem[] ImageFiles { get; set; }

        public string Entrypoint { get; set; }
        public string Cmd { get; set; }
        public string ImageWorkingDirectory { get; set; }
        public string ImageUser { get; set; }
        public ITaskItem[] Environment { get; set; }
        public ITaskItem[] Ports { get; set; }
        public ITaskItem[] Volumes { get; set; }
        public ITaskItem[] Labels { get; set; }

        public bool AllowInsecureRegistries { get; set; }
        public bool OfflineMode { get; set; }
        public string ApplicationLayersCacheDirectory { get; set; }
        public string BaseLayersCacheDirectory { get; set; }
        public bool ReproducableBuild { get; set; }
        public string ImageFormat { get; set; }

        [Required]
        public string CliExecutablePath { get; set; }

        [Output]
        public string ImageId { get; set; }

        [Output]
        public string ImageDigest { get; set; }

        /// <summary>
        /// Splits a command line string as defined by
        /// <see href="https://docs.microsoft.com/windows/win32/api/shellapi/nf-shellapi-commandlinetoargvw">
        ///   CommandLineToArgvW
        /// </see>.
        /// </summary>
        /// <param name="commandLine">The command line string to split.</param>
        /// <returns>An enumeration of the split arguments of the command line string.</returns>
        /// <seealso href="https://docs.microsoft.com/windows/win32/api/shellapi/nf-shellapi-commandlinetoargvw"/>
        private static IEnumerable<string> CommandLineToArgs(string commandLine)
        {
            if (commandLine.TrimStart() != commandLine)
            {
                yield return "";
            }
            bool inQuotes = false;
            int numBackslashes = 0;
            StringBuilder currentArg = new StringBuilder();
            foreach (char c in commandLine)
            {
                if (c == '"')
                {
                    currentArg.Append(Enumerable.Repeat('\\', numBackslashes / 2).ToArray());
                    if (numBackslashes % 2 == 0)
                    {
                        inQuotes ^= true;
                    }
                    else
                    {
                        currentArg.Append(c);
                    }
                    numBackslashes = 0;
                }
                else if (c == '\\')
                {
                    numBackslashes++;
                }
                else
                {
                    currentArg.Append(Enumerable.Repeat('\\', numBackslashes).ToArray());
                    numBackslashes = 0;
                    if (!char.IsWhiteSpace(c) || inQuotes)
                    {
                        currentArg.Append(c);
                    }
                    else
                    {
                        if (currentArg.Length != 0)
                        {
                            yield return currentArg.ToString();
                            currentArg.Clear();
                        }
                    }
                }
            }
            if (currentArg.Length != 0)
            {
                yield return currentArg.ToString();
                currentArg.Clear();
            }
        }

        protected override string ToolName => "dotnet";

        public override string ToolExe { get => "dotnet"; }

        protected override string GenerateFullPathToTool()
        {
            return ToolExe;
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            Log.LogMessage(singleLine, MessageImportance.High);
        }

        protected override MessageImportance StandardOutputLoggingImportance => MessageImportance.High;

        protected override string GenerateCommandLineCommands()
        {
            string publishType = PublishType.ToLowerInvariant();
            string commandLine = $" \"{CliExecutablePath}\" {publishType} --toolname Fib.Net.MSBuild";
            if (publishType == "tar")
            {
                return commandLine + $" --outputfile {OutputTarFile}";
            }
            return commandLine;
        }
        
        protected override string GetResponseFileSwitch(string responseFilePath)
        {
            return $"--configfile \"{responseFilePath}\"";
        }

        protected override string GenerateResponseFileCommands()
        {
            string EncodeItemSpec(ITaskItem i) => EncodeJavaScriptString(i.ItemSpec);
            string EncodeItemSpecArray(IEnumerable<ITaskItem> i) => EncodeJavaScriptArray(i, EncodeItemSpec);
            string EncodeItemSpecMap(IEnumerable<ITaskItem> items) => EncodeJavaScriptDictionary(
                    items.Select(i => i.ItemSpec)
                        .ToDictionary(
                            i => i.Substring(i.IndexOf('=')),
                            i => i.Substring(i.IndexOf('=') + 1, i.Length)));
            string EncodeCommandLine(string i) =>
                EncodeJavaScriptArray(CommandLineToArgs(i), EncodeJavaScriptString);
            Log.LogMessage(MessageImportance.High, "Ports Is Null: " + (Ports == null));
            Log.LogMessage(MessageImportance.High, "Ports count: " + Ports?.Length);
            return $@"
{{
    ""BaseImage"": {EncodeJavaScriptString(BaseImage)},
    ""TargetImage"": {EncodeJavaScriptString(TargetImage)},
    {EncodeJavaScriptProperty("TargetTags", TargetTags, EncodeItemSpecArray)}
    {EncodeJavaScriptProperty("ImageFormat", ImageFormat, EncodeJavaScriptString)}
    {EncodeJavaScriptProperty("ImageLayers", ImageFiles, EncodeJavaScriptLayersArray)}
    {EncodeJavaScriptProperty("Entrypoint", Entrypoint, EncodeCommandLine)}
    {EncodeJavaScriptProperty("Cmd", Cmd, EncodeCommandLine)}
    {EncodeJavaScriptProperty("Environment", Environment, EncodeItemSpecMap)}
    {EncodeJavaScriptProperty("ImageWorkingDirectory", ImageWorkingDirectory, EncodeJavaScriptString)}
    {EncodeJavaScriptProperty("ImageUser", ImageUser, EncodeJavaScriptString)}
    {EncodeJavaScriptProperty("Ports", Ports, EncodeItemSpecArray)}
    {EncodeJavaScriptProperty("Volumes", Volumes, EncodeItemSpecArray)}
    {EncodeJavaScriptProperty("Labels", Labels, EncodeItemSpecMap)}
    {EncodeJavaScriptProperty("ApplicationLayersCacheDirectory", ApplicationLayersCacheDirectory, EncodeJavaScriptString)}
    {EncodeJavaScriptProperty("BaseLayersCacheDirectory", BaseLayersCacheDirectory, EncodeJavaScriptString)}
    ""ReproducableBuild"": {ReproducableBuild.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()},
    ""AllowInsecureRegistries"": {AllowInsecureRegistries.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()},
    ""OfflineMode"": {OfflineMode.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()}
}}
";
        }

        private static string EncodeJavaScriptDictionary(IReadOnlyDictionary<string, string> values)
        {
            return $@"{{
            {string.Join(@",
            ", values.Select(pair => $"\"{pair.Key}\": \"{pair.Value}\""))}
        }}";
        }

        private string EncodeJavaScriptLayersArray(ITaskItem[] imageFiles) =>
            EncodeJavaScriptArray(imageFiles.GroupBy(i => i.GetMetadata("Layer")), EncodeJavaScriptLayerObject);

        private string EncodeJavaScriptLayerObject(IGrouping<string, ITaskItem> value)
        {
            return $@"{{
            ""Name"": {EncodeJavaScriptString(value.Key)},
            ""LayerEntries"": {EncodeJavaScriptArray(value, EncodeJavaScriptLayerEntryObject)}
        }}";
        }

        private string EncodeJavaScriptLayerEntryObject(ITaskItem value)
        {
            return $@"{{
                ""SourceFile"": {EncodeJavaScriptString(value.GetMetadata("FullPath"))},
                ""ExtractionPath"": {EncodeJavaScriptString(value.GetMetadata("TargetPath"))}
            }}";
        }

        private static string EncodeJavaScriptString(string value)
        {
            return $"\"{HttpUtility.JavaScriptStringEncode(value)}\"";
        }

        private static string EncodeJavaScriptProperty<T>(string propertyName, T value, Func<T, string> encodeJavaScript)
        {
            if (value == null)
            {
                return "";
            }
            return $@"""{propertyName}"": {encodeJavaScript(value)},";
        }

        private static string EncodeJavaScriptArray<T>(IEnumerable<T> values, Func<T, string> encodeJavaScript)
        {
            if(values == null)
            {
                return "null";
            }
            return $"[{string.Join(",", values.Select(encodeJavaScript))}]";
        }
    }
}