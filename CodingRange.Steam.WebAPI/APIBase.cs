using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodingRange.Steam.WebAPI
{
	public class APIBase
	{
#if DEBUG
		private static string BaseURL = "http://api.steampowered.com";
#else
		private static string BaseURL = "https://api.steampowered.com";
#endif
		
		protected static TResult Run<TResult>(APIMethod method, string interfaceName, string name, int version, Dictionary<string, object> parameter)
		{
			return RunAsync<TResult>(method, interfaceName, name, version, parameter).Result;
		}

		protected static async Task<TResult> RunAsync<TResult>(APIMethod method, string interfaceName, string name, int version, Dictionary<string, object> parameter)
		{
			var url = BuildURL(interfaceName, name, version);

			string json;

			switch (method)
			{
				case APIMethod.Get:
					json = await RunGetAsync(url, parameter);
					break;

				case APIMethod.Post:
					throw new InvalidOperationException("What part of ObsoleteAttribute don't you understand?");
					////json = await RunPostAsync(url, parameter);
					////break;

				default:
					throw new ArgumentException("Unknown 'method' value '" + method + "'");
			}

			// Hacky casting ahoy! We know the type cast is safe, but the compiler doesn't.
			if (typeof(TResult) == typeof(string))
			{
				return (TResult)(object)json;
			}
			else if (typeof(TResult) == typeof(JToken))
			{
				return (TResult)(object)JToken.Parse(json);
			}
			else
			{
				return JsonConvert.DeserializeObject<TResult>(json);
			}
		}

		static string BuildURL(string interfaceName, string methodName, int version)
		{
			return string.Format("{0}/{1}/{2}/v{3}", BaseURL, interfaceName, methodName, version);
		}

		static string BuildQueryString(string url, Dictionary<string, object> parameter)
		{
			var parameters = parameter
				.Where(x => (object)x.Value != null)
				.Select(x => string.Format("{0}={1}", Uri.EscapeDataString(x.Key), Uri.EscapeDataString(StringifyParameter(x.Value))));

			var queryString = string.Format("?{0}", string.Join("&", parameters));
			var newUri = url + queryString;

			return newUri;
		}

		static HttpContent BuildFormContent(Dictionary<string, object> parameter)
		{
			var content = new FormUrlEncodedContent(parameter
				.Where(x => (object)x.Value != null)
				.Select(x => new KeyValuePair<string, string>(x.Key, StringifyParameter(x.Value))));
			return content;
		}

		protected static async Task<string> RunGetAsync(string url, Dictionary<string, object> parameter)
		{
			using (var client = new HttpClient())
			{
				var fullUri = BuildQueryString(url, parameter);
				return await client.GetStringAsync(fullUri);
			}
		}

		protected static async Task<string> RunPostAsync(string url, Dictionary<string, object> parameter)
		{
			using (var client = new HttpClient())
			{
				var content = BuildFormContent(parameter);
				var response = await client.PostAsync(url, content);
				return await response.Content.ReadAsStringAsync();
			}
		}

		static string StringifyParameter(object parameter)
		{
			// Shortcut!
			var asString = parameter as string;
			if (asString != null)
			{
				return asString;
			}

			// Use the ISteamUser behaviour for now, not the Economy behaviour.
			// ISteamUser behaviour - { args: [ 1, 2, 3 ] } => { args: "1,2,3" }
			// Economy behaviour - { args: [ 1, 2, 3 ] } => { args[0]: 1, args[1]: 2, args[2]: 3 }
			// Consistency Level: Valve
			var asEnumerable = parameter as IEnumerable;
			if (asEnumerable != null)
			{
				var parameters = asEnumerable.Cast<object>().Select(x => StringifyParameter(x));
				return string.Join(",", parameters);
			}

			// TODO: binary data and anything else

			return parameter.ToString();
		}
	}
}
