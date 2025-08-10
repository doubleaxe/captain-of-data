using Mafi;
using Mafi.Core;
using Mafi.Core.Entities.Static.Layout;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.NuclearReactors;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Maintenance;
using Mafi.Core.Prototypes;
using System.Collections.Generic;
using System.Linq;
using static Mafi.Core.Factory.NuclearReactors.NuclearReactorProto;

namespace CaptainOfData.Exctractor.Mappers
{
	internal class ProductQuantityData
	{
		public readonly string Id;
		public readonly int Quantity;

		public ProductQuantityData(RecipeProduct product)
		{
			Id = product.Product.Id.ToString();
			Quantity = product.Quantity.Value;
		}

		public ProductQuantityData(ProductQuantity product)
		{
			Id = product.Product.Id.ToString();
			Quantity = product.Quantity.Value;
		}
	}

	internal class ProductQuantityFloatData
	{
		public readonly string Id;
		public readonly Fix32 Quantity;

		public ProductQuantityFloatData(MaintenanceCosts costs)
		{
			Id = costs.Product.Id.ToString();
			Quantity = costs.MaintenancePerMonth.Value;
		}
	}

	internal class RecipeData
	{
		public readonly string Id;
		public readonly Proto.Str Name;
		public readonly int DurationTicks;
		public readonly Fix32 Duration;
		public readonly ProductQuantityData[] Inputs = new ProductQuantityData[0];
		public readonly ProductQuantityData[] Outputs = new ProductQuantityData[0];

		public RecipeData(IRecipeForUi recipe)
		{
			Id = recipe.Id.ToString();
			if (recipe is RecipeProto)
			{
				Name = ((RecipeProto)recipe).Strings;
			}
			DurationTicks = recipe.Duration.Ticks;
			Duration = recipe.Duration.Seconds;

			Inputs = recipe.AllUserVisibleInputs.ToLyst(p => new ProductQuantityData(p)).ToArray();
			Outputs = recipe.AllUserVisibleOutputs.ToLyst(p => new ProductQuantityData(p)).ToArray();
		}
	}

	internal class MachineData
	{
		public readonly string Id;
		public readonly Proto.Str Name;
		public readonly int Workers;
		public readonly ProductQuantityFloatData MaintenanceCost;
		public int ElectricityConsumed;
		public int ElectricityGenerated;
		public int ComputingConsumed;
		public int ComputingGenerated;
		public readonly RecipeData[] Recipes = new RecipeData[0];

		public MachineData(MachineProto machine) : this(machine, machine.Recipes.AsEnumerable().Cast<IRecipeForUi>())
		{
			ElectricityGenerated = machine.ElectricityConsumed.Value;
			ComputingConsumed = machine.ComputingConsumed.Value;
		}

		public MachineData(LayoutEntityProto machine, IEnumerable<IRecipeForUi> machineRecipes)
		{
			Id = machine.Id.ToString();
			Name = machine.Strings;
			Workers = machine.Costs.Workers;
			MaintenanceCost = new ProductQuantityFloatData(machine.Costs.Maintenance);

			if (machineRecipes != null)
			{
				Recipes = machineRecipes.Select((r, index) => new RecipeData(r)).ToArray();
			}
		}
	}

	internal class ReactorEnrichmentStepData
	{
		public readonly Percent FuelMultiplier;
		public readonly int BreedingRatio;
		public readonly int SteamReductionDiv;

		public ReactorEnrichmentStepData(EnrichmentStepData data)
		{
			FuelMultiplier = data.FuelMultiplier;
			BreedingRatio = data.BreedingRatio;
			SteamReductionDiv = data.SteamReductionDiv;
		}
	}

	internal class ReactorEnrichmentData
	{
		public readonly string InputProduct;
		public readonly string OutputProduct;
		public readonly Fix32 ProcessedPerLevel;
		public readonly Fix32 BuffersCapacity;
		public readonly int DefaultEnrichmentStep;
		public readonly ReactorEnrichmentStepData[] EnrichmentSteps;

		public ReactorEnrichmentData(NuclearReactorProto.EnrichmentData data)
		{
			InputProduct = data.InputProduct.Id.ToString();
			OutputProduct = data.OutputProduct.Id.ToString();
			ProcessedPerLevel = data.ProcessedPerLevel.Value;
			BuffersCapacity = data.BuffersCapacity.Value;
			DefaultEnrichmentStep = data.DefaultEnrichmentStep;
			EnrichmentSteps = data.EnrichmentSteps.AsEnumerable().Select((d, index) => new ReactorEnrichmentStepData(d)).ToArray();
		}
	}

	internal class ReactorData : MachineData
	{
		public readonly int MaxPowerLevel;
		public readonly ProductQuantityData WaterInPerPowerLevel;
		public readonly ProductQuantityData SteamOutPerPowerLevel;
		public readonly int DurationTicks;
		public readonly Fix32 Duration;
		public readonly Fix32 FuelCapacity;
		public readonly Fix32 MinFuelToOperate;
		public readonly string CoolantIn;
		public readonly string CoolantOut;
		public readonly ReactorEnrichmentData Enrichment;

		public ReactorData(NuclearReactorProto machine) : base(machine, machine.Recipes.AsEnumerable())
		{
			ComputingConsumed = machine.ComputingConsumed.Value;
			MaxPowerLevel = machine.MaxPowerLevel;
			WaterInPerPowerLevel = new ProductQuantityData(machine.WaterInPerPowerLevel);
			SteamOutPerPowerLevel = new ProductQuantityData(machine.SteamOutPerPowerLevel);
			DurationTicks = machine.ProcessDuration.Ticks;
			Duration = machine.ProcessDuration.Seconds;
			FuelCapacity = machine.FuelCapacity.Value;
			MinFuelToOperate = machine.MinFuelToOperate.Value;
			CoolantIn = machine.CoolantIn.Id.ToString();
			CoolantOut = machine.CoolantOut.Id.ToString();

			NuclearReactorProto.EnrichmentData enrichmentData = machine.Enrichment.ValueOrNull;
			if (enrichmentData != null)
			{
				Enrichment = new ReactorEnrichmentData(enrichmentData);
			}
		}
	}
}
