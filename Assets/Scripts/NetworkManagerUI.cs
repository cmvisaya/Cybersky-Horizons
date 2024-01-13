using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class NetworkManagerUI : MonoBehaviour
{

    public static NetworkManagerUI Instance;
    [SerializeField] private Button serverBtn, hostBtn, clientBtn;
    [SerializeField] private Button joinBtn, refreshBtn, backBtn1, backBtn2, createBtn, initJoinBtn, startBtn;

    private string lobbyCode;

    public TextMeshProUGUI lt1, lt2, il1, il2, playerUn;
    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float heartbeatTimer, lobbyUpdateTimer;
    private string playerName;

    [SerializeField] private GameObject[] menus;

    private string KEY_START_GAME;

    private void Awake() {
        Instance = this;
        /*
        serverBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartServer();
        });
        hostBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
        });
        clientBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
        });*/

        joinBtn.onClick.AddListener(() => {
            JoinLobbyByCode(lobbyCode);
            ActivateMenu(2);
        });

        refreshBtn.onClick.AddListener(() => {
            ListLobbies();
        });

        backBtn1.onClick.AddListener(() => {
            ActivateMenu(0);
        });

        backBtn2.onClick.AddListener(() => {
            LeaveLobby();
            ActivateMenu(0);
        });

        initJoinBtn.onClick.AddListener(() => {
            ListLobbies();
            ActivateMenu(1);
        });

        createBtn.onClick.AddListener(() => {
            CreateLobby();
        });

        startBtn.onClick.AddListener(() => {
            StartGame();
        });
    }

    private async void Start() {
        playerName = "";
        KEY_START_GAME = "StartGame";

        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () => {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        ActivateMenu(0);
    }

    private void Update() {
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
    }

    private void ActivateMenu(int menuID) {
        foreach (GameObject menu in menus) {
            menu.SetActive(false);
        }
        menus[menuID].SetActive(true);
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

    private async void HandleLobbyPollForUpdates() {
        if (joinedLobby != null) {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0f) {
                float lobbyUpdateTimerMax = 1.1f;
                lobbyUpdateTimer = lobbyUpdateTimerMax;

                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;

                if (joinedLobby.Data[KEY_START_GAME].Value != "0") {
                    if (!IsLobbyHost()) {
                        ActivateMenu(3);
                        TestRelay.Instance.JoinRelay(joinedLobby.Data[KEY_START_GAME].Value);
                    }
                    joinedLobby = null;

                    //OnGameStarted?.Invoke(this, EventArgs.Empty);
                }
            }
            PrintPlayers();
            UpdateLobbyUI();
        }
    }

    private async void CreateLobby() {
        try {
            if (playerName.Length > 0) {
                string lobbyName = "MyLobby";
                int maxPlayers = 4;

                CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions {
                    IsPrivate = false,
                    Player = GetPlayer(),
                    Data = new Dictionary<string, DataObject> {
                        { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0") }
                    }
                };

                Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);
                hostLobby = lobby;
                joinedLobby = lobby;

                Debug.Log("Created Lobby! " + lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Id + " " + lobby.LobbyCode);
                il1.text = "Waiting for other players... (" + lobby.Players.Count + "/" + lobby.MaxPlayers + ")\n" + lobby.Name + " (" + lobby.LobbyCode + ")";
                PrintPlayers();
                ActivateMenu(2);
            }
            else {
                Debug.Log("Invalid username");
            }
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
                outputString += lobby.Name + " (" + lobby.Players.Count + "/" + lobby.MaxPlayers + ")\n";
            }
            lt2.text = outputString;
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    private async void JoinLobbyByCode(string lobbyCode) {
        try {
            if (playerName.Length > 0) {
                JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions {
                    Player = GetPlayer()
                };
                Lobby lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);
                joinedLobby = lobby;
                Debug.Log("Joined Lobby with code " + lobbyCode);
                il1.text = "Waiting for other players... (" + lobby.Players.Count + "/" + lobby.MaxPlayers + ")\n" + lobby.Name + " (" + lobby.LobbyCode + ")";
                PrintPlayers();
            } else {
                Debug.Log("Invalid username");
            }
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

    public async void LeaveLobby() {
        try {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
            joinedLobby = null;
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    private async void KickPlayer() {
        try {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, joinedLobby.Players[1].Id);
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    private Player GetPlayer() {
        return new Player {
            Data = new Dictionary<string, PlayerDataObject> {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
            }
        };
    }

    public bool IsLobbyHost() {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private void PrintPlayers() {
        PrintPlayers(joinedLobby);
    }
    private void PrintPlayers(Lobby lobby) {
        if(lobby != null) {
            Debug.Log("Players in Lobby " + lobby.Name);
            foreach (Player player in lobby.Players) {
                Debug.Log(player.Id + " " + player.Data["PlayerName"].Value);
            }
        }
    }

    private void UpdateLobbyUI() {
        if (joinedLobby != null) {
            string playerList = "";
            foreach (Player player in joinedLobby.Players) {
                playerList += player.Data["PlayerName"].Value + "\n";
            }
            il1.text = "Waiting for other players... (" + joinedLobby.Players.Count + "/" + joinedLobby.MaxPlayers + ")\n" + joinedLobby.Name + " (" + joinedLobby.LobbyCode + ")";
            il2.text = "Player\n\n" + playerList;
        }
    }

    public void ReadLobbyCode(string s) {
        lobbyCode = s;
    }

    public void ReadDisplayName(string s) {
        playerName = s;
        playerUn.text = s;
    }

    public async void StartGame() {
        if (IsLobbyHost()) {
            try {
                ActivateMenu(3);
                Debug.Log("Start Game");

                string relayCode = await TestRelay.Instance.CreateRelay();

                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions {
                    Data = new Dictionary<string, DataObject> {
                        { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                    }
                });

                joinedLobby = lobby;

                /*GameObject[] spawnables = GameObject.FindGameObjectsWithTag("Enemy");
                foreach (GameObject spawnable in spawnables) {
                    spawnable.GetComponent<NetworkSpawnable>().Alive();
                }*/
                
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }
}
