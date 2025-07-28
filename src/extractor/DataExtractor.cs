
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
		protected readonly DependencyResolver resolver;
		protected readonly ProtosDb protosDb;
		protected readonly AssetsDb assetsDb;
		protected readonly ApplicationConfig settings;

		private StreamWriter fileWriter;
		protected JsonTextWriter jsonWriter;
		protected StreamWriter dumpWriter;
		private readonly string imagesPath;
		private bool imagesPathCreated = false;

		private bool disposed = false;

		public DataExtractor(DependencyResolver resolver, ApplicationConfig settings, string baseFileName)
		{
			this.resolver = resolver;
			ProtosDb protosDb = resolver.Resolve<ProtosDb>();
			AssetsDb assetsDb = resolver.Resolve<AssetsDb>();
			this.protosDb = protosDb;
			this.assetsDb = assetsDb;
			this.settings = settings;
			this.fileWriter = new StreamWriter(Path.Combine(settings.OutputFolder, baseFileName + ".json"));
			this.fileWriter.NewLine = "\n";
			this.jsonWriter = new JsonTextWriter(this.fileWriter);
			this.jsonWriter.Formatting = Formatting.Indented;
			this.jsonWriter.Indentation = 2;
			this.jsonWriter.IndentChar = ' ';
			if (settings.DebugDump)
			{
				this.dumpWriter = new StreamWriter(Path.Combine(settings.OutputFolder, baseFileName + "-dump.txt"));
				this.dumpWriter.NewLine = "\n";
			}
			imagesPath = Path.Combine(settings.OutputFolder, baseFileName + "-images");
		}

		public abstract void ExtractData();

		protected void DumpObject(string name, object element)
		{
			if (dumpWriter == null)
				return;
			var content = ObjectDumper.Dump(element);
			dumpWriter.WriteLine(name);
			dumpWriter.WriteLine("");
			dumpWriter.WriteLine(content);
			dumpWriter.WriteLine("");
			dumpWriter.WriteLine("");
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

			if (!imagesPathCreated)
			{
				Directory.CreateDirectory(imagesPath);
				imagesPathCreated = true;
			}
			File.WriteAllBytes(Path.Combine(imagesPath, name + ".png"), bytes);
		}

		public void Close()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			if (jsonWriter != null)
			{
				jsonWriter.Close();
				jsonWriter = null;
			}
			if (fileWriter != null)
			{
				fileWriter.Close();
				fileWriter.Dispose();
				fileWriter = null;
			}
			if (dumpWriter != null)
			{
				dumpWriter.Close();
				dumpWriter.Dispose();
				dumpWriter = null;
			}

			disposed = true;
		}
	}
}
