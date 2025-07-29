
using Mafi;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;

namespace CaptainOfData
{
	internal enum DebugDumpType
	{
		json,
		txt
	}

	internal class ApplicationConfig
	{
		public string OutputFolder { get; set; }
		public DebugDumpType? DebugDump { get; set; }

		public static ApplicationConfig defaultConfig()
		{
			ApplicationConfig defaultConfig = new ApplicationConfig();
			defaultConfig.OutputFolder = @"C:\Tmp";
			defaultConfig.DebugDump = null;
			return defaultConfig;
		}
	}

	static internal class ApplicationConfigSerializer
	{
		public static ApplicationConfig LoadSettings()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			string assemblyLocation = assembly.Location;
			string configFilePath = Path.Combine(Path.GetDirectoryName(assemblyLocation), "config.json");

			if (!File.Exists(configFilePath))
			{
				Log.Error($"config.json doesn't exist: {configFilePath}");
				return ApplicationConfig.defaultConfig();
			}

			try
			{
				string jsonContent = File.ReadAllText(configFilePath);
				ApplicationConfig settings = JsonConvert.DeserializeObject<ApplicationConfig>(jsonContent);

				if (settings != null)
				{
					return settings;
				}
				Log.Error("Failed to deserialize config.json into AppSettings. Check JSON format.");
			}
			catch (Exception ex)
			{
				Log.Error($"Error deserializing config.json: {ex.Message}");
			}
			return ApplicationConfig.defaultConfig();
		}
	}

}
