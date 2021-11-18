using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Tools.DotNETCommon;

namespace UnrealBuildTool
{
    static class UnrealBuildTool
    {
        static public DateTime StartTimeUtc { get; } = DateTime.UtcNow;

        static public System.Collections.IDictionary InitialEnvironment;

        static private bool? bIsEngineInstalled;

        static private bool? bIsEnterpriseInstalled;

        static private bool? bIsProjectInstalled;

        static FileReference InstalledProjectFile;

        static DirectoryReference CachedEngineProgramSaveDirectory;

        public static readonly FileReference UnrealBuildToolPath = FileReference.FindCorrectCase(new FileReference(Assembly.GetExecutingAssembly()).GetOriginalLocation());

        public static readonly DirectoryReference RootDirectory = DirectoryReference.Combine(UnrealBuildToolPath.Directory, "..", "..", "..");

        public static readonly DirectoryReference EngineDirectory = DirectoryReference.Combine(RootDirectory, "Engine");

        public static readonly DirectoryReference EngineSourceDirectory = DirectoryReference.Combine(EngineDirectory, "Source");

        [Obsolete("Please use UnrealBuildTool.GetExtensionDirs(UnrealBuildTool.EngineDirectory, \"Source/Runtime\") instead.")]
        public static readonly DirectoryReference EngineSourceRuntimeDirectory = DirectoryReference.Combine(EngineSourceDirectory, "Runtime");

        [Obsolete("Please use UnrealBuildTool.GetExtensionDirs(UnrealBuildTool.EngineDirectory, \"Source/Developer\") instead.")]
        public static readonly DirectoryReference EngineSourceDeveloperDirectory = DirectoryReference.Combine(EngineSourceDirectory, "Developer");

        [Obsolete("Please use UnrealBuildTool.GetExtensionDirs(UnrealBuildTool.EngineDirectory, \"Source/Editor\") instead.")]
        public static readonly DirectoryReference EngineSourceEditorDirectory = DirectoryReference.Combine(EngineSourceDirectory, "Editor");

        [Obsolete("Please use UnrealBuildTool.GetExtensionDirs(UnrealBuildTool.EngineDirectory, \"Source/Programs\") instead.")]
        public static readonly DirectoryReference EngineSourceProgramsDirectory = DirectoryReference.Combine(EngineSourceDirectory, "Programs");

        [Obsolete("Please use UnrealBuildTool.GetExtensionDirs(UnrealBuildTool.EngineDirectory, \"Source/ThirdParty\") instead.")]
        public static readonly DirectoryReference EngineSourceThirdPartyDirectory = DirectoryReference.Combine(EngineSourceDirectory, "ThirdParty");

        public static readonly DirectoryReference EnterpriseDirectory = DirectoryReference.Combine(RootDirectory, "Enterprise");

        public static readonly DirectoryReference EnterpriseSourceDirectory = DirectoryReference.Combine(EnterpriseDirectory, "Source");

        public static readonly DirectoryReference EnterprisePluginsDirectory = DirectoryReference.Combine(EnterpriseDirectory, "Plugins");

        public static readonly DirectoryReference EnterpriseIntermediateDirectory = DirectoryReference.Combine(EnterpriseDirectory, "Intermediate");

        public static DirectoryReference EngineProgramSavedDirectory
        {
            get
            {
                if (CachedEngineProgramSaveDirectory == null)
                {
                    if (IsEngineInstalled())
                    {
                        CachedEngineProgramSaveDirectory = Utils.GetUserSettingDirectory() ?? DirectoryReference.Combine(EngineDirectory, "Programs");
                    }
                    else
                    {
                        CachedEngineProgramSaveDirectory = DirectoryReference.Combine(EngineDirectory, "Programs");
                    }
                }
                return CachedEngineProgramSaveDirectory;
            }
        }

        private static Dictionary<DirectoryReference, Tuple<List<DirectoryReference>, List<DirectoryReference>>> CachedExtensionDirectories = new Dictionary<DirectoryReference, Tuple<List<DirectoryReference>, List<DirectoryReference>>>();

