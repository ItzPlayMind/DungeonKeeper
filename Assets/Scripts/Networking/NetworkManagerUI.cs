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
    [SerializeField] private GameObject howToPlay;
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Button playButton;
    [SerializeField] private GameObject matchmakingUI;
    [SerializeField] private GameObject userUI;
    [SerializeField] private TMPro.TMP_InputField nameInput;
    [SerializeField] private TMPro.TextMeshProUGUI statusText;

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
    }

    private void OnEnable()
    {
        statusText.text = "";
        if (Lobby.Instance == null) return;
        if (!string.IsNullOrEmpty(Lobby.Instance.LobbyCode) && Lobby.Instance.IsHost)
        {
            Lobby.Instance.Matchmaking?.RemoveMatch(Lobby.Instance.LobbyCode);
            Lobby.Instance.SetLobbyCode("");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId != networkManager.LocalClientId) return;
        isFetching = false;
        LobbyPanel.Instance.gameObject.SetActive(true);
        gameObject.SetActive(false);
        StopRotatingText();
        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId != networkManager.LocalClientId) return;
        isFetching = false;
        networkManager.Shutdown();
        StopRotatingText();
        statusText.text = "Connection Failed";
        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void StartRotatingText(string text)
    {
        coroutine = StartCoroutine(RotatingText(text));
    }
    private IEnumerator RotatingText(string text)
    {
        if(coroutine != null) StopRotatingText();
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
    private void StopRotatingText()
    {
        StopCoroutine(coroutine);
    }
    Coroutine coroutine;
    public async void StartClient()
    {
        if (isFetching) return;
        isFetching = true;
        StartRotatingText("Matching");
        try
        {
            var match = await Lobby.Instance.Matchmaking.GetMatch(Lobby.Instance.LobbyCode);
            transport.ConnectionData.Address = match.address;
            transport.ConnectionData.Port = match.port;
            Lobby.Instance.SetLobbyCode(match.id);
            Debug.Log("Found Match: " + match.address + ":" + match.port);
            StartRotatingText("Connecting");
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
        StartRotatingText("Creating");
        isFetching = true;
        try
        {
            Lobby.Instance.SetLobbyCode(await Lobby.Instance.Matchmaking.CreateMatch());
            if (networkManager.StartHost())
            {
                isFetching = false;
                gameObject.SetActive(false);
                LobbyPanel.Instance.gameObject.SetActive(true);
            }
            else
            {
                isFetching = false;
                Lobby.Instance.Matchmaking.RemoveMatch(Lobby.Instance.LobbyCode);
                Lobby.Instance.SetLobbyCode("");
            }
            StopRotatingText();
        }catch(Matchmaking.MatchmakingException e)
        {
            StopCoroutine(coroutine);
            statusText.text = e.Message;
            isFetching = false;
        }
    }

    private void OnApplicationQuit()
    {
        if (!string.IsNullOrEmpty(Lobby.Instance.LobbyCode) && Lobby.Instance.IsHost)
        {
            Lobby.Instance.Matchmaking.RemoveMatch(Lobby.Instance.LobbyCode);
            Lobby.Instance.SetLobbyCode("");
        }
    }

    public void RemoveAllKeybindings()
    {
        RebindActionUI.ResetAllToDefault();
    }

    public void ToggleHowToPlay()
    {
        howToPlay.SetActive(!howToPlay.activeSelf);
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
        playButton.interactable = !string.IsNullOrEmpty(code);
    }

    public void SetGameMode(int index)
    {
        Lobby.Instance.SetGameModeIndex(index);
    }
}
