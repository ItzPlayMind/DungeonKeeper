using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkManagerUI : MonoBehaviour
{
    UnityTransport transport;
    NetworkManager networkManager;
    // Start is called before the first frame update
    void Start()
    {
        networkManager = GetComponentInParent<NetworkManager>();
        transport = networkManager.GetComponent<UnityTransport>();
    }

    public void StartClient()
    {
        networkManager.StartClient();
    }

    public void StartHost()
    {
        networkManager.StartHost();
    }

    public void ChangeIP(string ip)
    {
        transport.ConnectionData.Address = ip;
    }
}
