using Mafi;
using Mafi.Base;
using Mafi.Core;
using Mafi.Core.Game;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;
using Mafi.Unity;
using System;

namespace CaptainOfData
{

	public sealed class CaptainOfDataMod : IMod
	{

		public string Name => "CaptainOfData";
		public int Version => 2;
		public bool IsUiOnly => false;
		public Option<IConfig> ModConfig => Option.None;

		private ApplicationConfig config;
		private DependencyResolver resolver;

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
			config = ApplicationConfigSerializer.LoadSettings();

			ProtosDb protosDb = resolver.Resolve<ProtosDb>();
			AssetsDb assetsDb = resolver.Resolve<AssetsDb>();
			Log.Info("CaptainOfDataMod loaded");
		}

	}
}
