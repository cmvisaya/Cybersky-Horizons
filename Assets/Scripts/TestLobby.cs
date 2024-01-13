using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using TMPro;

public class TestLobby : MonoBehaviour
{
    public TextMeshProUGUI lt1, lt2;

    private Lobby hostLobby;
    private float heartbeatTimer;
    private async void Start() {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () => {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void Update() {
        HandleLobbyHeartbeat();

        /*
        if (Input.GetKeyDown(KeyCode.L)) {
            CreateLobby();
        }

        if (Input.GetKeyDown(KeyCode.P)) {
            ListLobbies();
        }

        if (Input.GetKeyDown(KeyCode.J)) {
            JoinLobbyByCode("AAAAAA");
        }
        */
    }

    private async void HandleLobbyHeartbeat() {
        if (hostLobby != null) {
            heartbeatTimer -= Time.deltaTime;
            if(heartbeatTimer < 0f) {
                float heartBeatTimerMax = 15;
                heartbeatTimer = heartBeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }

    private async void CreateLobby() {
        try {
            string lobbyName = "MyLobby";
            int maxPlayers = 4;

            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions {
                IsPrivate = true,
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);
            hostLobby = lobby;

            Debug.Log("Created Lobby! " + lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Id + " " + lobby.LobbyCode);
            lt1.text = "Created Lobby! " + lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Id + " " + lobby.LobbyCode;
            lt2.text = "";
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    private async void ListLobbies() {
        try {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions {
                Count = 25,
                Filters = new List<QueryFilter> {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder> {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            Debug.Log("Lobbies found: " + queryResponse.Results.Count);
            lt1.text = "Lobbies found: " + queryResponse.Results.Count;

            string outputString = "";
            foreach (Lobby lobby in queryResponse.Results) {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers);
                outputString += lobby.Name + " " + lobby.MaxPlayers + "\n";
            }
            lt2.text = outputString;
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    private async void JoinLobbyByCode(string lobbyCode) {
        try {
            await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode);

            Debug.Log("Joined Lobby with code " + lobbyCode);
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    private async void QuickJoinLobby() {
        try {
            await LobbyService.Instance.QuickJoinLobbyAsync();
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }
}
