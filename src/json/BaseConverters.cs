using Newtonsoft.Json;
using System;

namespace CaptainOfData.Json
{
	internal abstract class WriteConverter : JsonConverter
	{
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException("This converter only supports serialization");
		}
	}

	internal abstract class WriteConverter<T> : JsonConverter<T>
	{
		public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException("This converter only supports serialization");
		}
	}

	internal class DelegateConverter : WriteConverter
	{
		private readonly Func<Type, bool> _canConvert;
		private readonly Action<JsonWriter, Object, JsonSerializer> _writeJson;

		private DelegateConverter(Func<Type, bool> canConvert, Action<JsonWriter, Object, JsonSerializer> writeJson)
		{
			_canConvert = canConvert;
			_writeJson = writeJson;
		}

		public override bool CanConvert(Type objectType) => _canConvert(objectType);

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => _writeJson(writer, value, serializer);

		public static DelegateConverter Create(Func<Type, bool> canConvert, Action<JsonWriter, Object, JsonSerializer> writeJson) => new DelegateConverter(canConvert, writeJson);
	}

	internal class DelegateConverter<T> : WriteConverter<T>
	{
		private readonly Action<JsonWriter, T, JsonSerializer> _writeJson;

		private DelegateConverter(Action<JsonWriter, T, JsonSerializer> writeJson)
		{
			_writeJson = writeJson;
		}

		public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer) => _writeJson(writer, (T)value, serializer);

		public static DelegateConverter<T> Create(Action<JsonWriter, T, JsonSerializer> writeJson) => new DelegateConverter<T>(writeJson);
	}

}
