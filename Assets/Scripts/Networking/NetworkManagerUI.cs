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
    [SerializeField] private GameObject matchmakingUI;
    [SerializeField] private GameObject userUI;
    [SerializeField] private TMPro.TMP_InputField nameInput;
    [SerializeField] private TMPro.TextMeshProUGUI statusText;

    private Matchmaking matchmaking;
    private bool isFetching = false;
    private string namePreview;

    // Start is called before the first frame update
    void Start()
    {
        if (!string.IsNullOrEmpty(Lobby.Instance.PlayerStatistic.Name))
        {
            matchmakingUI.SetActive(true);
            userUI.SetActive(false);
        }
        networkManager = GetComponentInParent<NetworkManager>();
        transport = networkManager.GetComponent<UnityTransport>();
        nameInput.text = Lobby.Instance.PlayerStatistic.Name;
        matchmaking = new Matchmaking();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId != networkManager.LocalClientId) return;
        isFetching = false;
        LobbyPanel.Instance.gameObject.SetActive(true);
        gameObject.SetActive(false);
        statusText.text = "";
        StopCoroutine(coroutine);
        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId != networkManager.LocalClientId) return;
        isFetching = false;
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
        if (isFetching) return;
        isFetching = true;
        coroutine = StartCoroutine(RotatingText("Matching"));
        try
        {
            var match = await matchmaking.GetMatch(Lobby.Instance.LobbyCode);
            transport.ConnectionData.Address = match.address;
            transport.ConnectionData.Port = match.port;
            Lobby.Instance.SetLobbyCode(match.id);
            Debug.Log("Found Match: " + match.address + ":" + match.port);
            StopCoroutine(coroutine);
            coroutine = StartCoroutine(RotatingText("Connecting"));
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            networkManager.StartClient();
        }catch(Matchmaking.MatchmakingException e)
        {
            StopCoroutine(coroutine);
            statusText.text = e.Message;
            isFetching = false;
        }
        catch (Exception e)
        {
            StopCoroutine(coroutine);
            statusText.text = "An error occured";
            isFetching = false;
        }
    }
    public async void StartHost()
    {
        if (isFetching) return;
        isFetching = true;
        Lobby.Instance.SetLobbyCode(await matchmaking.CreateMatch());
        if (networkManager.StartHost())
        {
            isFetching = false;
            Lobby.Instance.OnGameStart = StartGame;
            gameObject.SetActive(false);
            LobbyPanel.Instance.gameObject.SetActive(true);
        }
        else
        {
            isFetching = false;
            matchmaking.RemoveMatch(Lobby.Instance.LobbyCode);
        }
    }

    public void StartGame()
    {
        matchmaking.RemoveMatch(Lobby.Instance.LobbyCode);
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
        namePreview = name;
    }

    public void SaveName()
    {
        Lobby.Instance.PlayerStatistic.SetName(namePreview);
        matchmakingUI.SetActive(true);
        userUI.SetActive(false);
    }

    public void ChangeCode(string code)
    {
        Lobby.Instance.SetLobbyCode(code);
    }

    public void SetGameMode(int index)
    {
        Lobby.Instance.SetGameModeIndex(index);
    }
}
