using Mafi;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Maintenance;
using System.Collections.Generic;
using System.Linq;

namespace CaptainOfData
{
	internal class MachineDataExtractor : DataExtractor
	{
		private class RecipeProductData
		{
			public string id;
			public CoiString name;
			public int quantity;

			public RecipeProductData(RecipeProduct product)
			{
				id = product.Product.Id.ToString();
				name = new CoiString(product.Product.Strings.Name);
				quantity = product.Quantity.Value;
			}
		}

		private class RecipeData
		{
			public string id;
			public CoiString name;
			public int duration_ticks;
			public CoiFix32 duration;
			public CoiFix32 duration_minutes;
			public RecipeProductData[] inputs = new RecipeProductData[0];
			public RecipeProductData[] outputs = new RecipeProductData[0];

			public RecipeData(IRecipeForUi recipe)
			{
				id = recipe.Id.ToString();
				duration_ticks = recipe.Duration.Ticks;
				duration = new CoiFix32(recipe.Duration.Seconds);
				duration_minutes = new CoiFix32(recipe.Duration.MinutesAsFix32);

				inputs = recipe.AllUserVisibleInputs.AsEnumerable().Select(p => new RecipeProductData(p)).ToArray();
				outputs = recipe.AllUserVisibleOutputs.AsEnumerable().Select(p => new RecipeProductData(p)).ToArray();
			}

			public RecipeData(RecipeProto recipe) : this((IRecipeForUi)recipe)
			{
				name = new CoiString(((RecipeProto)recipe).Strings.Name);
			}
		}

		private class MachineData
		{
			public string id;
			public CoiString name;
			public int workers;
			public string maintenance_cost_units;
			public CoiFix32 maintenance_cost_quantity;
			public int electricity_consumed;
			public int electricity_generated;
			public int computing_consumed;
			public int computing_generated;
			public RecipeData[] recipes = new RecipeData[0];

			public void SetMaintenance(MaintenanceCosts costs)
			{
				maintenance_cost_units = costs.Product.Id.ToString();
				maintenance_cost_quantity = new CoiFix32(costs.MaintenancePerMonth.Value);
			}

			public MachineData(MachineProto machine)
			{
				id = machine.Id.ToString();
				name = new CoiString(machine.Strings.Name);
				workers = machine.Costs.Workers;
				SetMaintenance(machine.Costs.Maintenance);
				electricity_consumed = machine.ElectricityConsumed.Value;
				computing_consumed = machine.ComputingConsumed.Value;

				recipes = machine.Recipes.AsEnumerable().Select((r, index) => new RecipeData(r)).ToArray();
			}
		}

		public MachineDataExtractor(DependencyResolver resolver, ApplicationConfig settings) : base(resolver, settings, "machines_and_buildings")
		{
		}

		public override void ExtractData()
		{
			_jsonWriter.WriteStartObject();
			_jsonWriter.WritePropertyName("machines_and_buildings");
			//MaintenanceDepotProto is also MachineProto
			IEnumerable<MachineProto> machines = _protosDb.All<MachineProto>();
			_jsonWriter.WriteStartArray();
			foreach (MachineProto machine in machines)
			{
				DumpMachine(machine);
			}
			_jsonWriter.WriteEndArray();
			_jsonWriter.WriteEndObject();
		}

		private void DumpMachine(MachineProto machine)
		{
			DumpImage(machine.Id.ToString(), _assetsDb.GetSharedTexture(machine.IconPath));
			DumpObject(machine.Id.ToString(), machine);

			MachineData machineData = new MachineData(machine);
			_jsonSerializer.Serialize(_jsonWriter, machineData);

		}

	}
}
