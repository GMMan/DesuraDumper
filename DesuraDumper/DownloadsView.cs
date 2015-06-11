using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace DesuraDumper
{
	public class DownloadsView
	{
		public string Name {get;set;}
		public List<NamePlatformStringTuple> Downloads {get;set;}
		public int BranchId {get;set;}
		public string Slug {get;set;}

		public DownloadsView ()
		{
			Downloads = new List<NamePlatformStringTuple>();
		}

		public static List<DownloadsView> CreateCollectionFromProducts(List<ProductInfo> products)
		{
			List<DownloadsView> views = new List<DownloadsView> ();
			foreach (ProductInfo product in products)
			{
				if (product.Downloads.Count == 0)
					continue;
				DownloadsView view = new DownloadsView ();
				view.Name = product.Name;
				view.Downloads = new List<NamePlatformStringTuple> (product.Downloads);
				view.BranchId = product.BranchId;
				view.Slug = product.Slug;
				views.Add (view);
			}

			return views;
		}

		public static void SerializeCollection(List<DownloadsView> infos, TextWriter output)
		{
			Serializer serializer = new Serializer();
			serializer.Serialize(output, infos);
		}

		public static List<DownloadsView> DeserializeCollection(TextReader input)
		{
			Deserializer deserializer = new Deserializer();
			return deserializer.Deserialize<List<DownloadsView>>(input);
		}

	}
}

