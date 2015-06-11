using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using YamlDotNet.Serialization;

namespace DesuraDumper
{
	public class KeysView
	{
		public string Name {get;set;}
		public List<NameKeyStringTuple> Keys {get;set;}

		public KeysView ()
		{
			Keys = new List<NameKeyStringTuple>();
		}

		public static List<KeysView> CreateCollectionFromProducts(List<ProductInfo> products, string regexString = "*")
		{
			List<KeysView> views = new List<KeysView>();
			Regex regex = new Regex (regexString);

			foreach (ProductInfo product in products)
			{
				if (product.Keys.Count == 0)
					continue;
				List<NameKeyStringTuple> keys = new List<NameKeyStringTuple> (product.Keys.Where(k => regex.IsMatch(k.Name)));
				if (keys.Count == 0)
					continue;
				KeysView view = new KeysView ();
				view.Name = product.Name;
				view.Keys = keys;
				views.Add (view);
			}

			return views;
		}

		public static void SerializeCollection(List<KeysView> infos, TextWriter output)
		{
			Serializer serializer = new Serializer();
			serializer.Serialize(output, infos);
		}

		public static List<KeysView> DeserializeCollection(TextReader input)
		{
			Deserializer deserializer = new Deserializer();
			return deserializer.Deserialize<List<KeysView>>(input);
		}

	}
}

