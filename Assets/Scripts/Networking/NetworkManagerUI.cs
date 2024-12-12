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
    [SerializeField] private LobbyPanel lobbyPanel;
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Button playButton;
    [SerializeField] private TMPro.TMP_InputField nameInput;
    // Start is called before the first frame update
    void Start()
    {
        networkManager = GetComponentInParent<NetworkManager>();
        transport = networkManager.GetComponent<UnityTransport>();
        nameInput.text = GameManager.instance.PlayerStatistics.Name;
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
        loadingScreen.SetActive(true);
        gameObject.SetActive(false);
        if (networkManager.StartClient())
        {
            lobbyPanel.gameObject.SetActive(true);
            loadingScreen.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
            loadingScreen.SetActive(false);
        }
    }
    private void StartHost()
    {
        loadingScreen.SetActive(true);
        gameObject.SetActive(false);
        if (networkManager.StartHost())
        {
            lobbyPanel.gameObject.SetActive(true);
            loadingScreen.SetActive(false);
        }
        else
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
        GameManager.instance.PlayerStatistics.SetName(name);
    }
}
