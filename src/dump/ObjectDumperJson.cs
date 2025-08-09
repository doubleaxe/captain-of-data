using Mafi;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CaptainOfData.dump
{
	internal static class TypeUtils
	{
		public static bool IsAnyType(Type objectType, Type baseType)
		{
			if (objectType == null) return false;
			if (baseType.IsAssignableFrom(objectType)) return true;
			if (objectType.IsGenericType && (objectType.GetGenericTypeDefinition() == baseType)) return true;
			if (objectType.GetInterfaces().Any(i => i.IsGenericType && (i.GetGenericTypeDefinition() == baseType)))
			{
				return true;
			}
			return false;
		}
	}

	internal abstract class WriteConverter : JsonConverter
	{
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException("This converter only supports serialization");
		}
	}

	internal class MafiToStringConverter : WriteConverter
	{
		private static bool IsMafiToString(Type objectType)
		{
			// Mafi namespace contains all basic types
			// Fix32 Quantity Electricity Upoints RelTile3f RelTile3i, etc...
			// They are all basic values, but very noisy in json dump, but implement ToSting, so we check and write ToString
			if (objectType.Namespace != "Mafi") return false;

			MethodInfo ToString = objectType.GetMethod("ToString", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
			bool implementsToString = ToString.DeclaringType != typeof(object);
			return implementsToString;
		}

		public override bool CanConvert(Type objectType)
		{
			return IsMafiToString(objectType);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.WriteValue(value.ToString());
		}
	}

	internal class OtherToStringConverter : WriteConverter
	{
		private static Type[] _otherToStringTypes =
		{
			typeof(Mafi.Core.Entities.Static.Layout.LayoutTile),
		};

		private static bool IsOtherToString(Type objectType)
		{
			foreach (Type type in _otherToStringTypes)
			{
				if (TypeUtils.IsAnyType(objectType, type))
				{
					return true;
				}
			}
			return false;
		}

		public override bool CanConvert(Type objectType)
		{
			return IsOtherToString(objectType);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.WriteValue(value.ToString());
		}
	}

	internal class IgnoreTypeConverter : WriteConverter
	{
		private static Type[] _ignoreTypes =
		{
			typeof(Mafi.Core.Entities.Static.Layout.TerrainVertexRel)
		};

		private static bool IsIgnoreType(Type objectType)
		{
			foreach (Type type in _ignoreTypes)
			{
				if (TypeUtils.IsAnyType(objectType, type))
				{
					return true;
				}
			}
			return false;
		}

		public override bool CanConvert(Type objectType)
		{
			return IsIgnoreType(objectType);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.WriteValue("Skip");
		}
	}

	internal class MafiCollectionConverter : WriteConverter
	{
		private static bool IsEnumerable(Type objectType)
		{
			return TypeUtils.IsAnyType(objectType, typeof(IEnumerable)) || TypeUtils.IsAnyType(objectType, typeof(IEnumerable<>));
		}

		public override bool CanConvert(Type objectType)
		{
			if (IsEnumerable(objectType)) return false;
			// Mafi.Collections.ImmutableCollections.ImmutableArray
			MethodInfo AsEnumerable = objectType.GetMethod("AsEnumerable");
			if ((AsEnumerable != null) && IsEnumerable(AsEnumerable.ReturnType)) return true;
			// Mafi.Collections.HybridSet
			MethodInfo All = objectType.GetMethod("All");
			if ((All != null) && IsEnumerable(All.ReturnType)) return true;
			// Mafi.Collections.ImmutableCollections.ImmutableArray
			MethodInfo ToLyst = objectType.GetMethod("ToLyst");
			if ((ToLyst != null) && IsEnumerable(ToLyst.ReturnType)) return true;
			return false;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			Type objectType = value.GetType();
			MethodInfo AsEnumerable = objectType.GetMethod("AsEnumerable");
			if (AsEnumerable != null)
			{
				object enumerable = AsEnumerable.Invoke(value, new object[0]);
				serializer.Serialize(writer, enumerable);
				return;
			}
			MethodInfo All = objectType.GetMethod("All");
			if (All != null)
			{
				object enumerable = All.Invoke(value, new object[0]);
				serializer.Serialize(writer, enumerable);
				return;
			}
			MethodInfo ToLyst = objectType.GetMethod("ToLyst");
			if (ToLyst != null)
			{
				object enumerable = ToLyst.Invoke(value, new object[0]);
				// Lyst is too hard to write
				enumerable = ((IEnumerable)enumerable).Cast<object>().ToList();
				serializer.Serialize(writer, enumerable);
				return;
			}
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

	internal class DelegateConverter<T> : JsonConverter<T>
	{
		private readonly Action<JsonWriter, T, JsonSerializer> _writeJson;

		private DelegateConverter(Action<JsonWriter, T, JsonSerializer> writeJson)
		{
			_writeJson = writeJson;
		}

		public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException("This converter only supports serialization");
		}

		public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer) => _writeJson(writer, (T)value, serializer);

		public static DelegateConverter<T> Create(Action<JsonWriter, T, JsonSerializer> writeJson) => new DelegateConverter<T>(writeJson);
	}

	public class ObjectDumperJson : ObjectDumper
	{
		private JsonTextWriter _jsonDumpWriter;
		private JsonSerializer _jsonDumpSerializer;

		public ObjectDumperJson(StreamWriter dumpWriter, int maxDepth) : base(dumpWriter)
		{
			_jsonDumpWriter = new JsonTextWriter(this._dumpWriter);
			_jsonDumpWriter.Formatting = Formatting.Indented;
			_jsonDumpWriter.WriteStartArray();

			JsonSerializerSettings settings = new JsonSerializerSettings
			{
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
				Converters = new List<JsonConverter>
				{
					new IgnoreTypeConverter(),
					/*
					DelegateConverter.Create(
						(objectType) => TypeUtils.IsAnyType(objectType, typeof(Option<>)),
						(writer, value, serializer) =>
						{
							Type objectType = value.GetType();
							PropertyInfo ValueOrNull = objectType.GetProperty("ValueOrNull");
							object _value = ValueOrNull.GetValue(value);
							if(_value == null)
							{
								writer.WriteNull();
								return;
							}
							serializer.Serialize(writer, _value);
						}
					),
					*/
					DelegateConverter<Mafi.Core.Mods.IMod>.Create((writer, value, serializer) => writer.WriteValue(value.Name)),
					DelegateConverter<Mafi.Core.Prototypes.Proto.Str>.Create((writer, value, serializer) => {
						if(value.Name.TranslatedString == "")
						{
							writer.WriteValue("");
							return;
						}
						writer.WriteStartObject();
						writer.WritePropertyName("id");
						writer.WriteValue(value.Name.Id);
						writer.WritePropertyName("value");
						writer.WriteValue(value.Name.TranslatedString);
						writer.WriteEndObject();
					}),
					DelegateConverter.Create(
						(objectType) =>
						{
							// Proto.ID, IoPortShapeProto.ID, etc...
							if(objectType.Name != "ID") return false;
							FieldInfo Value = objectType.GetField("Value");
							if((Value == null) || (Value.FieldType != typeof(string))) return false;
							return true;
						},
						(writer, value, serializer) =>
						{
							Type objectType = value.GetType();
							FieldInfo Value = objectType.GetField("Value");
							object _value = Value.GetValue(value);
							if(_value == null)
							{
								writer.WriteNull();
								return;
							}
							writer.WriteValue(_value);
						}
					),
					new MafiCollectionConverter(),
					new OtherToStringConverter(),
					// must be last one
					new MafiToStringConverter(),
				},
				Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
				{
					Log.Debug($"Cannot serialize {sender.GetType().FullName}");
					args.ErrorContext.Handled = true;
				}
			};

			_jsonDumpSerializer = JsonSerializer.Create(settings);
		}

		public override void Dispose()
		{
			if (_jsonDumpWriter != null)
			{
				try
				{
					_jsonDumpWriter.WriteEndArray();
				}
				catch (Exception) { }
				_jsonDumpWriter.Close();
				_jsonDumpWriter = null;
			}
		}

		public override void DumpObject(string name, object element)
		{
			_jsonDumpWriter.WriteStartObject();
			_jsonDumpWriter.WritePropertyName("dump-id");
			_jsonDumpWriter.WriteValue(name);
			_jsonDumpWriter.WritePropertyName("dump-value");
			_jsonDumpSerializer.Serialize(_jsonDumpWriter, element);
			_jsonDumpWriter.WriteEndObject();
		}
	}
}
