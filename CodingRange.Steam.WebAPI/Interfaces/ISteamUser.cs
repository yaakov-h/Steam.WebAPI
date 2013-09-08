using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CodingRange.Steam.WebAPI
{
	public interface ISteamUser
	{
		[SteamAPICall("GetPlayerSummaries", 2)]
		GenericResponse<GetPlayerSummariesResult> GetPlayerSummaries(string key, IEnumerable<string> steamids);

		[SteamAPICall("GetPlayerSummaries", 2)]
		Task<GenericResponse<GetPlayerSummariesResult>> GetPlayerSummariesAsync(string key, IEnumerable<string> steamids);
	}

	#region Model

	public enum PersonaState
	{
		Offline = 0,
		Online,
		Busy,
		Away,
		Snooze,
		LookingToTrade,
		LookingToPlay
	}

	public enum CommunityVisibilityState
	{
		Private = 1,
		FriendsOnly,
		FriendsOfFriends,
		UsersOnly,
		Public
	}

	public class GetPlayerSummariesResult
	{
		[JsonProperty("players")]
		public PlayerSummary[] Players { get; set; }
	}

	public class PlayerSummary
	{
		[JsonProperty("steamid")]
		public UInt64 SteamId { get; set; }

		[JsonProperty("communityvisibilitystate")]
		public CommunityVisibilityState CommunityVisibilityState { get; set; }

		[JsonProperty("profilestate")]
		public bool ProfileConfigured { get; set; }

		[JsonProperty("personaname")]
		public string PersonaName { get; set; }

		[JsonProperty("lastlogoff")]
		public UInt32 LastLogoffTimestamp { get; set; }

		[JsonProperty("commentpermission")]
		public bool AllowsPublicComments { get; set; }
		
		[JsonProperty("profileurl")]
		public string ProfileUrl { get; set; }

		[JsonProperty("avatar")]
		public string SmallSizeAvatarUrl { get; set; }

		[JsonProperty("avatarmedium")]
		public string MediumSizeAvatarUrl { get; set; }

		[JsonProperty("avatarfull")]
		public string FullSizeAvatarUrl { get; set; }

		[JsonProperty("personastate")]
		public PersonaState PersonaState { get; set; }

		[JsonProperty("realname")]
		public string RealName { get; set; }

		[JsonProperty("primaryclanid")]
		public UInt64 PrimaryClanId { get; set; }

		[JsonProperty("timecreated")]
		public UInt32 CreatedTimestamp { get; set; }

		[JsonProperty("loccountrycode")]
		public string LocalCountryCode { get; set; }

		[JsonProperty("locstatecode")]
		public string LocalStateCode { get; set; }

		[JsonProperty("loccityid")]
		public int LocalCityID { get; set; }
	}

	#endregion
}
