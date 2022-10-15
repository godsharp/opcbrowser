using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;

using Serilog;

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;
// ReSharper disable InconsistentNaming

[CheckBuildProjectConfigurations]
internal partial class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compress);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    private readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("GitHub Authentication Token")]
    [Secret]
    private string GhAccessToken;

    [Solution] private readonly Solution Solution;

    [GitRepository] private readonly GitRepository GitRepository;
    [GitVersion(Framework = "net5.0", NoFetch = true)] private readonly GitVersion GitVersion;

    [CI] private readonly AzurePipelines AzurePipelines;
    [CI] private readonly GitHubActions GitHubActions;

    private AbsolutePath SourceDirectory => RootDirectory / "src";
    private AbsolutePath PublishDirectory => RootDirectory / "publish";
    private AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    private HostType HostType = HostType.None;

    protected override void OnBuildInitialized()
    {
        base.OnBuildInitialized();
        GhAccessToken ??= Environment.GetEnvironmentVariable(nameof(GhAccessToken));
        Enum.TryParse(Host.GetType().Name, true, out HostType);
    }

    // ReSharper disable once UnusedMember.Local
    private Target Echo => _ => _
        .Description("Echo")
        .Executes(() =>
        {
            Console.WriteLine(GitVersion?.Sha);
            Console.WriteLine(GitRepository?.Branch);
        });

    private Target Clean => _ => _
        .Description("Clean Solution")
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(PublishDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    private Target Restore => _ => _
        .Description("Restore Solution")
        .DependsOn(Clean)
        .Executes(() =>
        {
            MSBuild(s => s
                .SetTargetPath(Solution)
                .SetTargets("Restore"));
        });

    private Target Compile => _ => _
        .Description("Compile Solution")
        .DependsOn(Restore)
        .Executes(() =>
        {
            MSBuild(s => s
                .SetTargetPath(Solution)
                .SetTargets("Rebuild", "Publish")
                .SetConfiguration(Configuration)
                .SetMaxCpuCount(Environment.ProcessorCount)
                .SetNodeReuse(IsLocalBuild));
        });

    private Target Publish => _ => _
        .Description("Publish Project")
        .DependsOn(Compile)
        .Executes(() =>
        {
            Solution.AllProjects.Where(x =>
            {
                try { return x.GetProperty<bool>("Publishable"); }
                catch (Exception) { return false; }
            }).ForEach(p =>
            {
                p.GetTargetFrameworks()?.Where(x => !string.IsNullOrWhiteSpace(x)).ForEach(f =>
                {
                    var dir = PublishDirectory / p.Name;
                    MSBuild(s => s
                        .SetTargetPath(p.Path)
                        .SetConfiguration(Configuration)
                        .SetOutDir(dir / f)
                        .SetMaxCpuCount(Environment.ProcessorCount)
                        .SetNodeReuse(IsLocalBuild)
                        .SetProperty("TargetFramework", f)
                        .SetTargets("Publish")
                    );
                });
            });
        });

    private Target Delete => _ => _
        .DependsOn(Publish)
        .Executes(() =>
        {
            Log.Information("Delete output..");
            PublishDirectory.GlobDirectories("**/output").ForEach(DeleteDirectory);
        });

    private Target Compress => _ => _
        //.DependsOn(Publish)
        .DependsOn(Publish, Delete)
        .OnlyWhenStatic(() => IsServerBuild, () => Configuration.Equals(Configuration.Release))
        .Description("Compress Publish")
        .Executes(() =>
        {
            GlobDirectories(PublishDirectory, "*")
                ?.ForEach(p =>
                {
                    var dir = Path.GetFileName(p);
                    var version = Solution.GetProject(dir).GetProperty("Version");
                    GlobDirectories(p, "*")
                        .ForEach(x =>
                        {
                            GlobFiles(x, "*.pdb").ForEach(DeleteFile);
                            var file = PublishDirectory / $"{dir}-v{version}-{new DirectoryInfo(x).Name}.zip";
                            ZipFile.CreateFromDirectory(x, file);
                            CopyFileToDirectory(file, ArtifactsDirectory / "app", FileExistsPolicy.OverwriteIfNewer);
                        });
                });
        });

    private Target Artifacts => _ => _
        .DependsOn(Compress)
        .OnlyWhenStatic(() => IsServerBuild)
        .Description("Upload Artifacts")
        .Executes(() =>
        {
            if (HostType != HostType.AzurePipelines) return;
            Log.Information("Upload artifacts to azure...");
            AzurePipelines
                .UploadArtifacts("artifacts", "artifacts", ArtifactsDirectory);
            Log.Information("Upload artifacts to azure finished.");
        });
}