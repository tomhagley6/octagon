using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Relay;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Netcode;
using System.Threading.Tasks;
using Unity.Services.Relay.Models;

public class RelayServerAllocation : MonoBehaviour
{
    public async Task<string> ServerAllocation()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        
        return joinCode;
    }

    public async Task<bool> ConnectToAllocation(string joinCode)
    {
        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
        
        return !string.IsNullOrEmpty(joinCode);
    }

    
}
