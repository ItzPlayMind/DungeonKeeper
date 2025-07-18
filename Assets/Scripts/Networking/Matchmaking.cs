using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
    [System.Serializable]
    private class IPCheckDTO
    {
        public string message;
        public bool success;
    }
    [System.Serializable]
    public class CharactersDTO
    {
        public CharacterDTO[] characters;
    }
    [System.Serializable]
    public class CharacterDTO
    {
        public int id;
        public bool enabled;
    }

    public Matchmaking()
    {
        client = new HttpClient();
        client.BaseAddress = new System.Uri(Config.MATCHMAKING_SERVER_IP);
    }

    private const string DELETION_TOKEN = "deletion-token";

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

    public async Task<string> CreateMatch(bool checkPortOpen = true)
    {
        var query = HttpUtility.ParseQueryString(client.BaseAddress.Query);
        Match match = new Match();
        match.address = (await GetExternalIpAddress()).ToString();
        match.port = 7777;
        if (checkPortOpen)
        {
            bool isPortOpen = await CheckRemotePort(match.address, match.port);
            if (!isPortOpen)
                throw new MatchmakingException("Port not open");
        }
        HttpContent content = new StringContent(JsonUtility.ToJson(match), System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync(Config.MATCHMAKING_LOBBY_ROUTES, content);
        if (response.StatusCode == HttpStatusCode.BadRequest)
            throw new MatchmakingException("Bad Match Create Request");
        response.EnsureSuccessStatusCode();
        var text = await response.Content.ReadAsStringAsync();
        var tokens = response.Headers.GetValues(DELETION_TOKEN);
        client.DefaultRequestHeaders.Remove(DELETION_TOKEN);
        client.DefaultRequestHeaders.Add(DELETION_TOKEN, tokens);
        return text;
    }

    public async Task<CharactersDTO> GetCharacters()
    {
        var query = HttpUtility.ParseQueryString(client.BaseAddress.Query);
        var response = await client.GetAsync(Config.CHARACTERS_ROUTE);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonUtility.FromJson<CharactersDTO>(content);
    }

    public static async Task<IPAddress?> GetExternalIpAddress()
    {
        var externalIpString = (await new HttpClient().GetStringAsync(Config.EXTERNAL_IP))
            .Replace("\\r\\n", "").Replace("\\n", "").Trim();
        if (!IPAddress.TryParse(externalIpString, out var ipAddress)) return null;
        return ipAddress;
    }

    async Task<bool> CheckRemotePort(string publicIp, ushort port)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        listener.AcceptSocketAsync();
        Match match = new Match()
        {
            address = publicIp,
            port = port
        };

        string json = JsonUtility.ToJson(match);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        try
        {
            var response = await client.PostAsync("check-port", content);
            string result = await response.Content.ReadAsStringAsync();
            var dto = JsonUtility.FromJson<IPCheckDTO>(result);
            listener.Stop();
            Debug.Log(result);
            return dto.success;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}
