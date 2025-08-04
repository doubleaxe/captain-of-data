using Mafi;
using Mafi.Core.Products;
using System.Collections.Generic;

namespace CaptainOfData
{
	internal class ProductDataExtractor : DataExtractor
	{
		public ProductDataExtractor(DependencyResolver resolver, ApplicationConfig settings) : base(resolver, settings, "products")
		{
		}

		public override void ExtractData()
		{
			IEnumerable<ProductProto> products = _protosDb.All<ProductProto>();
			_jsonWriter.WriteStartArray();
			foreach (ProductProto product in products)
			{
				_jsonWriter.WriteValue(product.Id.ToString());

				DumpImage(product.Id.ToString(), _assetsDb.GetSharedTexture(product.IconPath));
				DumpObject(product.Id.ToString(), product);
			}
			_jsonWriter.WriteEndArray();
		}
	}
}
