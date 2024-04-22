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
public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button serverButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    [SerializeField] TextMeshProUGUI ipAddressText;
	[SerializeField] TMP_InputField ip;

	[SerializeField] string ipAddress;
	[SerializeField] UnityTransport transport;


    private void Awake() {
    serverButton.onClick.AddListener(() => {
        NetworkManager.Singleton.StartServer();
        });
    hostButton.onClick.AddListener(() => {
        // ipAddress = "86.159.151.28";
		// transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
		// transport.ConnectionData.Address = ipAddress;
		NetworkManager.Singleton.StartHost();
        Debug.Log(ipAddress);
        gameObject.SetActive(false);
		// GetLocalIPAddress();
        });
    clientButton.onClick.AddListener(() => {
        // ipAddress = ip.text;
		// SetIpAddress();
        NetworkManager.Singleton.StartClient();
        gameObject.SetActive(false);
        }); 

    }


    void Start()
	{        
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
            "86.159.151.28", // public IP address
            (ushort)55598,   // port number as an unsigned short
            "0.0.0.0"        // Server listen address
        );

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


}




