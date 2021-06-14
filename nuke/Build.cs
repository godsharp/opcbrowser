using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using Octokit;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;

[CheckBuildProjectConfigurations]
partial class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("GitHub Authentication Token")]
    readonly string GitHubAuthenticationToken;

    [Solution] readonly Solution Solution;
    [GitVersion] readonly GitVersion GitVersion;
    [GitRepository] readonly GitRepository GitRepository;

    [CI] readonly AzurePipelines AzurePipelines;
    [CI] readonly GitHubActions GitHubActions;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath PublishDirectory => RootDirectory / "publish";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target Initial => _ => _
        .Description("Initial")
        .OnlyWhenStatic(() => IsServerBuild)
        .Executes(() =>
        {
            //ProcessTasks.StartProcess("regsvr32.exe", $"/s {SourceDirectory / "lib" / "opcdaauto.dll"}");
        });

    Target Clean => _ => _
        .Description("Clean Solution")
        .DependsOn(Initial)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            RootDirectory.GlobDirectories("**/output", "**/output").ForEach(DeleteDirectory);
            RootDirectory.GlobDirectories("**/publish", "**/artifacts").ForEach(DeleteDirectory);
            RootDirectory.GlobDirectories("**/artifacts", "**/artifacts").ForEach(DeleteDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .Description("Restore Solution")
        .DependsOn(Clean)
        .Executes(() =>
        {
            MSBuild(s => s
                .SetTargetPath(Solution)
                .SetTargets("Restore"));
        });

    Target Compile => _ => _
        .Description("Compile Solution")
        .DependsOn(Restore)
        .Executes(() =>
        {
            MSBuild(s => s
                .SetTargetPath(Solution)
                .SetTargets("Rebuild")
                .SetConfiguration(Configuration)
                .SetMaxCpuCount(Environment.ProcessorCount)
                .SetNodeReuse(IsLocalBuild));
        });

    Target Zip => _ => _
        .DependsOn(Compile)
        .OnlyWhenStatic(() => IsServerBuild, () => Configuration.Equals(Configuration.Release))
        .Description("Zip Publish")
        .Executes(() =>
        {
            var version = Solution.GetProject("OpcDaBrowser").GetProperty("Version");
            GlobDirectories(OutputDirectory / "OpcDaBrowser", "*")
                .ForEach(x =>
                {
                    GlobFiles(x, "*.pdb").ForEach(DeleteFile);
                    var file = OutputDirectory / $"OpcDaBrowser-v{version}-{AzurePipelines.BuildId}-{new DirectoryInfo(x).Name}.zip";
                    ZipFile.CreateFromDirectory(x, file);
                    CopyFileToDirectory(file, ArtifactsDirectory / "app", FileExistsPolicy.OverwriteIfNewer);
                });
        });

    Target UploadArtifacts => _ => _
        .DependsOn(Zip)
        .OnlyWhenStatic(() => IsServerBuild)
        .Description("Upload Artifacts")
        .Executes(() =>
        {
            Logger.Info("Upload artifacts to azure...");
            AzurePipelines
                .UploadArtifacts("artifacts", "artifacts", ArtifactsDirectory);
            Logger.Info("Upload artifacts to azure finished.");
        });
}