        public static List<DirectoryReference> GetExtensionDirs(DirectoryReference BaseDir, bool bIncludePlatformDirectories=true, bool bIncludeRestrictedDirectories=true, bool bIncludeBaseDirectory=true)
        {
            Tuple<List<DirectoryReference>, List<DirectoryReference>> CachedDirs;
            if (!CachedExtensionDirectories.TryGetValue(BaseDir, out CachedDirs))
            {
                CachedDirs = Tuple.Create(new List<DirectoryReference>(), new List<DirectoryReference>());

                CachedExtensionDirectories[BaseDir] = CachedDirs;

                DirectoryReference PlatformExtensionBaseDir = DirectoryReference.Combine(BaseDir, "Platforms");
                if (DirectoryReference.Exists(PlatformExtensionBaseDir))
                {
                    CachedDirs.Item1.AddRange(DirectoryReference.EnumerateDirectories(PlatformExtensionBaseDir));
                }

                DirectoryReference RestrictedBaseDir = DirectoryReference.Combine(BaseDir, "Restricted");
                if (DirectoryReference.Exists(RestrictedBaseDir))
                {
                    IEnumerable<DirectoryReference> RestrictedDirs = DirectoryReference.EnumerateDirectories(RestrictedBaseDir);
                    CachedDirs.Item2.AddRange(RestrictedDirs);

                    foreach (DirectoryReference RestrictedDir in RestrictedDirs)
                    {
                        DirectoryReference RestrictedPlatformExtensionBaseDir = DirectoryReference.Combine(RestrictedDir, "Platforms");
                        if (DirectoryReference.Exists(RestrictedPlatformExtensionBaseDir))
                        {
                            CachedDirs.Item1.AddRange(DirectoryReference.EnumerateDirectories(RestrictedPlatformExtensionBaseDir));
                        }
                    }
                }

                if (BaseDir != UnrealBuildTool.EngineDirectory && CachedDirs.Item1.Count > 0)
                {
                    CachedDirs.Item1.RemoveAll(x => DataDrivenPlatformInfo.GetDataDrivenInfoForPlatform(x.GetDirectoryName()) == null);
                }
            }

            List<DirectoryReference> ExtensionDirs = new List<DirectoryReference>();
            if (bIncludeBaseDirectory)
            {
                ExtensionDirs.Add(BaseDir);
            }
            if (bIncludePlatformDirectories)
            {
                ExtensionDirs.AddRange(CachedDirs.Item1);
            }
            if (bIncludeRestrictedDirectories)
            {
                ExtensionDirs.AddRange(CachedDirs.Item2);
            }
            return ExtensionDirs;
        }

        public static List<DirectoryReference> GetExtensionDirs(DirectoryReference BaseDir, string SubDir, bool bIncludePlatformDirectories=true, bool bIncludeRestrictedDirectories=true, bool bIncludeBaseDirectory=true)
        {
            return GetExtensionDirs(BaseDir, bIncludePlatformDirectories, bIncludeRestrictedDirectories, bIncludeBaseDirectory).Select(x => DirectoryReference.Combine(x, SubDir)).Where(x => DirectoryReference.Exists(x)).ToList();
        }

        static string RemoteIniPath = null;

        static public bool IsEngineInstalled()
        {
            if (!bIsEngineInstalled.HasValue)
            {
                bIsEngineInstalled = FileReference.Exists(FileReference.Combine(EngineDirectory, "Build", "InstalledBuild.txt"));
            }
            return bIsEngineInstalled.Value;
        }

        static public bool IsEnterpriseInstalled()
        {
            if (!bIsEnterpriseInstalled.HasValue)
            {
                bIsEnterpriseInstalled = FileReference.Exists(FileReference.Combine(EnterpriseDirectory, "Build", "InstalledBuild.txt"));
            }
            return bIsEnterpriseInstalled.Value;
        }

        static public bool IsProjectInstalled()
        {
            if (!bIsProjectInstalled.HasValue)
            {
                FileReference InstalledProjectLocationFile = FileReference.Combine(UnrealBuildTool.RootDirectory, "Engine", "Build", "InstalledProjectBuild.txt");
                if (FileReference.Exists(InstalledProjectLocationFile))
                {
                    InstalledProjectFile = FileReference.Combine(UnrealBuildTool.RootDirectory, File.ReadAllText(InstalledProjectLocationFile.FullName).Trim());
                    bIsProjectInstalled = true;
                }
                else
                {
                    InstalledProjectFile = null;
                    bIsProjectInstalled = false;
                }
            }
            return bIsProjectInstalled.Value;
        }

        static public FileReference GetInstalledProjectFile()
        {
            if (IsProjectInstalled())
            {
                return InstalledProjectFile;
            }
            else
            {
                return null;
            }
        }

