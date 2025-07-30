using Mafi;
using Mafi.Core.Factory.Machines;
using System.Collections.Generic;

namespace CaptainOfData
{
	internal class MachineDataExtractor : DataExtractor
	{
		public MachineDataExtractor(DependencyResolver resolver, ApplicationConfig settings) : base(resolver, settings, "machines_and_buildings")
		{
		}

		public override void ExtractData()
		{
			//MaintenanceDepotProto is also MachineProto
			IEnumerable<MachineProto> machines = _protosDb.All<MachineProto>();
			_jsonWriter.WriteStartArray();
			foreach (MachineProto machine in machines)
			{
				_jsonWriter.WriteValue(machine.Id.ToString());

				DumpImage(machine.Id.ToString(), _assetsDb.GetSharedTexture(machine.IconPath));
				DumpObject(machine.Id.ToString(), machine);
			}
			_jsonWriter.WriteEndArray();
		}
	}
}
