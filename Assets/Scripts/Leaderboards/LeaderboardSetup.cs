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

        // Anonyous authentication to create an anonymous account for the 
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
        AuthenticationService.Instance.SignedOut += () => {
            Debug.Log("Player signed out.");
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        await AuthenticationService.Instance.UpdatePlayerNameAsync("Default");
    }

    private async void OnDestroy()
    {
        await AuthenticationService.Instance.DeleteAccountAsync();
    }



}