        static public bool IsFileInstalled(FileReference File)
        {
            if (IsEngineInstalled() && File.IsUnderDirectory(EngineDirectory))
            {
                return true;
            }
            if (IsEnterpriseInstalled() && File.IsUnderDirectory(EnterpriseDirectory))
            {
                return true;
            }
            if (IsProjectInstalled() && File.IsUnderDirectory(InstalledProjectFile.Directory))
            {
                return true;
            }
            return false;
        }

        static public FileReference GetUBTPath()
        {
            return UnrealBuildToolPath;
        }

        static public string GetRemoteIniPath()
        {
            return RemoteIniPath;
        }

        static public void SetRemoteIniPath(string Path)
        {
            RemoteIniPath = Path;
        }

        class GlobalOptions
        {
            [CommandLine(Prefix = "-Verbose", Value ="Verbose")]
            [CommandLine(Prefix = "-VeryVerbose", Value="VeryVerbose")]
            public LogEventType LogOutputLevel = LogEventType.Log;

            [CommandLine(Prefix = "-Log")]
            public FileReference LogFileName = null;

            [CommandLine(Prefix = "-Timestamps")]
            public bool bLogTimestamps = false;

            [CommandLine(Prefix = "-FromMsBuild")]
            public bool bLogFromMsBuild = false;

            [CommandLine(Prefix = "-Progress")]
            public bool bWriteProgressMarkup = false;

            [CommandLine(Prefix = "-NoMutex")]
            public bool bNoMutex = false;

            [CommandLine(Prefix = "-WaitMutex")]
            public bool bWaitMutex = false;

            [CommandLine(Prefix = "-RemoteIni")]
            public string RemoteIni = "";

            [CommandLine]
            [CommandLine("-Clean", Value = "Clean")]
            [CommandLine("-ProjectFiles", Value = "GenerateProjectFiles")]
            [CommandLine("-ProjectFileFormat=", Value = "GenerateProjectFiles")]
            [CommandLine("-Makefile", Value = "GenerateProjectFiles")]
            [CommandLine("-CMakefile", Value = "GenerateProjectFiles")]
            [CommandLine("-QMakefile", Value = "GenerateProjectFiles")]
            [CommandLine("-KDevelopfile", Value = "GenerateProjectFiles")]
            [CommandLine("-CodeliteFiles", Value = "GenerateProjectFiles")]
            [CommandLine("-XCodeProjectFiles", Value = "GenerateProjectFiles")]
            [CommandLine("-EdditProjectFiles", Value = "GenerateProjectFiles")]
            [CommandLine("-VSCode", Value = "GenerateProjectFiles")]
            [CommandLine("-VSMac", Value = "GenerateProjectFiles")]
            [CommandLine("-CLion", Value = "GenerateProjectFiles")]
            [CommandLine("-Rider", Value = "GenerateProjectFiles")]
            public string Mode = null;

            public GlobalOptions(CommandLineArguments Arguments)
            {
                Arguments.ApplyTo(this);
                if (!string.IsNullOrEmpty(RemoteIni))
                {
                    UnrealBuildTool.SetRemoteIniPath(RemoteIni);
                }
            }
        }

