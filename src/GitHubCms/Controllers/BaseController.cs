﻿using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Octokit;

namespace GitHubCms.Controllers
{
	public class BaseController : Controller
	{
		// secrets from https://github.com/settings/applications/549481
		private static readonly string ClientId = ConfigurationManager.AppSettings["ClientId"];
		private static readonly string ClientSecret = ConfigurationManager.AppSettings["ClientSecret"];
		private static readonly string DeployedEndpoint = ConfigurationManager.AppSettings["deployedEndpoint"];
		protected static readonly string RepoForTesting = ConfigurationManager.AppSettings["RepoForTest"];
		protected static readonly string RepoForTestingOwner = ConfigurationManager.AppSettings["OwnerOfRepoForTest"];
		private static readonly string GitHubAppName = ConfigurationManager.AppSettings["GitHubAppName"];

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