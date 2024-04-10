using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


// UI class to link NetworkManager Server/Host/Client
// methods to canvas buttons
public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button serverButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    private void Awake() {
    serverButton.onClick.AddListener(() => {
        NetworkManager.Singleton.StartServer();

    });
    hostButton.onClick.AddListener(() => {
        NetworkManager.Singleton.StartHost();

    });
    clientButton.onClick.AddListener(() => {
        NetworkManager.Singleton.StartClient();

    });
}
}


