using CaptainOfData.Exctractor.Mappers;
using Mafi;
using Mafi.Core.Factory.Machines;

namespace CaptainOfData
{
	internal class MachineDataExtractor : DataExtractor
	{
		public MachineDataExtractor(DependencyResolver resolver, ApplicationConfig settings) : base(resolver, settings, "machines_and_buildings")
		{
		}

		public override void ExtractData()
		{
			ExtractEnumerable(_protosDb.All<MachineProto>(), ExtractMachine);
		}

		private void ExtractMachine(MachineProto machine)
		{
			ExtractImage(machine.Id.ToString(), _assetsDb.GetSharedTexture(machine.IconPath));
			DumpObject(machine.Id.ToString(), machine);

			MachineData machineData = new MachineData(machine);
			_jsonSerializer.Serialize(_jsonWriter, machineData);

		}

	}
}
