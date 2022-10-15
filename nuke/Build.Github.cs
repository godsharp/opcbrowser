using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.StaticFiles;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using Octokit;

[GitHubActions(
    "continuous",
    GitHubActionsImage.WindowsLatest,
    AutoGenerate = true,
    PublishArtifacts = true,
    // On = new[] { GitHubActionsTrigger.Push },
    OnPushBranches = new []{"main"},
    InvokedTargets = new []{nameof(Deploy)},
    ImportSecrets = new []{nameof(GhAccessToken)},
    CacheKeyFiles = new string[0]
)]
partial class Build
{
    Target Deploy => _ => _
        .Description("Deploy")
        .DependsOn(Artifacts)
        .WhenSkipped(DependencyBehavior.Execute)
        .OnlyWhenStatic(() => IsServerBuild && HostType == HostType.GitHubActions)
        .OnlyWhenDynamic(() => GitVersion.BranchName.Equals("main") || GitVersion.BranchName.Equals("origin/main"))
        .Executes(async () =>
        {
            if(string.IsNullOrWhiteSpace(GhAccessToken)) ControlFlow.Fail($"{nameof(GhAccessToken)} is null");
            Logger.Info("Release to github...");
            await PublishAndUploadToGitHubRelease(GitVersion);
            Logger.Info("Release to github finished.");
        });
    
    async Task PublishAndUploadToGitHubRelease(GitVersion git)
    {
        try
        {
            var releaseTag = $"v{DateTime.Now:yyyyMMddHHmmss}";
            GitHubTasks.GitHubClient = new GitHubClient(new ProductHeaderValue(nameof(NukeBuild)))
            {
                Credentials = new Credentials(GhAccessToken, AuthenticationType.Bearer)
            };

            var (repositoryOwner, repositoryName) = GitRepository.GetGitHubRepositoryInfo();
            var existingReleases = await GitHubTasks.GitHubClient.Repository.Release.GetAll(repositoryOwner, repositoryName);

            if (existingReleases.Any(r => r.TagName == releaseTag)) return;

            var newRelease = new NewRelease(releaseTag)
            {
                TargetCommitish = git.Sha,
                Draft = true,
                Name = $"Release at {DateTime.Now:yyyyMMddHHmmss}",
                Prerelease = true,
                Body = $"Release at {DateTime.Now:yyyyMMddHHmmss}"
            };

            var createdRelease = await GitHubTasks.GitHubClient
                .Repository.Release
                .Create(repositoryOwner, repositoryName, newRelease);

            ArtifactsDirectory.GlobFiles("**/*").ForEach(async p => await UploadReleaseAssetToGithub(createdRelease, p));
        }
        catch (Exception e)
        {
            Logger.Error(e);
        }
        // await GitHubTasks.GitHubClient
        //     .Repository.Release
        //     .Edit(repositoryOwner, repositoryName, createdRelease.Id, new ReleaseUpdate { Draft = false });
    }
    
    Task UploadReleaseAssetToGithub(Release release, AbsolutePath asset)
    {
        if (!FileSystemTasks.FileExists(asset)) return Task.CompletedTask;

        if (!new FileExtensionContentTypeProvider().TryGetContentType(asset, out var assetContentType))
        {
            assetContentType = "application/x-binary";
        }

        var releaseAssetUpload = new ReleaseAssetUpload
        {
            ContentType = assetContentType,
            FileName = Path.GetFileName(asset),
            RawData = File.OpenRead(asset)
        };
        
        return GitHubTasks.GitHubClient.Repository.Release.UploadAsset(release, releaseAssetUpload);
    }
}