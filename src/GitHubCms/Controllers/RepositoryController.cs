using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using GitHubCms.Models;
using Octokit;

namespace GitHubCms.Controllers
{
	public class RepositoryController : BaseController
	{
		// GET: Repository
		public async Task<ActionResult> Index(long id, string name, string referrer = "edit", string reason="", string user="")
		{
			var accessToken = Session["OAuthToken"] as string;
			if (accessToken != null)
			{
				// This allows the client to make requests to the GitHub API on the user's behalf
				// without ever having the user's OAuth credentials.
				Client.Credentials = new Credentials(accessToken);
			}
			else
			{
				return Redirect(GetOauthLoginUrl());
			}

			try
			{
				if (string.IsNullOrEmpty(user))
				{
					var githubUser = await Client.User.Current();
					user = githubUser.Login;
				}
				IReadOnlyList<RepositoryContributor> contributors = await Client.Repository.GetAllContributors(RepoForTestingOwner, RepoForTesting);
				if (contributors.All(c => c.Login != user))
				{
					referrer = "not_authorized";
					reason =
						$"{user} is not a contributor for {RepoForTesting} repository. Please send request to " +
						$"{RepoForTestingOwner} at <a href='https://github.com/{RepoForTestingOwner}/{RepoForTesting}/graphs/contributors'>https://github.com/{RepoForTestingOwner}/{RepoForTesting}/graphs/contributors</a> ";
				}

				var originContent = await GetOriginContentForRepo(RepoForTesting);
				var model = new RepositoryViewModel(id, name,
					new List<RepositoryFileViewModel> { originContent }, referrer, reason, user);

				return View(model);
			}
			catch (AuthorizationException)
			{
				// Either the accessToken is null or it's invalid. This redirects
				// to the GitHub OAuth login page. That page will redirect back to the
				// Authorize action.
				return Redirect(GetOauthLoginUrl());
			}
		}

		[HttpPost]
		public async Task<ActionResult> Save(string fileName, string fileContent)
		{
			var accessToken = Session["OAuthToken"] as string;
			if (accessToken != null)
			{
				// This allows the client to make requests to the GitHub API on the user's behalf
				// without ever having the user's OAuth credentials.
				Client.Credentials = new Credentials(accessToken);
			}
			else
			{
				return Redirect(GetOauthLoginUrl());
			}

			try
			{
				var repository = (await GetRepo(RepoForTesting)).First();
				User user = await Client.User.Current();
				try
				{
					Reference reference = await UpdateContent("README.md", fileContent);
					return RedirectToAction("Index", new {id = repository.Id, name = repository.Name, referrer = "save_success", reason = reference.Object.Sha, user = user.Login });
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					return RedirectToAction("Index", new { id = repository.Id, name = repository.Name, referrer = "save_fail", reason=e.Message, user = user.Name });
				}

				
			}
			catch (AuthorizationException)
			{
				// Either the accessToken is null or it's invalid. This redirects
				// to the GitHub OAuth login page. That page will redirect back to the
				// Authorize action.
				return Redirect(GetOauthLoginUrl());
			}
		}

		private async Task<Reference> UpdateContent(string fileName, string fileContent)
		{
			var repoOwner = RepoForTestingOwner;// user.Login;

			var master = await Client.Git.Reference.Get(repoOwner, RepoForTesting, "heads/master");

			// create new commit for master branch
			var newMasterTree = await CreateTree(new Dictionary<string, string> {
				{ fileName, fileContent } }, repoOwner);
			var newMaster = await CreateCommit("Content edited via GitHub-CMS", newMasterTree.Sha, master.Object.Sha, repoOwner);

			// update master
			Reference reference = await Client.Git.Reference.Update(repoOwner, RepoForTesting, "heads/master", new ReferenceUpdate(newMaster.Sha));
			if (newMaster.Sha != reference.Object.Sha)
			{
				throw new Exception(
					$"UpdateClient commit sha dont match newMaster.Sha={newMaster.Sha} reference.Sha={reference.Object.Sha}");
			}
			return reference;
		}

		public async Task<RepositoryFileViewModel> GetOriginContentForRepo(string repoName)
		{
			try
			{
				var content = (await Client.Repository.Content.GetAllContents(RepoForTestingOwner, RepoForTesting, "README.md")).First();
				return new RepositoryFileViewModel("Readme.md", content.Content, content.HtmlUrl.ToString());
			}
			catch (Exception e)
			{
				Trace.TraceError($"Error getting repo content {e}");
				return new RepositoryFileViewModel("Readme.md", "internal server error", "");
			}
		}

		async Task<Octokit.Commit> CreateCommit(string message, string sha, string parent, string username)
		{
			var newCommit = new NewCommit(message, sha, parent);
			return await Client.Git.Commit.Create(username, RepoForTesting, newCommit);
		}

		async Task<TreeResponse> CreateTree(IDictionary<string, string> treeContents, string username)
		{
			var collection = new List<NewTreeItem>();

			foreach (var c in treeContents)
			{
				var baselineBlob = new NewBlob
				{
					Content = c.Value,
					Encoding = EncodingType.Utf8
				};
				var baselineBlobResult = await Client.Git.Blob.Create(username, RepoForTesting, baselineBlob);

				collection.Add(new NewTreeItem
				{
					Type = TreeType.Blob,
					Mode = Octokit.FileMode.File,
					Path = c.Key,
					Sha = baselineBlobResult.Sha
				});
			}

			var newTree = new NewTree();
			foreach (var item in collection)
			{
				newTree.Tree.Add(item);
			}

			return await Client.Git.Tree.Create(username, RepoForTesting, newTree);
		}
	}
}
