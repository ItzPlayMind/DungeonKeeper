using System.Collections;
using System.Collections.Generic;
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
    // Start is called before the first frame update
    void Start()
    {
        networkManager = GetComponentInParent<NetworkManager>();
        transport = networkManager.GetComponent<UnityTransport>();
        nameInput.text = Lobby.Instance.PlayerStatistic.Name;
    }

    public void Play()
    {
        if (string.IsNullOrEmpty(transport.ConnectionData.Address))
            StartHost();
        else
            StartClient();
    }

    private void StartClient()
    {
        LobbyPanel.Instance.gameObject.SetActive(true);
        gameObject.SetActive(false);
        if (!networkManager.StartClient())
        {
            gameObject.SetActive(true);
            loadingScreen.SetActive(false);
        }
    }
    private void StartHost()
    {
        LobbyPanel.Instance.gameObject.SetActive(true);
        gameObject.SetActive(false);
        if (!networkManager.StartHost())
        {
            gameObject.SetActive(true);
            loadingScreen.SetActive(false);
        }
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
