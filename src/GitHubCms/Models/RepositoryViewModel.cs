using System.Collections.Generic;

namespace GitHubCms.Models
{
	public class RepositoryViewModel
	{
		public RepositoryViewModel(long repoId,
			string repoName,
			IEnumerable<RepositoryFileViewModel> repositoryFiles,
			string referrer,
			string reason,
			string user)
		{
			RepoId = repoId;
			RepoName = repoName;
			RepositoryFiles = repositoryFiles;
			Referrer = referrer;
			Reason = reason;
			User = user;
		}

		public RepositoryViewModel()
		{

		}

		public string RepoName { get; set; }
		public string Reason { get; set; }
		public string Referrer { get; set; }
		public string User { get; set; }
		public long RepoId { get; set; }
		public IEnumerable<RepositoryFileViewModel> RepositoryFiles { get; set; }
	}
}