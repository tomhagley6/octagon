using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class UnityServicesSetup : MonoBehaviour
{

    // Using asynchronous code for efficiency
    private async void Awake()
    {
        // Initialise the SDK and its dependencies
        await UnityServices.InitializeAsync();

        // Anonyous authentication to create an anonymous account for the 
        // player to upload their scores

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
        AuthenticationService.Instance.SignedOut += () => {
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
