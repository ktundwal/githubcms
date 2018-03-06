namespace GitHubCms.Models
{
	public class RepositoryFileViewModel
	{
		public string Name { get; set; }
		public string Content { get; set; }
		public string GitHubUrl { get; set; }

		public RepositoryFileViewModel()
		{

		}

		public RepositoryFileViewModel(string name, string content, string gitHubUrl)
		{
			Name = name;
			Content = content;
			GitHubUrl = gitHubUrl;
		}
	}
}