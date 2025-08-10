using Mafi;
using Mafi.Base;
using Mafi.Core;
using Mafi.Core.Game;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;
using System;
using System.Collections.Generic;

namespace CaptainOfData
{

	public sealed class CaptainOfDataMod : IMod
	{

		public string Name => "CaptainOfData";
		public int Version => 2;
		public bool IsUiOnly => false;
		public Option<IConfig> ModConfig => Option.None;

		public CaptainOfDataMod(CoreMod coreMod, BaseMod baseMod)
		{
		}

		public void RegisterPrototypes(ProtoRegistrator registrator)
		{
		}

		public void RegisterDependencies(DependencyResolverBuilder depBuilder, ProtosDb protosDb, bool gameWasLoaded)
		{
		}

		public void EarlyInit(DependencyResolver resolver)
		{
		}

		public void Initialize(DependencyResolver resolver, bool gameWasLoaded)
		{
			ApplicationConfig config = ApplicationConfigSerializer.LoadSettings();
			Log.Info("CaptainOfDataMod loaded");

			List<DataExtractor> extractors = new List<DataExtractor>();
			extractors.Add(new ProductDataExtractor(resolver, config));
			extractors.Add(new MachineDataExtractor(resolver, config));
			extractors.Add(new ReactorDataExtractor(resolver, config));

			foreach (DataExtractor extractor in extractors)
			{
				using (extractor)
				{
					try
					{
						extractor.ExtractData();
					}
					catch (Exception e)
					{
						Log.Error($"{extractor.GetType().Name}: {e.Message}\n{e.StackTrace}");
					}
				}
			}
		}

	}
}
