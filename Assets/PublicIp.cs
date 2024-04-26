using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text.RegularExpressions;

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
                Debug.LogError("Failed to get public IP: " + www.error);
            }
            else
            {
                string webTextReturn = www.downloadHandler.text;
                // Debug.Log("Web server return string: " + webTextReturn);
                // Now you can display ipAddress on your screen or use it as needed.

                string ipPattern = @"\d\d\.\d\d\d\.\d\d\d\.\d\d";
                Regex regex = new Regex(ipPattern);

                // Check if an IP is present from the queried URL
                Debug.Log(regex.IsMatch(webTextReturn));
                var match = regex.Match(webTextReturn);
                Debug.Log(match.Success);
                if (match.Success)
                {
                    publicIp = match.Groups[0].Value;
                    Debug.Log("Public IP Address: " + publicIp);
                }
            }
        }
    }



}
