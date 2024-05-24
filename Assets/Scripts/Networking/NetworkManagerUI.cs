using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Globals;
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
    [SerializeField] TMP_InputField enteredPort;
    [SerializeField] TMP_InputField enteredJoinCode;

	
	[SerializeField] UnityTransport transport;

    private NetworkManager networkManager;

    private Action toggleIpAddress;
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


    private void Awake() 
    {
        // Add listeners to all buttons
        serverButton.onClick.AddListener(() => 
        {
            NetworkManager.Singleton.StartServer();
        });


        hostButton.onClick.AddListener(async () => 
        {
            
            // Get public IP address from NetworkManager
            ipAddress = GetPublicIPAddress();
            ipAddressText.text = ipAddress;
            Debug.Log("NetworkManagerUI IP is: " + ipAddress);

            // Identify if using the default port, or if the end-user has entered one
            if (string.IsNullOrEmpty(enteredPort.GetComponent<TMP_InputField>().text))
            {
                Debug.Log("String is null or empty. Using default port");
                
                // If client does not input the host port, resort to default
                portDefaultBool = true;
            }
            else
            {
                // Else, use the input port
                try 
                {   
                    Debug.Log(enteredPort.GetComponent<TMP_InputField>().text);
                    var testPortInput = UInt16.Parse(enteredPort.GetComponent<TMP_InputField>().text);
                    portDefaultBool = false;
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    Debug.LogWarning("Port number must an integer between 0 and 65535");
                    Debug.Log("Attempting to use default port value");
                    portDefaultBool = true;
                }  
            }
            
            // Set the connection data, where ipAddress is irrelevant(?) for the host
            if (portDefaultBool != null)
            {
                port = (bool)portDefaultBool ? portDefault : port = UInt16.Parse(enteredPort.GetComponent<TMP_InputField>().text);
                SetConnectionData(ipAddress, port, listenAddress); // This is ignored when using Relay
            }
            PrintConnectionInfo();

            // Request and join a Unity Relay server allocation, returning a join code
            joinCode = await RelaySetup.ServerAllocation();
            Debug.LogWarning("join code is: " + joinCode);
            
            // Disable buttons and input fields once no longer needed
            DisableButtons();
            gameObject.transform.Find("IpAddress").gameObject.SetActive(false);
            NetworkManager.Singleton.StartHost();

        });


        clientButton.onClick.AddListener(async () => 
        {   

            // Identify if connecting to a relay server
            // (In which case, ignore direct connection logic)
            if (!string.IsNullOrEmpty(enteredJoinCode.GetComponent<TMP_InputField>().text))
            {
                Debug.LogWarning("Join Code field populated, attempting to join Relay server...");
                joinCodeBool = true;
            }
            
            // Identify if using the default IP, or if the end-user has entered one
            if (string.IsNullOrEmpty(enteredIp.GetComponent<TMP_InputField>().text))
            {
                Debug.Log("String is null or empty. Using default IP address");
                
                // If client does not input the host public IP, resort to default
                IpDefaultBool = true;
            }
            else
            {
                // Else, use the input IP
                IpDefaultBool = false;
            }

            // Identify if using the default port, or if the end-user has entered one
            if (string.IsNullOrEmpty(enteredPort.GetComponent<TMP_InputField>().text))
            {
                Debug.Log("String is null or empty. Using default port");
                
                // If client does not input the host port, resort to default
                portDefaultBool = true;
            }
            else
            {
                // Else, use the input port
                try 
                {   
                    Debug.Log(enteredPort.GetComponent<TMP_InputField>().text);
                    var testPortInput = UInt16.Parse(enteredPort.GetComponent<TMP_InputField>().text);
                    portDefaultBool = false;
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    Debug.LogWarning("Port number must an integer between 0 and 65535");
                    Debug.Log("Attempting to use default port value");
                    portDefaultBool = true;
                }  
            }

            // Collect logic and set connection data
            if (IpDefaultBool != null && portDefaultBool != null)
            {
                ipAddress = (bool)IpDefaultBool ? ipAddressDefault : enteredIp.GetComponent<TMP_InputField>().text;
                port = (bool)portDefaultBool ? portDefault : port = UInt16.Parse(enteredPort.GetComponent<TMP_InputField>().text);
                SetConnectionData(ipAddress, port, listenAddress); // This is ignored when using Relay
                PrintConnectionInfo();
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
        
        // Set to hardcoded values at the beginning for testing
        SetConnectionData(ipAddressDefault, portDefault, listenAddress);

        // Print network connection value defaults when application starts
        PrintConnectionInfo();

        // Subscribe to the method handling IP Address field visibility toggle
        toggleIpAddress += ToggleIpAddressListener;
    }


    // Monitor toggle key for IP Address visibility
    public void Update()
    {
        if (Input.GetKeyDown(General.toggleIP))
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
        gameObject.transform.Find("PortInput").gameObject.SetActive(false);
        gameObject.transform.Find("JoinCodeInput").gameObject.SetActive(false);
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
    
    public string GetPublicIPAddress() {
        
        try {
        // Get public IP address from NetworkManager
        ipAddress = networkManager.GetComponent<PublicIp>().GetPublicIp();
        ipAddressText.text = ipAddress;
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
            Debug.LogWarning($"Public IP lookup failed. Using default IP {ipAddressDefault} instead");
            ipAddress = ipAddressDefault;
        }
        return ipAddress;
    }


	// /* Sets the Ip Address of the Connection Data in Unity Transport
	// to the Ip Address which was input in the Input Field */
	// // ONLY FOR CLIENT SIDE
	// public void SetIpAddress() {
	// 	transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
	// 	transport.ConnectionData.Address = ipAddress;
    //     Debug.Log(ipAddress);

	// }


    // /* Gets the Ip Address of your connected network and
	// shows on the screen in order to let other players join
	// by inputing that Ip in the input field */
	// // ONLY FOR HOST SIDE 
	// public string GetLocalIPAddress() {
	// 	var host = Dns.GetHostEntry(Dns.GetHostName());
	// 	foreach (var ip in host.AddressList) {
	// 		if (ip.AddressFamily == AddressFamily.InterNetwork) {
	// 			ipAddressText.text = ip.ToString();
	// 			ipAddress = ip.ToString();
	// 			return ip.ToString();
	// 		}
	// 	}
	// 	throw new System.Exception("No network adapters with an IPv4 address in the system!");
	// }

}




