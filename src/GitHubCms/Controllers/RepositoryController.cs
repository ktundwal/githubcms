using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using GitHubCms.Helpers;
using GitHubCms.Models;
using Octokit;

namespace GitHubCms.Controllers
{
	public class RepositoryController : BaseController
	{
		private const string FileNameForTesting = "README.md";

		// GET: Repository
		public async Task<ActionResult> Index(long id, string name, string referrer = "edit", string reason = "",
			string user = "")
		{
			if (Session["OAuthToken"] is string accessToken)
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

				IReadOnlyList<RepositoryContributor> contributors =
					await Client.Repository.GetAllContributors(RepoForTestingOwner, RepoForTesting);
				if (contributors.All(c => c.Login != user))
				{
					referrer = "not_authorized";
					reason =
						$"{user} is not a contributor for {RepoForTesting} repository. Please send request to " +
						$"{RepoForTestingOwner} at <a href=" +
						$"'https://github.com/{RepoForTestingOwner}/{RepoForTesting}/graphs/contributors'>" +
						$"https://github.com/{RepoForTestingOwner}/{RepoForTesting}/graphs/contributors</a> ";
				}

				var originContent = await GitHubHelper.GetOriginContentForRepo(Client,
					RepoForTesting,
					RepoForTestingOwner,
					FileNameForTesting);
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
			if (Session["OAuthToken"] is string accessToken)
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
					Reference reference = await GitHubHelper.UpdateContent(Client,
						RepoForTesting,
						RepoForTestingOwner,
						FileNameForTesting,
						fileContent);

					return RedirectToAction("Index",
						new
						{
							id = repository.Id,
							name = repository.Name,
							referrer = "save_success",
							reason = reference.Object.Sha,
							user = user.Login
						});
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					return RedirectToAction("Index",
						new
						{
							id = repository.Id,
							name = repository.Name,
							referrer = "save_fail",
							reason = e.Message,
							user = user.Name
						});
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
	}
}
