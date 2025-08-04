using Mafi;
using Mafi.Localization;
using Newtonsoft.Json;
using System;

namespace CaptainOfData
{
	internal abstract class WriteConverter<T> : JsonConverter<T>
	{
		public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException("This converter only supports serialization");
		}
	}

	internal class CoiString
	{
		public readonly string id;
		public readonly string text;

		public CoiString(LocStr str)
		{
			id = str.Id;
			text = str.TranslatedString;
		}
	}

	internal class CoiFix32Serializer : WriteConverter<CoiFix32>
	{
		public override void WriteJson(JsonWriter writer, CoiFix32 value, JsonSerializer serializer)
		{
			Fix32 number = value.number;
			if (number.IsInteger)
			{
				writer.WriteValue(number.IntegerPart);
			}
			else
			{
				writer.WriteValue(number.ToDouble());
			}
		}
	}

	[JsonConverter(typeof(CoiFix32Serializer))]
	internal class CoiFix32
	{
		public readonly Fix32 number;

		public CoiFix32(Fix32 number)
		{
			this.number = number;
		}
	}
}
