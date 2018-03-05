using GitHubCms.Models;
using Octokit;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace GitHubCms.Controllers
{
	public class HomeController : BaseController
	{
		// This URL uses the GitHub API to get a list of the current user's
		// repositories which include public and private repositories.
		public async Task<ActionResult> Index()
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
				// The following requests retrieves all of the user's repositories and
				// requires that the user be logged in to work.
				var repositories = await GetRepo(RepoForTesting);
				var model = new IndexViewModel(repositories);

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

		public async Task<ActionResult> Emojis()
		{
			var emojis = await Client.Miscellaneous.GetAllEmojis();
			return View(emojis);
		}
	}
}