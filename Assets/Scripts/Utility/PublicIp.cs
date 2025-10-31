using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text.RegularExpressions;

/* Query a web server to get the public IP address 
   and store it in a string variable for later use */
public class PublicIp : MonoBehaviour
{

    private const string IPIdentificationURL = "https://www.ipchicken.com/";
    private string publicIp;


    public string GetPublicIp()
    {
        return publicIp;
    }

    void Start()
    {
        StartCoroutine(GetIP());
    }

    IEnumerator GetIP()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(IPIdentificationURL))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning("Failed to get public IP: " + www.error);
                Debug.LogWarning("You will have to manually enter the public IP of the host to continue connecting");
            }
            else
            {
                string webTextReturn = www.downloadHandler.text;
                // Debug.Log("Web server return string: " + webTextReturn);
                // Now you can display ipAddress on your screen or use it as needed.

                string ipPattern = @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}";
                Regex regex = new Regex(ipPattern);

                // Check if an IP is present from the queried URL
                Debug.Log("Regex matches an IP address: " + regex.IsMatch(webTextReturn));
                var match = regex.Match(webTextReturn);
                // Debug.Log(match.Success);
                if (match.Success)
                {
                    publicIp = match.Groups[0].Value;
                    Debug.Log("Public IP Address: " + publicIp);
                }
                else
                {
                    Debug.Log("No match for IP regex found");
                }
            }
        }
    }



}
