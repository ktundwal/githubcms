using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using GitHubCms.Helpers;
using Octokit;

namespace GitHubCms.Controllers
{
	public class BaseController : Controller
	{
		private static readonly string ClientId = StartupHelpers.ReadConfig("ClientId");
		private static readonly string ClientSecret = StartupHelpers.ReadConfig("ClientSecret");
		private static readonly string DeployedEndpoint = StartupHelpers.ReadConfig("deployedEndpoint");
		protected static readonly string RepoForTesting = StartupHelpers.ReadConfig("RepoForTest");
		protected static readonly string RepoForTestingOwner = StartupHelpers.ReadConfig("OwnerOfRepoForTest");
		private static readonly string GitHubAppName = StartupHelpers.ReadConfig("GitHubAppName");

		protected readonly GitHubClient Client =
			new GitHubClient(new ProductHeaderValue(GitHubAppName), GitHubClient.GitHubApiUrl);

		protected async Task<System.Collections.Generic.IEnumerable<Repository>> GetRepo(string repoName) =>
			(await Client.Repository.GetAllForUser(RepoForTestingOwner)).Where(r => r.Name.Contains(repoName));

		// This is the Callback URL that the GitHub OAuth Login page will redirect back to.
		public async Task<ActionResult> Authorize(string code, string state)
		{
			if (!String.IsNullOrEmpty(code))
			{
				var expectedState = Session["CSRF:State"] as string;
				if (state != expectedState) throw new InvalidOperationException("SECURITY FAIL!");
				Session["CSRF:State"] = null;

				Uri uri = new Uri($"{DeployedEndpoint}/home/authorize");

				var token = await Client.Oauth.CreateAccessToken(
					new OauthTokenRequest(ClientId, ClientSecret, code)
					{
						RedirectUri = uri
					});
				if (token.AccessToken == null) Trace.TraceError($"token.AccessToken is null");
				Session["OAuthToken"] = token.AccessToken;
			}

			return RedirectToAction("Index");
		}

		protected string GetOauthLoginUrl()
		{
			string csrf = System.Web.Security.Membership.GeneratePassword(24, 1);
			Session["CSRF:State"] = csrf;

			// 1. Redirect users to request GitHub access
			var request = new OauthLoginRequest(ClientId)
			{
				Scopes = { "user", "notifications", "public_repo", "repo", "write:org" },
				State = csrf
			};
			var oauthLoginUrl = Client.Oauth.GetGitHubLoginUrl(request);
			return oauthLoginUrl.ToString();
		}
	}
}