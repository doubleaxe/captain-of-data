using CaptainOfData.Exctractor.Mappers;
using Mafi;
using Mafi.Core.Factory.NuclearReactors;

namespace CaptainOfData
{
	internal class ReactorDataExtractor : DataExtractor
	{
		public ReactorDataExtractor(DependencyResolver resolver, ApplicationConfig settings) : base(resolver, settings, "reactor")
		{
		}

		public override void ExtractData()
		{
			ExtractEnumerable(_protosDb.All<NuclearReactorProto>(), ExtractReactor);
		}

		private void ExtractReactor(NuclearReactorProto machine)
		{
			ExtractImage(machine.Id.ToString(), _assetsDb.GetSharedTexture(machine.IconPath));
			DumpObject(machine.Id.ToString(), machine);

			ReactorData machineData = new ReactorData(machine);
			_jsonSerializer.Serialize(_jsonWriter, machineData);

		}
	}
}
