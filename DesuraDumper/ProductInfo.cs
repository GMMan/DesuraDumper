using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;

namespace DesuraDumper
{
	public class ProductInfo
	{
		public string Name {get;set;}
		public List<NamePlatformStringTuple> Downloads {get;set;}
		public List<NameKeyStringTuple> Keys {get;set;}
		public int GameId {get;set;}
		public int BranchId {get;set;}
		public string Slug {get;set;}

		public ProductInfo()
		{
			Downloads = new List<NamePlatformStringTuple>();
			Keys = new List<NameKeyStringTuple>();
		}

		public static List<ProductInfo> CreateFromCollection(JArray coll)
		{
			List<ProductInfo> infos = new List<ProductInfo>();

			foreach (JArray o in coll)
			{
				ProductInfo info = new ProductInfo();
				if (string.IsNullOrEmpty((string)o[24]))
				{
					Console.Error.WriteLine("Game {0} ({1}) does not have branch ID, skipping", o[0], o[1]);
					continue;
				}
				info.GameId = (int)o[0];
				info.BranchId = (int)o[24];
				info.Name = (string)o[1];
				info.Slug = (string)o[2];
				infos.Add(info);
			}

			return infos;
		}

		public static void SerializeCollection(List<ProductInfo> infos, TextWriter output)
		{
			Serializer serializer = new Serializer();
			serializer.Serialize(output, infos);
		}

		public static List<ProductInfo> DeserializeCollection(TextReader input)
		{
			Deserializer deserializer = new Deserializer();
			return deserializer.Deserialize<List<ProductInfo>>(input);
		}
	}
}

