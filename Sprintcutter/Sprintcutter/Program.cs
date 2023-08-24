using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.RegularExpressions;
using System.Linq;

namespace AzureSprintcutter
{
    internal class Program
    {
        private static string[]? _repoArray;
        private static string[]? _midSprintRepoArray;
        private static string _sourceBranchName = "";
        private static string _newBranchName = "";
        private static string _midSprintBranchName = "";
        private static string _tagName = "";
        private static string _midSprintTagName = "";
        private static string _repoName = "";
        private static string _sprint = "";
        private static string _sprint_regex = "";
        private static string _pat = "";    // git token number of the user making changes in the git
        private static string _deleteBranch = "https://dev.azure.com/PlaceHolder/_apis/git/repositories/{0}/refs?api-version=6.0"; // Insert Organization Group Name where Placeholder

        private const string TAG_PUSH_DATE_FORMAT = "MM/dd/yyyy hh:mm:ss tt";

        private static bool LockBranch(string baseBranchName, bool toLock)
        {
            bool result = false;
            const string uri = "https://dev.azure.com/Placeholder";

            // using default credentials for the user
            VssBasicCredential credentials = new VssBasicCredential(string.Empty, _pat);
            Console.WriteLine($"Connecting to Azure DevOps Services at {uri}");
            VssConnection connection = new VssConnection(new Uri(uri), credentials);

            Console.WriteLine($"Getting a GitHttpClient to talk to the Git endpoints");
            using (GitHttpClient gitClient = connection.GetClient<GitHttpClient>())
            {
                try
                {
                    Console.WriteLine($"Getting data about {_repoName} repository");
                    GitRepository repo = gitClient.GetRepositoryAsync("PlaceHolder", _repoName).Result; // Insert Organization Project Name where Placeholder

                    bool exists = BranchExists(gitClient, repo, baseBranchName);
                    if (exists)
                    {
                        var gitRef = gitClient.UpdateRefAsync(new GitRefUpdate { IsLocked = toLock }, repositoryId: repo.Id, filter: $"heads/{baseBranchName}").Result;
                        result = true;
                    }
                    else
                    {
                        Console.WriteLine($"Source Branch: {_sourceBranchName} doesn't exist in {_repoName} repository");
                    }
                }
                catch (System.AggregateException ae)
                {
                    Console.WriteLine(ae.Message);
                    Console.WriteLine($"Please validate your access token or validate your access to the URL/repo: {_repoName}");
                    throw ae;
                }
                return result;
            }
        }
        // Delete any Release Branches older then 5 sprints
        private static bool DeletePreviousReleaseBranches()
        {
            bool result = false;
            Guid projectId;
            List<GitBranchStats> branches = new List<GitBranchStats>();
            List<GitBranchStats> releaseBranchesToDelete = new List<GitBranchStats>();
            VssBasicCredential credentials = new VssBasicCredential(string.Empty, _pat);
            VssConnection connection = new VssConnection(new Uri(string.Format("https://dev.azure.com/{0}", "PlaceHolder")), credentials);
            using (GitHttpClient gitClient = connection.GetClient<GitHttpClient>())
            {
                GitRepository repo = gitClient.GetRepositoryAsync("PlaceHolder", _repoName).Result; // Insert Organization Project Name where Placeholder
                branches = gitClient.GetBranchesAsync(repo.Id).SyncResult();
                List<int> pastFiveSprints = new List<int> { Int32.Parse(_sprint), Int32.Parse(_sprint) - 1, Int32.Parse(_sprint) - 2, Int32.Parse(_sprint) - 3, Int32.Parse(_sprint) - 4 };
                List<string> pastFiveSprintsString = pastFiveSprints.ConvertAll<string>(x => x.ToString());

                foreach (GitBranchStats branch in branches)
                {
                    if (branch.Name.Contains("Release") && !pastFiveSprintsString.Any(branch.Name.Contains))
                    {
                        if (!releaseBranchesToDelete.Contains(branch))
                        {
                            releaseBranchesToDelete.Add(branch);
                        }
                    }
                }

                foreach (GitBranchStats branch in releaseBranchesToDelete)
                {
                    var gitRef = gitClient.UpdateRefsAsync(new GitRefUpdate[] {new GitRefUpdate
                            {
                                Name = $"refs/heads/{branch.Name}",
                                OldObjectId = branch.Commit.CommitId,
                                NewObjectId = "0000000000000000000000000000000000000000",
                            }}, repositoryId: repo.Id).Result;
                    result = !BranchExists(gitClient, repo, branch.Name);
                }
            }
            return result;
        }

