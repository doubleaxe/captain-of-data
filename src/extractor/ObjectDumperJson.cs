using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;

namespace CaptainOfData
{
	internal class DepthLimitingContractResolver : DefaultContractResolver
	{
		private readonly int _maxDepth;
		private int _currentDepth = 0;

		public DepthLimitingContractResolver(int maxDepth)
		{
			this._maxDepth = maxDepth;
		}

		protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
		{
			JsonProperty property = base.CreateProperty(member, memberSerialization);

			if (_currentDepth >= _maxDepth)
			{
				property.ShouldSerialize = instance => false;
			}

			return property;
		}

		protected override JsonObjectContract CreateObjectContract(Type objectType)
		{
			JsonObjectContract contract = base.CreateObjectContract(objectType);

			contract.OnSerializingCallbacks.Add((o, context) => _currentDepth++);
			contract.OnSerializedCallbacks.Add((o, context) => _currentDepth--);

			return contract;
		}
	}

	internal class ObjectDumperJson : ObjectDumper
	{
		private JsonTextWriter _jsonDumpWriter;
		private JsonSerializer _jsonDumpSerializer;

		public ObjectDumperJson(StreamWriter dumpWriter, ApplicationConfig settings) : base(dumpWriter)
		{
			_jsonDumpWriter = new JsonTextWriter(this._dumpWriter);
			_jsonDumpWriter.Formatting = Formatting.Indented;

			_jsonDumpSerializer = new JsonSerializer();
			_jsonDumpSerializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
			_jsonDumpSerializer.ContractResolver = new DepthLimitingContractResolver(settings.DebugDumpMaxDepth);
		}

		public override void Dispose()
		{
			if (_jsonDumpWriter != null)
			{
				_jsonDumpWriter.Close();
				_jsonDumpWriter = null;
			}
		}

		public override void DumpObject(string name, object element)
		{
			_jsonDumpWriter.WriteStartObject();
			_jsonDumpWriter.WritePropertyName("id");
			_jsonDumpWriter.WriteValue(name);
			_jsonDumpWriter.WritePropertyName("value");
			_jsonDumpSerializer.Serialize(_jsonDumpWriter, element);
			_jsonDumpWriter.WriteEndObject();
		}
	}
}
