
using Mafi;
using Mafi.Core.Prototypes;
using Mafi.Unity;
using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

namespace CaptainOfData
{
	internal abstract class DataExtractor : IDisposable
	{
		protected readonly DependencyResolver _resolver;
		protected readonly ProtosDb _protosDb;
		protected readonly AssetsDb _assetsDb;
		protected readonly ApplicationConfig _settings;

		private StreamWriter _fileWriter;
		protected JsonTextWriter _jsonWriter;
		private StreamWriter _dumpWriter;
		private ObjectDumper _objectDumper;
		private readonly string _imagesPath;
		private bool _imagesPathCreated = false;

		public DataExtractor(DependencyResolver resolver, ApplicationConfig settings, string baseFileName)
		{
			this._resolver = resolver;
			ProtosDb protosDb = resolver.Resolve<ProtosDb>();
			AssetsDb assetsDb = resolver.Resolve<AssetsDb>();
			this._protosDb = protosDb;
			this._assetsDb = assetsDb;
			this._settings = settings;
			this._fileWriter = new StreamWriter(Path.Combine(settings.OutputFolder, baseFileName + ".json"));
			this._fileWriter.NewLine = "\n";
			this._jsonWriter = new JsonTextWriter(this._fileWriter);
			this._jsonWriter.Formatting = Formatting.Indented;
			if (settings.DebugDump != null)
			{
				this._dumpWriter = new StreamWriter(Path.Combine(settings.OutputFolder, baseFileName + "-dump.txt"));
				this._dumpWriter.NewLine = "\n";

				switch (settings.DebugDump)
				{
					case DebugDumpType.json:
						_objectDumper = new ObjectDumperJson(_dumpWriter, settings.DebugDumpMaxDepth);
						break;
					case DebugDumpType.txt:
						_objectDumper = new ObjectDumperTxt(_dumpWriter, settings.DebugDumpMaxDepth);
						break;
				}
			}
			_imagesPath = Path.Combine(settings.OutputFolder, baseFileName + "-images");
		}

		public abstract void ExtractData();

		protected void DumpObject(string name, object element)
		{
			if (_objectDumper != null)
			{
				_objectDumper.DumpObject(name, element);
			}
		}

		protected void DumpImage(string name, Texture2D image)
		{
			byte[] bytes;
			if (image.isReadable)
			{
				bytes = image.EncodeToPNG();
			}
			else
			{
				RenderTexture rt = RenderTexture.GetTemporary(
					image.width,
					image.height,
					0,
					RenderTextureFormat.ARGB32
				);
				RenderTexture currentActiveRT = RenderTexture.active;
				RenderTexture.active = rt;
				Graphics.Blit(image, rt);

				Texture2D readableTexture = new Texture2D(
					rt.width,
					rt.height,
					TextureFormat.RGBA32,
					false,
					true
				);
				readableTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
				readableTexture.Apply();
				RenderTexture.active = currentActiveRT;
				RenderTexture.ReleaseTemporary(rt);
				bytes = readableTexture.EncodeToPNG();
			}

			if (!_imagesPathCreated)
			{
				Directory.CreateDirectory(_imagesPath);
				_imagesPathCreated = true;
			}
			File.WriteAllBytes(Path.Combine(_imagesPath, name + ".png"), bytes);
		}

		public void Close()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (_jsonWriter != null)
			{
				_jsonWriter.Close();
				_jsonWriter = null;
			}
			if (_fileWriter != null)
			{
				_fileWriter.Close();
				_fileWriter.Dispose();
				_fileWriter = null;
			}
			if (_objectDumper != null)
			{
				_objectDumper.Dispose();
				_objectDumper = null;
			}
			if (_dumpWriter != null)
			{
				_dumpWriter.Close();
				_dumpWriter.Dispose();
				_dumpWriter = null;
			}
		}
	}
}
