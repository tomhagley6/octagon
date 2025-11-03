using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

/* Temporary session creation to access Unity Services 
   (It should be clarified whether this is only required for Unity Leaderboards,
   or whether Unity Relay also requries sign in */
public class UnityServicesSetup : MonoBehaviour
{

    // Using asynchronous code for efficiency
    private async void Awake()
    {
        // Initialise the SDK and its dependencies
        await UnityServices.InitializeAsync();


        // subscribe to 3 event types with Debug statements
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in as: " + AuthenticationService.Instance.PlayerId);
        };
        AuthenticationService.Instance.SignInFailed += async s =>
        {
            Debug.Log("Sign in failed");
            Debug.Log(s);
        };
        AuthenticationService.Instance.SignedOut += () =>
        {
            Debug.Log("Player signed out.");
        };

        // We clear the session token (if it exists) before each attempt to sign in,
        // because a session token is good for only one sign in, but will persist across
        // runs of the Octagon executable
        if (AuthenticationService.Instance.SessionTokenExists)
        {
            try
            {
                AuthenticationService.Instance.ClearSessionToken();
            }
            catch (AuthenticationException e)
            {
                Debug.Log("Authentication error has occurred trying to clear session token.");
                Debug.LogWarning(e);
            }
        }

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        await AuthenticationService.Instance.UpdatePlayerNameAsync("Default");
    }

    private async void OnDestroy()
    {
        await AuthenticationService.Instance.DeleteAccountAsync();
    }



}
