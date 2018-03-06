using System;
using System.Configuration;

namespace GitHubCms.Helpers
{
	public static class StartupHelpers
	{
		public static string ReadConfig(string config)
		{
			string configValue = ConfigurationManager.AppSettings[config];
			if (configValue.Contains("placeholder"))
				throw new Exception($"{config} config is not available. Update it in web.config");
			return configValue;
		}
	}
}