using UnityEngine;
using Unity.Services.Relay;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Netcode;
using System.Threading.Tasks;
using Unity.Services.Relay.Models;

/* Request a Relay server allocation (Host) and join a Relay server allocation (Client) */
public class RelaySetup : MonoBehaviour
{
    public static async Task<string> ServerAllocation()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        return joinCode;
    }

    public static async Task<bool> ConnectToAllocation(string joinCode)
    {
        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

        return !string.IsNullOrEmpty(joinCode);
    }


}
