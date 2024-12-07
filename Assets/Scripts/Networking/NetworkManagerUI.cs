using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkManagerUI : MonoBehaviour
{
    UnityTransport transport;
    NetworkManager networkManager;
    [SerializeField] private LobbyPanel lobbyPanel;
    [SerializeField] private GameObject loadingScreen;
    // Start is called before the first frame update
    void Start()
    {
        networkManager = GetComponentInParent<NetworkManager>();
        transport = networkManager.GetComponent<UnityTransport>();
    }

    public void StartClient()
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
    public void StartHost()
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
}
