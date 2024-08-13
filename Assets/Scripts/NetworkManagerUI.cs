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
    [SerializeField] private Button joinBtn, refreshBtn, backBtn1, backBtn2, backCSBtn, createBtn, initJoinBtn, startBtn, switchTeamBtn;

    private string lobbyCode;

    public TextMeshProUGUI lt1, lt2, il1, il2, playerUn, teamText, dispNameNotif, towerHealth;
    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float heartbeatTimer, lobbyUpdateTimer;
    private string playerName;

    [SerializeField] private GameObject[] menus;
    [SerializeField] private GameObject[] playerPrefabs;
    [SerializeField] private GameObject ppc;

    private string KEY_START_GAME;
    private string teamAdded = " (RED)";
    private int newClientId = 0;
    private Player thisPlayer;

    private bool gameStarted = false;

    [SerializeField] private AudioClip battleBGM;
    [SerializeField] private float bgmVol = 0.3f;

    private void Awake() {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

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
            AudioManager.Instance.PlaySoundEffect(0, 2f);
        });

        refreshBtn.onClick.AddListener(() => {
            ListLobbies();
        });

        backBtn1.onClick.AddListener(() => {
            ActivateMenu(0);
            dispNameNotif.text = "Enter Display Name";
        });

        backBtn2.onClick.AddListener(() => {
            LeaveLobby();
            ActivateMenu(0);
            dispNameNotif.text = "Enter Display Name";

        });

        backCSBtn.onClick.AddListener(() => {
            if(joinedLobby != null) LeaveLobby();
            GameManager.Instance.LoadScene(1, GameManager.GameState.IN_SELECTION_MENU);
            NetworkManager.Singleton.Shutdown();
            Cleanup();
        });

        initJoinBtn.onClick.AddListener(() => {
            if (playerName.Length > 0) {
                ListLobbies();
                ActivateMenu(1);
            } else {
                dispNameNotif.text = "Invalid Display Name!";
            }
            AudioManager.Instance.PlaySoundEffect(0, 2f);
        });

        createBtn.onClick.AddListener(() => {
            CreateLobby();
            AudioManager.Instance.PlaySoundEffect(0, 2f);
        });

        startBtn.onClick.AddListener(() => {
            StartGame();
            AudioManager.Instance.PlaySoundEffect(0, 2f);
        });

        switchTeamBtn.onClick.AddListener(() => {
            AudioManager.Instance.PlaySoundEffect(1, 2f);
            GameManager gm = GameObject.Find("GameManager").GetComponent<GameManager>();
            gm.ChangeTeam();
            switch (gm.teamId) {
                case 0:
                    teamText.text = "(Your Team: RED)";
                    UpdatePlayerTeam(" (RED)");
                    break;
                case 1:
                    teamText.text = "(Your Team: BLUE)";
                    UpdatePlayerTeam(" (BLUE)");
                    break;
            }
        });
    }

    private async void Start() {
        try {
            playerName = "";
            KEY_START_GAME = "StartGame";

            if(GameObject.Find("AuthenticatedGameObject") == null)
            {

                await UnityServices.InitializeAsync();

                AuthenticationService.Instance.SignedIn += () => {
                    Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
                };

                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                GameObject AuthenticatedGameObject = new GameObject("AuthenticatedGameObject");
                DontDestroyOnLoad(AuthenticatedGameObject);
            }
        } catch (AuthenticationException e) {
            GameManager.Instance.LoadScene(3, GameManager.GameState.IN_SELECTION_MENU);
            Debug.Log(e);
        }
        
        GameManager gm = GameObject.Find("GameManager").GetComponent<GameManager>();
        gm.SetTeam(0);
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
        try {
            if (joinedLobby != null && !gameStarted) {
                lobbyUpdateTimer -= Time.deltaTime;
                if (lobbyUpdateTimer < 0f) {
                    float lobbyUpdateTimerMax = 1.1f;
                    lobbyUpdateTimer = lobbyUpdateTimerMax;

                    Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                    joinedLobby = lobby;

                    if (joinedLobby.Data[KEY_START_GAME].Value != "0") {
                        if (!IsLobbyHost()) {
                            ActivateMenu(3);
                            AudioManager.Instance.PlayBGM(battleBGM, bgmVol);
                            TestRelay.Instance.JoinRelay(joinedLobby.Data[KEY_START_GAME].Value);
                        }
                        //joinedLobby = null;
                        gameStarted = true;

                        //OnGameStarted?.Invoke(this, EventArgs.Empty);
                    }
                }
                //PrintPlayers();
                UpdateLobbyUI();
            }
        } catch (LobbyServiceException e) {
            Debug.Log("Whyyyyyyy is this the way that it is: " + e);
        }
    }

    private async void CreateLobby() {
        try {
            if (playerName.Length > 0) {
                string lobbyName = "Tower Takedown";
                int maxPlayers = 10;

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
                //PrintPlayers();
                dispNameNotif.text = "Enter Display Name";
                ActivateMenu(2);
            }
            else {
                Debug.Log("Invalid username");
                dispNameNotif.text = "Invalid Display Name!";
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
                dispNameNotif.text = "Enter Display Name";
                ActivateMenu(2);
            } else {
                Debug.Log("Invalid username");
                dispNameNotif.text = "Invalid Display Name!";
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
            Debug.Log(joinedLobby.Id);
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

    public async void DeleteLobby() {
        try {
            Debug.Log("Am I being called from somewhere?");
            await LobbyService.Instance.DeleteLobbyAsync(hostLobby.Id);
            NetworkManager.Singleton.Shutdown();
            //Cleanup();
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    void Cleanup()
    {
        if (NetworkManager.Singleton != null)
        {
            Destroy(NetworkManager.Singleton.gameObject);
        }
    }

    public Player GetPlayer() {
        return new Player {
            Data = new Dictionary<string, PlayerDataObject> {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                { "TeamAdded", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, teamAdded) },
                { "CharCode", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "" + GameManager.Instance.selectedCharacterCode) }
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
                Debug.Log(player.Id + " " + player.Data["PlayerName"].Value + " " + player.Data["CharCode"].Value);
            }
        }
    }

    private void UpdateLobbyUI() {
        if (joinedLobby != null) {
            string playerList = "";
            foreach (Player player in joinedLobby.Players) {
                playerList += player.Data["PlayerName"].Value + player.Data["TeamAdded"].Value + "\n";
            }
            towerHealth.text = "Tower Health: " + GameObject.Find("TTRunner").GetComponent<TTRunner>().maxHp.Value;
            il1.text = "Waiting for other players... (" + joinedLobby.Players.Count + "/" + joinedLobby.MaxPlayers + ")\n" + joinedLobby.Name + " (" + joinedLobby.LobbyCode + ")";
            il2.text = "Players:\n\n" + playerList;
        }
    }

    private void UpdatePlayerTeam(string newPlayerTeam) {
        teamAdded = newPlayerTeam;
        LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions {
            Data = new Dictionary<string, PlayerDataObject> {
                { "TeamAdded", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, teamAdded) }
            }
        });
    }

    public void ReadLobbyCode(string s) {
        lobbyCode = s;
    }

    public void ReadDisplayName(string s) {
        playerName = s;
        playerUn.text = s;
        GameObject.Find("GameManager").GetComponent<GameManager>().displayName = s;
    }

    public void ReadTowerHealth(string s) {
        towerHealth.text = "Tower Health: " + s;
        int towerHp;

        bool success = int.TryParse(s, out towerHp);
        if(success) {
            GameObject.Find("TTRunner").GetComponent<TTRunner>().maxHp.Value = towerHp;
        } else {
            GameObject.Find("TTRunner").GetComponent<TTRunner>().maxHp.Value = 1350;
        }
    }

    public async void StartGame() {
        if (IsLobbyHost()) {
            try {
                PrintPlayers();
                ActivateMenu(3);
                Debug.Log("Start Game");

                string relayCode = await TestRelay.Instance.CreateRelay(joinedLobby.Players.Count);

                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions {
                    Data = new Dictionary<string, DataObject> {
                        { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                    }
                });

                joinedLobby = lobby;

                AudioManager.Instance.PlayBGM(battleBGM, bgmVol);
                GameObject.Find("TTRunner").GetComponent<TTRunner>().Init(); //UNCOMMENT THIS LINE FOR SUPPOSED REJOIN LOBBY
                //PrintPlayers();

                /*GameObject[] spawnables = GameObject.FindGameObjectsWithTag("Enemy");
                foreach (GameObject spawnable in spawnables) {
                    spawnable.GetComponent<NetworkSpawnable>().Alive();
                }*/
                
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public async void ResetLobbyData() {
        try {
            if(IsLobbyHost()) {
                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions {
                    Data = new Dictionary<string, DataObject> {
                        { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0") }
                    }
                });
            }
            gameStarted = false;
            ppc.SetActive(true);
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void ReturnToLobby() {
        try {
            //NetworkManager.Singleton.Shutdown();
            ActivateMenu(2);
            gameStarted = false;
            ppc.SetActive(true);
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }
}
