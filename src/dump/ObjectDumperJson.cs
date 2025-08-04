using Mafi;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace CaptainOfData.dump
{
	internal abstract class WriteConverter : JsonConverter
	{
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException("This converter only supports serialization");
		}
	}

	// don't use ReferenceLoopHandling.Ignore with this serializer
	// it works slightly weird way, and must serialize same object twice
	// it will perform ReferenceLoopHandling.Ignore by itself
	// it should be only one converter for everything, otherwise call chain is broken
	internal class DepthLimitingConverter : WriteConverter
	{
		private readonly int _maxDepth;
		private readonly IContractResolver _contractResolver;

		private enum CurrentState
		{
			Waiting,
			Serializing
		}
		private CurrentState _currentState;
		private Stack<object> _currentStack = new Stack<object>();
		private static Type[] _otherToStringTypes =
		{
		};

		public DepthLimitingConverter(JsonSerializer jsonSerializer, int maxDepth)
		{
			_contractResolver = jsonSerializer.ContractResolver;
			_maxDepth = maxDepth;
			_currentState = CurrentState.Waiting;
		}

		public void Reset()
		{
			_currentState = CurrentState.Waiting;
			_currentStack.Clear();
		}

		private static bool IsEnumerable(Type objectType)
		{
			if (objectType == null) return false;
			if (typeof(IEnumerable).IsAssignableFrom(objectType)) return true;
			if (objectType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
			{
				return true;
			}
			return false;
		}

		private static bool IsMafiToString(Type objectType)
		{
			// Mafi namespace contains all basic types
			// Fix32 Quantity Electricity Upoints RelTile3f RelTile3i, etc...
			// They are all basic values, but very noisy in json dump, but implement ToSting, so we check and write ToString
			if (objectType.Namespace != "Mafi") return false;

			MethodInfo toStringMethod = objectType.GetMethod("ToString", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
			bool implementsToString = toStringMethod.DeclaringType != typeof(object);
			return implementsToString;
		}

		private static bool IsOtherToString(Type objectType)
		{
			foreach (Type type in _otherToStringTypes)
			{
				if (objectType.IsAssignableFrom(type))
				{
					return true;
				}
			}
			return false;
		}

		// this is based totally on assumption that CanConvert is called exactly once per conversion srep
		// it is true for current json, but may be changed later
		public override bool CanConvert(Type objectType)
		{
			JsonContract contract = _contractResolver.ResolveContract(objectType);
			bool canConvert = canConvert = (contract is JsonContainerContract);
			if (!canConvert) return false;

			if (IsMafiToString(objectType) || IsOtherToString(objectType)) return true;

			SerializationCallback onSerializing = OnSerializing;
			if (!contract.OnSerializingCallbacks.Any(existingCallback => existingCallback.Method == onSerializing.Method))
			{
				contract.OnSerializingCallbacks.Add(onSerializing);
			}
			SerializationCallback onSerialized = OnSerialized;
			if (!contract.OnSerializedCallbacks.Any(existingCallback => existingCallback.Method == onSerialized.Method))
			{
				contract.OnSerializingCallbacks.Add(onSerialized);
			}

			if (_currentState == CurrentState.Serializing)
			{
				return false;
			}
			return true;
		}

		private void OnSerializing(object o, StreamingContext context)
		{
			// we need to do this in OnSerializing handler, because CanConvert is not always executed
			// and this is fired always, when base implementation is about to serialize
			if (_currentState == CurrentState.Serializing)
			{
				_currentState = CurrentState.Waiting;
			}
		}

		private void OnSerialized(object o, StreamingContext context)
		{

		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			// this can be called in Serializing state, CanConvert is executed not every time, let's be careful
			if (_currentState != CurrentState.Waiting)
			{
				return;
			}

			if (value == null)
			{
				writer.WriteNull();
				return;
			}

			Type objectType = value.GetType();
			if (IsMafiToString(objectType) || IsOtherToString(objectType))
			{
				writer.WriteValue(value.ToString());
				return;
			}

			object toSerialize = value;
			MethodInfo asEnumerable = objectType.GetMethod("AsEnumerable");
			if ((asEnumerable != null) && IsEnumerable(asEnumerable.ReturnType))
			{
				// Mafi.Collections.ImmutableCollections.ImmutableArray
				object enumerable = asEnumerable.Invoke(value, new object[0]);
				toSerialize = enumerable;
			}

			foreach (object alreadySerializing in _currentStack)
			{
				// using identity compare
				if (ReferenceEquals(alreadySerializing, toSerialize))
				{
					// ReferenceLoopHandling.Ignore
					return;
				}
			}

			int currentDepth = _currentStack.Count;
			if (currentDepth >= _maxDepth)
			{
				writer.WriteValue($"[Max depth {_maxDepth} reached]");
				return;
			}

			_currentState = CurrentState.Serializing;
			_currentStack.Push(toSerialize);
			try
			{
				Log.Debug($"Serializing -> {currentDepth} : {objectType}{(objectType == toSerialize.GetType() ? "" : " => " + toSerialize.GetType())}");
				serializer.Serialize(writer, toSerialize);
			}
			finally
			{
				_currentStack.Pop();
			}
		}
	}

	public class ObjectDumperJson : ObjectDumper
	{
		private JsonTextWriter _jsonDumpWriter;
		private JsonSerializer _jsonDumpSerializer;
		private DepthLimitingConverter _converter;

		public ObjectDumperJson(StreamWriter dumpWriter, int maxDepth) : base(dumpWriter)
		{
			_jsonDumpWriter = new JsonTextWriter(this._dumpWriter);
			_jsonDumpWriter.Formatting = Formatting.Indented;
			_jsonDumpWriter.WriteStartArray();

			_jsonDumpSerializer = new JsonSerializer();
			_jsonDumpSerializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
			_jsonDumpSerializer.Error += delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
			{
				Log.Debug($"Cannot serialize {sender.GetType().FullName}");
				args.ErrorContext.Handled = true;
			};

			_converter = new DepthLimitingConverter(_jsonDumpSerializer, maxDepth);
			_jsonDumpSerializer.Converters.Add(_converter);
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
			_converter.Reset();

			_jsonDumpWriter.WriteStartObject();
			_jsonDumpWriter.WritePropertyName("dump-id");
			_jsonDumpWriter.WriteValue(name);
			_jsonDumpWriter.WritePropertyName("dump-value");
			_jsonDumpSerializer.Serialize(_jsonDumpWriter, element);
			_jsonDumpWriter.WriteEndObject();
		}
	}
}
