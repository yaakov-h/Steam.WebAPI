using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CodingRange.Steam.WebAPI
{
	public class GenericResponse<TResult>
	{
		[JsonProperty("response")]
		public TResult Response { get; set; }
	}
}
