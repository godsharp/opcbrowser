using Nuke.Common.Git;

public static class Extensions
{
    public static (string RepositoryOwner, string RepositoryName) GetGitHubRepositoryInfo(this  GitRepository gitRepository)
    {
        var split = gitRepository.Identifier.Split('/');
        return (split[0], split[1]);
    }
}