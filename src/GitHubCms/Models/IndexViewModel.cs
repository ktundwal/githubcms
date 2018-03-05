using Octokit;
using System.Collections.Generic;

namespace GitHubCms.Models
{
	public class IndexViewModel
	{
		public IndexViewModel(IEnumerable<Repository> repositories)
		{
			Repositories = repositories;
		}

		public IEnumerable<Repository> Repositories { get; }
	}
}