        private static bool DeleteCustomBranches()
        {
            bool result = false;
            Guid projectId;
            List<GitBranchStats> branches = new List<GitBranchStats>();
            List<GitBranchStats> custombranchList = new List<GitBranchStats>();
            VssBasicCredential credentials = new VssBasicCredential(string.Empty, _pat);
            VssConnection connection = new VssConnection(new Uri(string.Format("https://dev.azure.com/{0}", "PlaceHolder")), credentials); // Insert Organization Group Name where Placeholder
            using (GitHttpClient gitClient = connection.GetClient<GitHttpClient>())
            {
                GitRepository repo = gitClient.GetRepositoryAsync("PlaceHolder", "CustomRepo").Result; // Insert Organization Project Name where Placeholder and replace repo name
                branches = gitClient.GetBranchesAsync(repo.Id).SyncResult();

                foreach (GitBranchStats branch in branches)
                {
                    if (branch.Name.Contains("CustomFeatureBranch")) // Replace with name format of branches you want to delete
                    {
                        custombranchList.Add(branch);
                    }
                }

                foreach (GitBranchStats branch in custombranchList)
                {
                    var gitRef = gitClient.UpdateRefsAsync(new GitRefUpdate[] {new GitRefUpdate
                            {
                                Name = $"refs/heads/{branch.Name}",
                                OldObjectId = branch.Commit.CommitId,
                                NewObjectId = "0000000000000000000000000000000000000000",
                            }}, repositoryId: repo.Id).Result;
                    result = !BranchExists(gitClient, repo, branch.Name);
                }
            }
            return result;
        }

        private static bool CreateBranch(string baseBranchName, string newBranchName)
        {
            bool result = false;
            VssBasicCredential credentials = new VssBasicCredential(string.Empty, _pat);
            VssConnection connection = new VssConnection(new Uri(string.Format("https://dev.azure.com/{0}", "PlaceHolder")), credentials); // Insert Organization Group Name where Placeholder
            using (GitHttpClient gitClient = connection.GetClient<GitHttpClient>())
            {
                GitRepository repo = gitClient.GetRepositoryAsync("PlaceHolder", _repoName).Result; // Insert Organization Project Name where Placeholder 
                GitBranchStats? baseBranch = null;
                try
                {
                    baseBranch = gitClient.GetBranchAsync(repo.ProjectReference.Id, repo.Id, baseBranchName).Result;
                }
                catch (AggregateException ae)
                {
                    if (ae.InnerException == null || ae.InnerException.Message != $"Branch \"{baseBranchName}\" does not exist in the {repo.Id} repository.")
                    {
                        throw ae;
                    }
                }

                if (baseBranch != null)
                {
                    bool exists = BranchExists(gitClient, repo, newBranchName);
                    if (!exists)
                    {
                        var gitRef = gitClient.UpdateRefsAsync(new GitRefUpdate[] {new GitRefUpdate
                            {
                                Name = $"refs/heads/{newBranchName}",
                                NewObjectId = baseBranch.Commit.CommitId,
                                OldObjectId = new string('0', 40),
                                IsLocked = false,
                            }}, repositoryId: repo.Id).Result;

                        result = BranchExists(gitClient, repo, newBranchName);
                    }
                }
            }
            return result;
        }

