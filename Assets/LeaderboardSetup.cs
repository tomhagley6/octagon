using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class LeaderboardSetup : MonoBehaviour
{

    // Using asynchronous code for efficiency
    private async void Awake()
    {
        // Initialise the Leaderboards SDK and its dependencies
        await UnityServices.InitializeAsync();

        // Anonyous authentication tocreate an anonymous account for the 
        // player to persist their scores
        
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in as: " + AuthenticationService.Instance.PlayerId);
        };
        AuthenticationService.Instance.SignInFailed += s =>
        {
            Debug.Log("Sign in failed");
            Debug.Log(s);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }


}
