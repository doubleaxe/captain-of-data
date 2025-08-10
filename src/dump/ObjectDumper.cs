using System;
using System.IO;

namespace CaptainOfData.Dump
{
	public abstract class ObjectDumper : IDisposable
	{
		protected StreamWriter _dumpWriter;

		public ObjectDumper(StreamWriter dumpWriter)
		{
			this._dumpWriter = dumpWriter;
		}

		virtual public void Dispose()
		{
		}

		public abstract void DumpObject(string name, object element);
	}
}
