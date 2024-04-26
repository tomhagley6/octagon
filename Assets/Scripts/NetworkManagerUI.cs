using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Mono.CSharp;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;


// UI class to link NetworkManager Server/Host/Client
// methods to canvas buttons
public class NetworkManagerUI : NetworkBehaviour
{
    [SerializeField] private Button serverButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    [SerializeField] TextMeshProUGUI ipAddressText;
	[SerializeField] TMP_InputField enteredIp;

	
	[SerializeField] UnityTransport transport;

    private NetworkManager networkManager;

    private Action toggleIpAddress;
    bool IpAddressVisible = false;

    // SetConnectionData variables
    private string ipAddressDefault = "86.159.151.28";
    [SerializeField] private string ipAddress;
    private ushort port = 55598;
    private string listenAddress = "0.0.0.0";

    private void Awake() {
    serverButton.onClick.AddListener(() => {
        NetworkManager.Singleton.StartServer();
        });

    hostButton.onClick.AddListener(() => {
		// transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
		// transport.ConnectionData.Address = ipAddress;

        Debug.Log(ipAddress);
        ipAddress = GetPublicIPAddress();
        // Get public IP address from NetworkManager
        ipAddress = GetPublicIPAddress();
        Debug.Log(ipAddress);
        ipAddressText.text = ipAddress;
        Debug.Log("NetworkManagerUI IP is: " + ipAddress);
        SetConnectionData(ipAddress, port, listenAddress);
        PrintConnectionInfo();
        DisableButtons();
        gameObject.transform.Find("IpAddress").gameObject.SetActive(false);
        NetworkManager.Singleton.StartHost();
		// GetLocalIPAddress();

        });
    clientButton.onClick.AddListener(() => {
        // ipAddress = ip.text;
		// SetIpAddress();
        
        if (string.IsNullOrEmpty(enteredIp.GetComponent<TMP_InputField>().text))
        {
            Debug.Log("String is null or empty. Using default IP address");
            // If client does not input the host public IP, resort to default
            SetConnectionData(ipAddressDefault, port, listenAddress);
        }
        else
        {
            // Else, use the input IP
            ipAddress = enteredIp.GetComponent<TMP_InputField>().text;
            SetConnectionData(ipAddress, port, listenAddress);
        }

        DisableButtons();
        gameObject.transform.Find("IpAddress").gameObject.SetActive(false);

        NetworkManager.Singleton.StartClient();
        }); 

    }


    void Start()
	{        
        
        networkManager = FindObjectOfType<NetworkManager>();
        
        // Set to hardcoded values at the beginning for testing
        SetConnectionData(ipAddressDefault, port, listenAddress);

        // Print network connection value defaults when application starts
        PrintConnectionInfo();

        // Subscribe to the method handling IP Address field visibility toggle
        toggleIpAddress += ToggleIpAddressListener;
    }


    // Monitor toggle key for IP Address visibility
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            toggleIpAddress();
        }
    }

    private void ToggleIpAddressListener() 
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

    private void SetConnectionData(string ip, ushort port, string listenAddress)
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
            ip,                // public IP address
            port,              // port number as an unsigned short
            listenAddress      // Server listen address
        );

    }

    private void DisableButtons()
    {
        gameObject.transform.Find("ServerButton").gameObject.SetActive(false);
        gameObject.transform.Find("HostButton").gameObject.SetActive(false);
        gameObject.transform.Find("ClientButton").gameObject.SetActive(false);
        gameObject.transform.Find("IpAddressInput").gameObject.SetActive(false);
    }

    // print IP address, port, and server listen address
    private void PrintConnectionInfo()
    {
        var currConnectionInfo = NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData;
        string currIp = currConnectionInfo.Address;
        ushort currPort = currConnectionInfo.Port;
        string currListen = currConnectionInfo.ServerListenAddress;
        Debug.Log($"Ip, port, and listen address are:\n{currIp}\n{currPort}\n{currListen}");
    }


	/* Sets the Ip Address of the Connection Data in Unity Transport
	to the Ip Address which was input in the Input Field */
	// ONLY FOR CLIENT SIDE
	public void SetIpAddress() {
		transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
		transport.ConnectionData.Address = ipAddress;
        Debug.Log(ipAddress);

	}


    /* Gets the Ip Address of your connected network and
	shows on the screen in order to let other players join
	by inputing that Ip in the input field */
	// ONLY FOR HOST SIDE 
	public string GetLocalIPAddress() {
		var host = Dns.GetHostEntry(Dns.GetHostName());
		foreach (var ip in host.AddressList) {
			if (ip.AddressFamily == AddressFamily.InterNetwork) {
				ipAddressText.text = ip.ToString();
				ipAddress = ip.ToString();
				return ip.ToString();
			}
		}
		throw new System.Exception("No network adapters with an IPv4 address in the system!");
	}

    public string GetPublicIPAddress() {
        
        // Get public IP address from NetworkManager
        ipAddress = networkManager.GetComponent<PublicIp>().GetPublicIp();
        ipAddressText.text = ipAddress;
        return ipAddress;
    }

}




