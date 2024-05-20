using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Relay;

public class RelayServerAllocation : MonoBehaviour
{
    public async Task<string> ServerAllocation()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocation(2);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        
        return joinCode;
    }

    public async Task<bool> ConnectToAllocation(string joinCode)
    {
        var joinAllocation = await RelayService.Instance.joinAllocation(joinCode: joinCode)
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
        
        return !string.IsNullOrEmpty(joinCode);
    }

    
}
