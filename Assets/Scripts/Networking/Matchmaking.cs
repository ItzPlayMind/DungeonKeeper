using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using UnityEngine;
using UnityEngine.Networking;

public class Matchmaking
{
    [System.Serializable]
    public class MatchmakingException : System.Exception
    {
        public MatchmakingException(string message) : base(message) { }
    }

    private HttpClient client;

    [System.Serializable]
    public class Match
    {
        public string address;
        public ushort port;
        public string id;
    }

    public Matchmaking()
    {
        client = new HttpClient();
        client.BaseAddress = new System.Uri(Config.MATCHMAKING_SERVER_IP);
    }

    public async Task<Match> GetMatch(string id)
    {
        var query = HttpUtility.ParseQueryString(client.BaseAddress.Query);
        query["id"] = id;
        var response = await client.GetAsync(Config.MATCHMAKING_LOBBY_ROUTES + (string.IsNullOrEmpty(id) ? "" : "?"+query.ToString()));
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new MatchmakingException("No Match found");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonUtility.FromJson<Match>(content);
    }

    public async void RemoveMatch(string id)
    {
        var query = HttpUtility.ParseQueryString(client.BaseAddress.Query);
        query["id"] = id;
        var response = await client.DeleteAsync(Config.MATCHMAKING_LOBBY_ROUTES + "?" + query.ToString());
        if (response.StatusCode == HttpStatusCode.BadRequest)
            throw new MatchmakingException("Bad Match Delete Request");
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new MatchmakingException("No Match with ID found");
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> CreateMatch()
    {
        var query = HttpUtility.ParseQueryString(client.BaseAddress.Query);
        Match match = new Match();
        match.address = (await GetExternalIpAddress()).ToString();
        match.port = 7777;
        HttpContent content = new StringContent(JsonUtility.ToJson(match), System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync(Config.MATCHMAKING_LOBBY_ROUTES, content);
        if (response.StatusCode == HttpStatusCode.BadRequest)
            throw new MatchmakingException("Bad Match Create Request");
        response.EnsureSuccessStatusCode();
        var text = await response.Content.ReadAsStringAsync();
        return text;
    }

    public static async Task<IPAddress?> GetExternalIpAddress()
    {
        var externalIpString = (await new HttpClient().GetStringAsync(Config.EXTERNAL_IP))
            .Replace("\\r\\n", "").Replace("\\n", "").Trim();
        if (!IPAddress.TryParse(externalIpString, out var ipAddress)) return null;
        return ipAddress;
    }
}
