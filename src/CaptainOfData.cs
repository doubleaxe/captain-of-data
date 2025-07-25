using Mafi;
using Mafi.Base;
using Mafi.Core;
using Mafi.Core.Mods;

namespace CaptainOfData
{

	public sealed class CaptainOfDataMod : DataOnlyMod
	{

		public override string Name => "CaptainOfData";
		public override int Version => 2;


		public CaptainOfDataMod(CoreMod coreMod, BaseMod baseMod)
		{
			// You can use Log class for logging. These will be written to the log file
			// and can be also displayed in the in-game console with command `also_log_to_console`.
			Log.Info("ExampleMod: constructed");
		}


		public override void RegisterPrototypes(ProtoRegistrator registrator)
		{
			Log.Info("ExampleMod: registering prototypes");
		}

	}
}