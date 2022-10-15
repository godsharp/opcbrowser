using Nuke.Common.CI.AzurePipelines;

[AzurePipelines(
    null,
    AzurePipelinesImage.WindowsLatest,
    AutoGenerate = true,
    TriggerBranchesInclude = new[] { "main" },
    InvokedTargets = new[] { nameof(Deploy) },
    NonEntryTargets = new[] { nameof(Clean), nameof(Restore), nameof(Compile), nameof(Publish), nameof(Delete), nameof(Compress), nameof(Artifacts) },
    // ImportSecrets = new[] {nameof(GitHubAccessToken)},
    CacheKeyFiles = new string[0]
)]
internal partial class Build
{
}