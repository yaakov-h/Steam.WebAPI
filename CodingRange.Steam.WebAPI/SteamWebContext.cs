using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodingRange.Steam.WebAPI
{
	public class SteamWebContext
	{
		public SteamWebContext(string apiKey)
		{
			this.apiKey = apiKey;
		}

		readonly string apiKey;

		public TInterface GetInterface<TInterface>()
		{
			return JITEngine.GetInterface<TInterface>(apiKey);
		}
	}
}
