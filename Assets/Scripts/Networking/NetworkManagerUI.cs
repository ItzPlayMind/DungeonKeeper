using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    UnityTransport transport;
    NetworkManager networkManager;
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Button playButton;
    [SerializeField] private TMPro.TMP_InputField nameInput;
    [SerializeField] private TMPro.TextMeshProUGUI statusText;

    private string id;
    private Matchmaking matchmaking;

    // Start is called before the first frame update
    void Start()
    {
        networkManager = GetComponentInParent<NetworkManager>();
        transport = networkManager.GetComponent<UnityTransport>();
        nameInput.text = Lobby.Instance.PlayerStatistic.Name;
        matchmaking = new Matchmaking();
    }

    private void OnClientConnected(ulong clientId)
    {
        LobbyPanel.Instance.gameObject.SetActive(true);
        gameObject.SetActive(false);
        statusText.text = "";
        StopCoroutine(coroutine);
        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        networkManager.Shutdown();
        statusText.text = "Connection Failed";
        StopCoroutine(coroutine);
        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private IEnumerator RotatingText(string text)
    {
        while (true)
        {
            statusText.text = text+".";
            yield return new WaitForSeconds(0.25f);
            statusText.text = text + "..";
            yield return new WaitForSeconds(0.25f);
            statusText.text = text + "...";
            yield return new WaitForSeconds(0.25f);
        }
    }
    Coroutine coroutine;
    public async void StartClient()
    {
        coroutine = StartCoroutine(RotatingText("Matching"));
        try
        {
            var match = await matchmaking.GetMatch();
            transport.ConnectionData.Address = match.address;
            transport.ConnectionData.Port = match.port;
            StopCoroutine(coroutine);
            coroutine = StartCoroutine(RotatingText("Connecting"));
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            networkManager.StartClient();
        }catch(Matchmaking.MatchmakingException e)
        {
            StopCoroutine(coroutine);
            statusText.text = e.Message;
        }
        catch (Exception e)
        {
            StopCoroutine(coroutine);
            statusText.text = "An error occured";
        }
    }
    public async void StartHost()
    {
        id = await matchmaking.CreateMatch();
        if (networkManager.StartHost())
        {
            Lobby.Instance.OnGameStart = StartGame;
            gameObject.SetActive(false);
            LobbyPanel.Instance.gameObject.SetActive(true);
        }
        else
            matchmaking.RemoveMatch(id);
    }

    public void StartGame()
    {
        matchmaking.RemoveMatch(id);
    }

    public void ChangeIP(string ip)
    {
        transport.ConnectionData.Address = ip;
    }

    private void Update()
    {
        playButton.enabled = !string.IsNullOrEmpty(name);
    }

    public void ChangeName(string name)
    {
        Lobby.Instance.PlayerStatistic.SetName(name);
    }

    public void SetGameMode(int index)
    {
        Lobby.Instance.SetGameModeIndex(index);
    }
}