        private static int Main(string[] ArgumentsArray)
        {
            SingleInstanceMutex Mutex = null;
            try
            {
                Timeline.Start();

                CommandLineArguments Arguments = new CommandLineArguments(ArgumentsArray);

                GlobalOptions Options = new GlobalOptions(Arguments);

                Log.OutputLevel = Options.LogOutputLevel;
                Log.IncludeTimestamps = Options.bLogTimestamps;
                Log.IncludeProgramNameWithSeverityPrefix = Options.bLogFromMsBuild;

                ProgressWriter.bWriteMarkup = Options.bWriteProgressMarkup;

                if (Options.LogFileName != null)
                {
                    Log.AddFileWriter("LogTraceListener", Options.LogFileName);
                }

                AssemblyUtils.InstallAssemblyResolver(Path.GetDirectoryName(Assembly.GetEntryAssembly().GetOriginalLocation()));

                DirectoryReference.SetCurrentDirectory(UnrealBuildTool.EngineSourceDirectory);

                Type ModeType = typeof(BuildMode);
                if (Options.Mode != null)
                {
                    Dictionary<string, Type> ModeNameToType = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                    foreach (Type Type in Assembly.GetExecutingAssembly().GetTypes())
                    {
                        if (Type.IsClass && !Type.IsAbstract && Type.IsSubclassOf(typeof(ToolMode)))
                        {
                            ToolModeAttribute Attribute = Type.GetCustomAttributes<ToolModeAttribute>();
                            if (Attribute == null)
                            {
                                throw new BuildException("Class '{0}' should have a ToolModeAttribute", Type.Name);
                            }
                            ModeNameToType.Add(Attribute.Name, Type);
                        }
                    }

                    if (!ModeNameToType.TryGetValue(Options.Mode, out ModeType))
                    {
                        Log.TraceError("No mode named '{0}'. Available modes are:\n {1}", Options.Mode, String.Join("\n ", ModeNameToType.Keys));
                        return 1;
                    }
                }

                ToolModeOptions ModeOptions = ModeType.GetCustomAttribute<ToolModeAttribute>().Options;

                if ((ModeOptions & ToolModeOptions.StartPrefetchingEngine) != 0)
                {
                    using (Timeline.ScopeEvent("FileMetadataPrefetch.QueueEngineDirectory()"))
                    {
                        FileMetadataPrefetch.QueueEngineDirectory();
                    }
                }

                if ((ModeOptions & ToolModeOptions.XmlConfig) != 0)
                {
                    using (Timeline.ScopeEvent("XmlConfig.ReadConfigFiles()"))
                    {
                        string XmlConfigMutexName = SingleInstanceMutex.GetUniqueMutexForPath("UnrealBuildTool_Mutex_XmlConfig", Assembly.GetExecutingAssembly().CodeBase);
                        using (SingleInstanceMutex XmlConfigMutex = new SingleInstanceMutex(XmlConfigMutexName, true))
                        {
                            FileReference XmlConfigCache = Arguments.GetFileReferenceOrDefault("-XmlConfigCache=", null);
                            XmlConfig.ReadConfigFiles(XmlConfigCache);
                        }
                    }
                }

                if ((ModeOptions & ToolModeOptions.SingleInstance) != 0 && !Options.bNoMutex)
                {
                    using(Timeline.ScopeEvent("SingleInstanceMutex.Acquire()"))
                    {
                        string MutexName = SingleInstanceMutex.GetUniqueMutexForPath("UnrealBuildTool_Mutex", Assembly.GetExecutingAssembly().CodeBase);
                        Mutex = new SingleInstanceMutex(MutexName, Options.bWaitMutex);
                    }
                }

                if ((ModeOptions & ToolModeOptions.BuildPlatform) != 0)
                {
                    using (Timeline.ScopeEvent("UEBuildPlatform.RegisterPlatforms()"))
                    {
                        UEBuildPlatform.RegisterPlatforms(false, false);
                    }
                }
                if ((ModeOptions & ToolModeOptions.BuildPlatformsHostOnly) != 0)
                {
                    using (Timeline.ScopeEvent("UEBuildPlatform.RegisterPlatforms()"))
                    {
                        UEBuildPlatform.RegisterPlatforms(false, true);
                    }
                }
                if ((ModeOptions & ToolModeOptions.BuildPlatformsForValidation) != 0)
                {
                    using (Timeline.ScopeEvent("UEBuildPlatform.RegisterPlatforms()"))
                    {
                        UEBuildPlatform.RegisterPlatforms(true, false);
                    }
                }

                ToolMode Mode = (ToolMode)Activator.CreateInstance(ModeType);

                int Result = Mode.Execute(Arguments);
                if ((ModeOptions & ToolModeOptions.ShowExecutionTime) != 0)
                {
                    Log.TraceInformation("Total execution time: {0:0.00} seconds", Timeline.Elapsed.TotalSeconds);
                }
                return Result;
            }
            catch (CompilationResultException Ex)
            {
                Log.TraceLog(ExceptionUtils.FormatExceptionDetails(Ex));
                return (int)Ex.Result;
            }
            catch (BuildException Ex)
            {
                Log.TraceError(ExceptionUtils.FormatException(Ex));
                Log.TraceLog(ExceptionUtils.FormatExceptionDetails(Ex));
                return (int)CompilationResult.OtherCompilationError;
            }
            catch (Exception Ex)
            {
                Log.TraceError("Unhandled exception: {0}", ExceptionUtils.FormatException(Ex));
                Log.TraceLog(ExceptionUtils.FormatExceptionDetails(Ex));
                return (int)CompilationResult.OtherCompilationError;
            }
            finally
            {
                using (Timeline.ScopeEvent("FileMetadataPrefetch.Stop()"))
                {
                    FileMetadataPrefetch.Stop();
                }

                Timeline.Print(TimeSpan.FromMilliseconds(20.0), LogEventType.Log);

                Trace.Close();

                TraceSpan.Flush();

                if (Mutex != null)
                {
                    Mutex.Dispose();
                }
            }
        }
    }
}