        private static bool BranchExists(GitHttpClient gitClient, GitRepository repo, string branchName)
        {
            bool result = true;
            try
            {
                var branch = gitClient.GetBranchAsync(repo.ProjectReference.Id, repo.Id, branchName).Result;
            }
            catch (AggregateException ae)
            {
                if (ae.InnerException != null && ae.InnerException.Message == $"Branch \"{branchName}\" does not exist in the {repo.Id} repository.")
                {
                    result = false;
                }
                else
                {
                    throw ae;
                }
            }
            return result;
        }

        private static bool CreateTag(string branchName, string tagName)
        {
            VssBasicCredential credentials = new VssBasicCredential(string.Empty, _pat);
            VssConnection connection = new VssConnection(new Uri(string.Format("https://dev.azure.com/{0}", "PlaceHolder")), credentials); // Insert Organization Group Name where Placeholder
            using (GitHttpClient gitClient = connection.GetClient<GitHttpClient>())
            {
                GitRepository repo = gitClient.GetRepositoryAsync("PlaceHolder", _repoName).Result; // Insert Organization Project Name where Placeholder 
                var branch = gitClient.GetBranchAsync(repo.ProjectReference.Id, repo.Id, branchName).Result;
                GitAnnotatedTag newTag = new GitAnnotatedTag
                {
                    Name = tagName,
                    Message = "A new Tag Message",
                    ObjectId = branch.Commit.CommitId,
                    TaggedBy = new GitUserDate
                    {
                        Date = DateTime.Now,
                        Email = "Developer@PlaceHolder.com", // Put email of account that will be shown on Tags
                        ImageUrl = null,
                        Name = Environment.UserName
                    },
                    Url = String.Empty,
                    TaggedObject = new GitObject
                    {
                        ObjectId = branch.Commit.CommitId,
                        ObjectType = GitObjectType.Commit
                    }
                };
                if (!TagExists(gitClient, repo, tagName))
                {
                    var createdTag = gitClient.CreateAnnotatedTagAsync(newTag, "PlaceHolder", repo.Id).Result; // Insert Organization Project Name where Placeholder 
                    return true;
                }
                else
                {
                    return false;
                }

            }
        }

        private static bool TagExists(GitHttpClient gitClient, GitRepository repo, string tagName)
        {
            bool result = true;
            var refTags = gitClient.GetRefsAsync(repo.Id, filterContains: tagName, peelTags: true).Result;
            if (!refTags.Any())
            {
                result = false;
            }
            return result;
        }



        private static void EnterParams()
        {
            Console.Write("Sprint: ");
            _sprint = Console.ReadLine();
        }



        private static bool IsParamsValid()
        {
            EnterParams();
            bool result = true;

            if (string.IsNullOrEmpty(_sprint))
            {
                result = false;
            }
            else
            {
                Regex pattern = new Regex(_sprint_regex);
                Match match = pattern.Match(_sprint);
                if (string.IsNullOrEmpty(match.Groups[0].Value))
                {
                    result = false;
                }
            }
            return result;
        }


        private static bool EnterToken()
        {
            bool result = false;
            Console.Write("Enter your DevOps access token: ");
            _pat = Console.ReadLine();

            if (!string.IsNullOrEmpty(_pat))
            {
                result = true;
            }
            return result;
        }

