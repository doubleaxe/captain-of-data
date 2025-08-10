using Mafi;
using Newtonsoft.Json;

namespace CaptainOfData.Json
{
	internal class Fix32Converter : WriteConverter<Fix32>
	{
		public override void WriteJson(JsonWriter writer, Fix32 value, JsonSerializer serializer)
		{
			Fix32 number = value;
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

	internal class PercentConverter : WriteConverter<Percent>
	{
		public override void WriteJson(JsonWriter writer, Percent value, JsonSerializer serializer)
		{
			writer.WriteValue(value.RawValue);
		}
	}

	internal class LocStrConverter : WriteConverter<Mafi.Localization.LocStr>
	{
		public override void WriteJson(JsonWriter writer, Mafi.Localization.LocStr value, JsonSerializer serializer)
		{
			writer.WriteStartObject();
			writer.WritePropertyName("Id");
			writer.WriteValue(value.Id);
			writer.WritePropertyName("Text");
			writer.WriteValue(value.TranslatedString);
			writer.WriteEndObject();
		}
	}

	internal class StringsConverter : WriteConverter<Mafi.Core.Prototypes.Proto.Str>
	{
		public override void WriteJson(JsonWriter writer, Mafi.Core.Prototypes.Proto.Str value, JsonSerializer serializer)
		{
			serializer.Serialize(writer, value.Name);
		}
	}
}
