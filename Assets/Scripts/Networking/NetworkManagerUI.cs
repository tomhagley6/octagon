using System;
using Globals;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;


// UI class to link NetworkManager Server/Host/Client
// methods to canvas buttons

// See Archive for previous versions of this script that include 
// port forwarding, IP address/port input and generation, and addition of
// self IP address to the diagnostics overlay
public class NetworkManagerUI : NetworkBehaviour
{
    [SerializeField] private Button serverButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    [SerializeField] TextMeshProUGUI ipAddressText;
    [SerializeField] TMP_InputField enteredIp;
    [SerializeField] TMP_InputField enteredPort;
    [SerializeField] TMP_InputField enteredJoinCode; // This is the only InputField currently in use


    [SerializeField] UnityTransport transport;

    private NetworkManager networkManager;

    bool IpAddressVisible = false;

    // SetConnectionData variables
    private string ipAddressDefault = "86.159.151.28";
    [SerializeField] private string ipAddress;
    [SerializeField] private ushort port;
    private ushort portDefault = 55599;
    private string listenAddress = "0.0.0.0";
    private bool? IpDefaultBool; // control logic for updating connection data
    private bool? portDefaultBool; // control logic for updating connection data
    private bool? joinCodeBool; // control logic for joining a relay server
    public string joinCode; // code returned by the Host during Relay server allocation
    [SerializeField] private string clientJoinCode; // code entered by the Client during Relay server connection


    // Add Listeners to all buttons
    private void Awake()
    {

        // SERVER
        serverButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
        });

        // Use async here because we are making Network calls and want our UI
        // to remain responsive
        // The AddListener method accepts a UnityAction delegate (essentially a function
        // pointer).
        // Here we are passing an async void method (in the form a lambda method)
        // The button click will trigger this async operation, but the UI will
        // continue to be responsive while the async runs in the background


        // HOST
        hostButton.onClick.AddListener(async () =>
        {

            // Request and join a Unity Relay server allocation, returning a join code
            joinCode = await RelaySetup.ServerAllocation();
            Debug.LogWarning("join code is: " + joinCode);

            // Disable buttons and input fields once no longer needed
            DisableButtons();
            gameObject.transform.Find("IpAddress").gameObject.SetActive(false);
            NetworkManager.Singleton.StartHost();

        });

        // CLIENT
        clientButton.onClick.AddListener(async () =>
        {
            // Identify if Relay join code has been input
            if (!string.IsNullOrEmpty(enteredJoinCode.GetComponent<TMP_InputField>().text))
            {
                Debug.LogWarning("Join Code field populated, attempting to join Relay server...");
                joinCodeBool = true;
            }

            // Relay server connection logic
            if (joinCodeBool == true)
            {
                clientJoinCode = enteredJoinCode.GetComponent<TMP_InputField>().text;
                await RelaySetup.ConnectToAllocation(clientJoinCode);
            }

            DisableButtons();
            gameObject.transform.Find("IpAddress").gameObject.SetActive(false);

            NetworkManager.Singleton.StartClient();
        });

    }


    void Start()
    {

        networkManager = FindObjectOfType<NetworkManager>();

        // Subscribe to the method handling IP Address field visibility toggle
        UIToggleListener.toggleOverlay += ToggleOverlayAddressListener;
    }



    // This is currently unused because we never take input for IPAddress when using Relay
    private void ToggleOverlayAddressListener()
    {
        // Set IP Address text GameObject to inactive if toggled off, and active
        // if toggled on 
        if (!IpAddressVisible)
        {
            gameObject.transform.Find("IpAddress").gameObject.SetActive(true);
            gameObject.transform.Find("IpAddress").gameObject.GetComponent<TMP_Text>().text = ipAddress;
        }
        else
        {
            gameObject.transform.Find("IpAddress").gameObject.SetActive(false);
        }

        // Correctly track the IP address visibility
        IpAddressVisible = !IpAddressVisible;
    }


    private void DisableButtons()
    {
        gameObject.transform.Find("ServerButton").gameObject.SetActive(false);
        gameObject.transform.Find("HostButton").gameObject.SetActive(false);
        gameObject.transform.Find("ClientButton").gameObject.SetActive(false);
        gameObject.transform.Find("IpAddressInput").gameObject.SetActive(false);
        gameObject.transform.Find("PortInput").gameObject.SetActive(false);
        gameObject.transform.Find("JoinCodeInput").gameObject.SetActive(false);
    }


}




