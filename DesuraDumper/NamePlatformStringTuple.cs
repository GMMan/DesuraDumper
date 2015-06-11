using System;

namespace DesuraDumper
{
	public class NamePlatformStringTuple
	{
		public string Name {get; private set;}
		public string Platform {get;private set;}

		public NamePlatformStringTuple()
		{}

		public NamePlatformStringTuple (string name, string platform)
		{
			Name = name;
			Platform = platform;
		}
	}
}

