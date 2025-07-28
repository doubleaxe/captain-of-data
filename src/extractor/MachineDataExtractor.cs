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
			IEnumerable<MachineProto> machines = protosDb.All<MachineProto>();
			jsonWriter.WriteStartArray();
			foreach (MachineProto machine in machines)
			{
				jsonWriter.WriteValue(machine.Id.ToString());

				DumpImage(machine.Id.ToString(), assetsDb.GetSharedTexture(machine.IconPath));
			}
			jsonWriter.WriteEndArray();
		}
	}
}
