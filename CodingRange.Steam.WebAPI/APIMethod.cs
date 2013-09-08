using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodingRange.Steam.WebAPI
{
	public enum APIMethod
	{
		Get,

		[Obsolete("Post is not fully supported yet - JITEngine supports it, but not APIBase")]
		Post
	}
}
