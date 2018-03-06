using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GitHubCms.Models;
using Octokit;

namespace GitHubCms.Helpers
{
	/// <summary>
	/// Wraps internal GitHub Repository APIs in helper methods. 
	/// Used by Controllers.
	/// </summary>
	public static class GitHubHelper
	{
		private const string MasterBranchReference = "heads/master";

		/// <summary>
		/// Update content in GitHub repo
		/// </summary>
		/// <param name="client">GitHub client to use</param>
		/// <param name="repoName">repository name</param>
		/// <param name="repoOwner">Owner of repository</param>
		/// <param name="fileName">File in repo to update</param>
		/// <param name="fileContent">Content (plain text) to use for update</param>
		/// <returns>Git reference</returns>
		public static async Task<Reference> UpdateContent(GitHubClient client,
			string repoName,
			string repoOwner,
			string fileName,
			string fileContent)
		{
			var master = await client.Git.Reference.Get(repoOwner, repoName, MasterBranchReference);

			// create new commit for master branch
			var newMasterTree = await CreateTree(
				client,
				repoName,
				new Dictionary<string, string>
				{
						{fileName, fileContent}
				},
				repoOwner);
			var newMaster = await CreateCommit(client,
				repoName,
				"Content edited via GitHub-CMS",
				newMasterTree.Sha,
				master.Object.Sha,
				repoOwner);

			// update master
			Reference reference = await client.Git.Reference.Update(repoOwner,
				repoName,
				MasterBranchReference,
				new ReferenceUpdate(newMaster.Sha));
			if (newMaster.Sha != reference.Object.Sha)
			{
				throw new Exception(
					$"UpdateClient commit sha dont match newMaster.Sha={newMaster.Sha} " +
					$"reference.Sha={reference.Object.Sha}");
			}

			return reference;
		}

		/// <summary>
		/// Get content from GitHub
		/// </summary>
		/// <param name="client">GitHub client to use</param>
		/// <param name="repoName">repository name</param>
		/// <param name="repoOwner">Owner of repository</param>
		/// <param name="fileName">File in repo to update</param>
		/// <returns>File content</returns>
		public static async Task<RepositoryFileViewModel> GetOriginContentForRepo(GitHubClient client,
			string repoName,
			string repoOwner,
			string fileName)
		{
			try
			{
				var content = (await client.Repository.Content.GetAllContents(repoOwner, repoName, fileName)).First();
				return new RepositoryFileViewModel(fileName, content.Content, content.HtmlUrl);
			}
			catch (Exception e)
			{
				Trace.TraceError($"Error getting repo content {e}");
				return new RepositoryFileViewModel(fileName, "internal server error", "");
			}
		}

		static async Task<Commit> CreateCommit(GitHubClient client,
			string repoName,
			string message,
			string sha,
			string parent,
			string username)
		{
			var newCommit = new NewCommit(message, sha, parent);
			return await client.Git.Commit.Create(username, repoName, newCommit);
		}

		static async Task<TreeResponse> CreateTree(GitHubClient client,
			string repoName,
			IDictionary<string, string> treeContents,
			string username)
		{
			var collection = new List<NewTreeItem>();

			foreach (var c in treeContents)
			{
				var baselineBlob = new NewBlob
				{
					Content = c.Value,
					Encoding = EncodingType.Utf8
				};
				var baselineBlobResult = await client.Git.Blob.Create(username, repoName, baselineBlob);

				collection.Add(new NewTreeItem
				{
					Type = TreeType.Blob,
					Mode = FileMode.File,
					Path = c.Key,
					Sha = baselineBlobResult.Sha
				});
			}

			var newTree = new NewTree();
			foreach (var item in collection)
			{
				newTree.Tree.Add(item);
			}

			return await client.Git.Tree.Create(username, repoName, newTree);
		}
	}
}
