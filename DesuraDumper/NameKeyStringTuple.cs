using System;

namespace DesuraDumper
{
	public class NameKeyStringTuple
	{
		public string Name {get; private set;}
		public string Key {get;private set;}

		public NameKeyStringTuple()
		{}

		public NameKeyStringTuple (string name, string key)
		{
			Name = name;
			Key = key;
		}
	}
}