        static void Main(string[] args)
        {

            IConfiguration configuration = new ConfigurationBuilder()   // Building JSON configuration file from appsettings.json file
            .AddJsonFile("appsettings.json")
            .Build();
            _sprint_regex = configuration["SPRINT_REGEX_4_DIGITS"];

            EnterToken();

            bool valid = IsParamsValid();   // Taking input from user for sprint number
            while (!valid)                  // Recurring loop if user input is invalid
            {
                Console.WriteLine("Invalid User Input!!\nEnter a valid sprint number");
                valid = IsParamsValid();
            }

            _sourceBranchName = configuration["sourceBranchName"];
            _newBranchName = string.Format(configuration["branchName"], _sprint);
            _midSprintBranchName = string.Format(configuration["midSprintBranchName"], _sprint);
            _tagName = string.Format(configuration["tagname"], _sprint);
            _midSprintTagName = string.Format(configuration["midSprintTagName"], _sprint);
            var repArr = configuration.GetSection("repos").Get<string[]>();     // Accessing the repository array from appsettings.json file
            var midSprintRepArr = configuration.GetSection("midSprintRepos").Get<string[]>();
            _repoArray = repArr;
            _midSprintRepoArray = midSprintRepArr;


            if (_repoArray != null)
            {
                foreach (var repo in _repoArray)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Repository: {repo}");
                    _repoName = repo;

                    // Locking the source branch-"Development"
                    bool sourcebranchexists = LockBranch(_sourceBranchName, true);
                    if (!sourcebranchexists)
                    {
                        return;
                    }
                    Console.WriteLine($"Branch: {_sourceBranchName} is locked");

                    // Creating a new Release Candidate branch
                    bool branchCreated = CreateBranch(_sourceBranchName, _newBranchName);
                    if (branchCreated)
                    {
                        Console.WriteLine($"Branch: {_newBranchName} is created");
                    }
                    else
                    {
                        Console.WriteLine($"Branch: {_newBranchName} already exists");
                    }

                    // Creating a new tag
                    bool tagCreated = CreateTag(_newBranchName, _tagName);
                    if (tagCreated)
                    {
                        Console.WriteLine($"Tag: {_tagName} is created");
                    }
                    else
                    {
                        Console.WriteLine($"Tag: {_tagName} already exists");
                    }

                    // Unlocking the source branch-"Development"
                    LockBranch(_sourceBranchName, false);
                    Console.WriteLine($"Branch: {_sourceBranchName} is unlocked");

                    // Deleting previous Release Branches
                    bool releaseBranchesDeleted = DeletePreviousReleaseBranches();
                    if (releaseBranchesDeleted)
                    {
                        Console.WriteLine("Previous Release Branches Deleted");
                    }
                    else
                    {
                        Console.WriteLine("No Release Branches to Delete");
                    }
                }
                foreach (var repo in _midSprintRepoArray)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Repository: {repo}");
                    _repoName = repo;

                    // Locking the source branch-"Development"
                    bool sourcebranchexists = LockBranch(_sourceBranchName, true);
                    if (!sourcebranchexists)
                    {
                        return;
                    }
                    Console.WriteLine($"Branch: {_sourceBranchName} is locked");

                    // Creating a new Release Candidate branch
                    bool branchCreated = CreateBranch(_sourceBranchName, _midSprintBranchName);
                    if (branchCreated)
                    {
                        Console.WriteLine($"Branch: {_midSprintBranchName} is created");
                    }
                    else
                    {
                        Console.WriteLine($"Branch: {_midSprintBranchName} already exists");
                    }

                    // Creating a new tag
                    bool tagCreated = CreateTag(_midSprintBranchName, _midSprintTagName);
                    if (tagCreated)
                    {
                        Console.WriteLine($"Tag: {_midSprintTagName} is created");
                    }
                    else
                    {
                        Console.WriteLine($"Tag: {_midSprintTagName} already exists");
                    }

                    // Unlocking the source branch-"Development"
                    LockBranch(_sourceBranchName, false);
                    Console.WriteLine($"Branch: {_sourceBranchName} is unlocked");
                }
                // Deleting all Custom Feature Branches with Specific Name
                bool branchDeleted = DeleteCustomBranches();
                if (branchDeleted)
                {
                    Console.WriteLine("Custom Branches Deleted");
                }
                else
                {
                    Console.WriteLine("No Custom Branches to Delete");
                }
            }
            else
            {
                Console.WriteLine("Supplied repository array is null");
                return;
            }

        }
    }
}