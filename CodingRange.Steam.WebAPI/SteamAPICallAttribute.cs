using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CodingRange.Steam.WebAPI
{
	public class SteamAPICallAttribute : Attribute
	{
		public SteamAPICallAttribute(string name, int version, APIMethod method = APIMethod.Get)
		{
			this.Name = name;
			this.Version = version;
			this.Method = method;
		}

		public string Name { get; set; }
		public int Version { get; set; }
		public APIMethod Method { get; set; }
	}
}
