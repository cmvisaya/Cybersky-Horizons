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

/*
 * Script that is attached to NetworkManagerUI object, which is a child of the canvas in multiplayer online scenes
 * Not very aptly named: Handles all things regarding networking (all server-client interactions)
 */

public class NetworkManagerUI : MonoBehaviour
{

    //Singleton instance of NetworkManagerUI
    public static NetworkManagerUI Instance;

    // UI buttons used for various actions
    [SerializeField] private Button serverBtn, hostBtn, clientBtn;
    [SerializeField] private Button joinBtn, refreshBtn, backBtn1, backBtn2, backCSBtn, createBtn, initJoinBtn, startBtn, switchTeamBtn;

    // Lobby-related variables
    private string lobbyCode;
    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float heartbeatTimer, lobbyUpdateTimer;
    private string playerName;

    // UI elements and prefabs
    public TextMeshProUGUI lt1, lt2, il1, il2, playerUn, teamText, dispNameNotif, towerHealth;
    [SerializeField] private GameObject[] menus;
    [SerializeField] private GameObject[] playerPrefabs;
    [SerializeField] private GameObject ppc; // Pre-player camera - camera not attached to any player prefab

    // Key for starting the game in the lobby data
    private string KEY_START_GAME;
    private string teamAdded = " (RED)";
    private int newClientId = 0;
    private Player thisPlayer;

    // Game state variables
    private bool gameStarted = false;

    // Audio settings
    [SerializeField] private AudioClip battleBGM;
    [SerializeField] private float bgmVol = 0.3f;

    // Awake is called when the script instance is being loaded
    private void Awake() {
    // Ensure that only one instance of NetworkManagerUI exists (Singleton pattern)
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Assigning onClick listeners to buttons

        // Join lobby button
        joinBtn.onClick.AddListener(() => {
            JoinLobbyByCode(lobbyCode);
            AudioManager.Instance.PlaySoundEffect(0, 2f);
        });

        // Refresh lobby list button
        refreshBtn.onClick.AddListener(() => {
            ListLobbies();
        });

        // Back to main menu button
        backBtn1.onClick.AddListener(() => {
            ActivateMenu(0);
            dispNameNotif.text = "Enter Display Name";
        });

        // Back to main menu button from within a lobby
        backBtn2.onClick.AddListener(() => {
            LeaveLobby();
            ActivateMenu(0);
            dispNameNotif.text = "Enter Display Name";
        });

        // Back to selection menu button from the create/select screen
        backCSBtn.onClick.AddListener(() => {
            if (joinedLobby != null) LeaveLobby();
            GameManager.Instance.LoadScene(1, GameManager.GameState.IN_SELECTION_MENU);
            NetworkManager.Singleton.Shutdown();
            Cleanup();
        });

        // Initialize the join menu button
        initJoinBtn.onClick.AddListener(() => {
            if (playerName.Length > 0) {
                ListLobbies();
                ActivateMenu(1);
            } else {
                dispNameNotif.text = "Invalid Display Name!";
            }
            AudioManager.Instance.PlaySoundEffect(0, 2f);
        });

        // Create a new lobby button
        createBtn.onClick.AddListener(() => {
            CreateLobby();
            AudioManager.Instance.PlaySoundEffect(0, 2f);
        });

        // Start the game button
        startBtn.onClick.AddListener(() => {
            StartGame();
            AudioManager.Instance.PlaySoundEffect(0, 2f);
        });

        // Switch teams button
        switchTeamBtn.onClick.AddListener(() => {
            AudioManager.Instance.PlaySoundEffect(1, 2f);
            GameManager gm = GameObject.Find("GameManager").GetComponent<GameManager>();
            gm.ChangeTeam();
            
            // Update the UI and team data based on the selected team
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
            playerName = "";  // Initialize playerName to an empty string
            KEY_START_GAME = "StartGame";  // Key for starting the game

            // Initialize Unity Services and sign in the player anonymously if not already authenticated
            if (GameObject.Find("AuthenticatedGameObject") == null) {
                await UnityServices.InitializeAsync();

                // Event triggered upon successful sign-in
                AuthenticationService.Instance.SignedIn += () => {
                    Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
                };

                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                // Create a GameObject to mark that the player is authenticated, and prevent it from being destroyed on load
                GameObject AuthenticatedGameObject = new GameObject("AuthenticatedGameObject");
                DontDestroyOnLoad(AuthenticatedGameObject);
            }
        } catch (AuthenticationException e) {
            // If authentication fails, load the selection menu scene and log the error
            GameManager.Instance.LoadScene(3, GameManager.GameState.IN_SELECTION_MENU);
            Debug.Log(e);
        }

        // Set the initial team and activate the main menu
        GameManager gm = GameObject.Find("GameManager").GetComponent<GameManager>();
        gm.SetTeam(0);
        ActivateMenu(0);
    }

    private void Update() {
        HandleLobbyHeartbeat();  // Send heartbeat to keep the lobby alive
        HandleLobbyPollForUpdates();  // Poll for any updates to the lobby state
    }


    private void ActivateMenu(int menuID) {
        // Deactivate all menus
        foreach (GameObject menu in menus) {
            menu.SetActive(false);
        }
        // Activate the menu corresponding to the passed ID
        menus[menuID].SetActive(true);
    }


    private async void HandleLobbyHeartbeat() {
        if (hostLobby != null) {  // Check if the player is hosting a lobby
            heartbeatTimer -= Time.deltaTime;  // Decrement the heartbeat timer
            if (heartbeatTimer < 0f) {
                float heartBeatTimerMax = 15;  // Reset the timer for the next heartbeat
                heartbeatTimer = heartBeatTimerMax;

                // Send a heartbeat ping to keep the lobby alive
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }

    private async void HandleLobbyPollForUpdates() {
        try {
            if (joinedLobby != null && !gameStarted) {  // Check if the player has joined a lobby and the game hasn't started
                lobbyUpdateTimer -= Time.deltaTime;  // Decrement the lobby update timer
                if (lobbyUpdateTimer < 0f) {
                    float lobbyUpdateTimerMax = 1.1f;  // Reset the timer for the next update check
                    lobbyUpdateTimer = lobbyUpdateTimerMax;

                    // Fetch the latest lobby data
                    Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                    joinedLobby = lobby;

                    // Check if the game has started
                    if (joinedLobby.Data[KEY_START_GAME].Value != "0") {
                        if (!IsLobbyHost()) {  // If not the host, join the game
                            ActivateMenu(3);
                            AudioManager.Instance.PlayBGM(battleBGM, bgmVol);
                            TestRelay.Instance.JoinRelay(joinedLobby.Data[KEY_START_GAME].Value);
                        }
                        gameStarted = true;
                    }
                }
                UpdateLobbyUI();  // Update the lobby UI
            }
        } catch (LobbyServiceException e) {
            // Log any errors encountered during the lobby update process
            Debug.Log("Error while updating lobby: " + e);
        }
    }


    private async void CreateLobby() {
        try {
            if (playerName.Length > 0) {  // Check if the player name is valid
                string lobbyName = "Tower Takedown";  // Name of the lobby
                int maxPlayers = 10;  // Maximum number of players

                // Options for creating the lobby
                CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions {
                    IsPrivate = false,  // Set lobby visibility to public
                    Player = GetPlayer(),  // Add the player to the lobby
                    Data = new Dictionary<string, DataObject> {
                        { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0") }
                    }
                };

                // Create the lobby asynchronously
                Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);
                hostLobby = lobby;  // Store the created lobby as the host lobby
                joinedLobby = lobby;  // Set the created lobby as the joined lobby

                // Log lobby details and update the UI
                Debug.Log("Created Lobby! " + lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Id + " " + lobby.LobbyCode);
                il1.text = "Waiting for other players... (" + lobby.Players.Count + "/" + lobby.MaxPlayers + ")\n" + lobby.Name + " (" + lobby.LobbyCode + ")";
                ActivateMenu(2);  // Activate the lobby menu
            } else {
                // Log an error if the player name is invalid
                Debug.Log("Invalid username");
                dispNameNotif.text = "Invalid Display Name!";
            }
        } catch (LobbyServiceException e) {
            // Log any errors encountered during lobby creation
            Debug.Log(e);
        }
    }

    private async void ListLobbies() {
        try {
            // Options for querying available lobbies
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions {
                Count = 25, //Maximum number of lobbies to list
                Filters = new List<QueryFilter> {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder> {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };

            // Query the available lobbies asynchronously
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            // Update the UI with the list of lobbies
            Debug.Log("Lobbies found: " + queryResponse.Results.Count);
            lt1.text = "Lobbies found: " + queryResponse.Results.Count;

            string outputString = "";
            foreach (Lobby lobby in queryResponse.Results) {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers);
                outputString += lobby.Name + " (" + lobby.Players.Count + "/" + lobby.MaxPlayers + ")\n";
            }
            lt2.text = outputString;
        } catch (LobbyServiceException e) {
            // Log any errors
            Debug.Log(e);
        }
    }

    private async void JoinLobbyByCode(string code) {
        try {
            if (playerName.Length > 0) {  // Check if the player name is valid
                // Join the lobby by its code asynchronously
                JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions {
                    Player = GetPlayer()  // Pass the player details when joining the lobby
                };
                Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code, options);

                joinedLobby = lobby;  // Set the joined lobby

                // Log lobby details and update the UI
                Debug.Log("Joined Lobby! " + lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Id + " " + lobby.LobbyCode);
                il1.text = "Waiting for other players... (" + lobby.Players.Count + "/" + lobby.MaxPlayers + ")\n" + lobby.Name + " (" + lobby.LobbyCode + ")";
                ActivateMenu(2);  // Activate the lobby menu
            } else {
                // Log an error if the player name is invalid
                Debug.Log("Invalid username");
                dispNameNotif.text = "Invalid Display Name!";
            }
        } catch (LobbyServiceException e) {
            // Log any errors encountered during the lobby joining process
            Debug.Log("Failed to join lobby: " + e);
        }
    }

    private async void QuickJoinLobby() {
        try {
            await LobbyService.Instance.QuickJoinLobbyAsync(); // Call to LobbyService's Quick Join functionality
        } catch (LobbyServiceException e) {
            //Log any errors encountered
            Debug.Log(e);
        }
    }

    private async void LeaveLobby() {
        if (joinedLobby != null) {  // Check if the player is in a lobby
            try {
                // Leave the current lobby asynchronously
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                joinedLobby = null;  // Clear the joined lobby reference
                hostLobby = null;  // Clear the host lobby reference

                // Log the successful lobby leave
                Debug.Log("Successfully left the lobby");
            } catch (LobbyServiceException e) {
                // Log any errors encountered during the lobby leave process
                Debug.Log("Error while leaving lobby: " + e);
            }
        }
    }


    private async void KickPlayer(int joinIndex) {
        try {
            // Call to kick specific player by id
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, joinedLobby.Players[joinIndex].Id);
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void DeleteLobby() {
        try {
            // Deletes lobby when either finished with game or no more players are present in the lobby
            await LobbyService.Instance.DeleteLobbyAsync(hostLobby.Id);
            // Calls network manager shutdown to stop networking
            NetworkManager.Singleton.Shutdown();
            //Cleanup();
        } catch (LobbyServiceException e) {
            // Log all errors
            Debug.Log(e);
        }
    }

    void Cleanup()
    {
        // Destroys network manager gameobject
        if (NetworkManager.Singleton != null)
        {
            Destroy(NetworkManager.Singleton.gameObject);
        }
    }

    public Player GetPlayer() {
        // Gets the client's player data where this script's instance is located
        return new Player {
            Data = new Dictionary<string, PlayerDataObject> {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                { "TeamAdded", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, teamAdded) },
                { "CharCode", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "" + GameManager.Instance.selectedCharacterCode) }
            }
        };
    }

    public bool IsLobbyHost() {
        // Returns if client calling this function is the host of the room
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private void PrintPlayers() {
        // Helper method to print all players in the lobby that the client has joined
        PrintPlayers(joinedLobby);
    }
    private void PrintPlayers(Lobby lobby) {
        // Prints all players in a given lobby
        if(lobby != null) {
            Debug.Log("Players in Lobby " + lobby.Name);
            foreach (Player player in lobby.Players) {
                Debug.Log(player.Id + " " + player.Data["PlayerName"].Value + " " + player.Data["CharCode"].Value);
            }
        }
    }

    private void UpdateLobbyUI() {
        // Updates all lobby UI elements
        if (joinedLobby != null) {
            string playerList = "";
            foreach (Player player in joinedLobby.Players) {
                playerList += player.Data["PlayerName"].Value + player.Data["TeamAdded"].Value + "\n"; // Updates playerlist to include player name and team
            }
            towerHealth.text = "Tower Health: " + GameObject.Find("TTRunner").GetComponent<TTRunner>().maxHp.Value; // Shows towerhealth set in "TTRunner" script
            il1.text = "Waiting for other players... (" + joinedLobby.Players.Count + "/" + joinedLobby.MaxPlayers + ")\n" + joinedLobby.Name + " (" + joinedLobby.LobbyCode + ")";
            il2.text = "Players:\n\n" + playerList;
        }
    }

    private void UpdatePlayerTeam(string newPlayerTeam) {
        // Updates a client's team in the lobby data
        teamAdded = newPlayerTeam;
        LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions {
            Data = new Dictionary<string, PlayerDataObject> {
                { "TeamAdded", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, teamAdded) }
            }
        });
    }

    public void ReadLobbyCode(string s) {
        // Sets instance variable "lobbyCode"
        lobbyCode = s;
    }

    public void ReadDisplayName(string s) {
        // Sets gamemanager global variable "displayName" for proper nametag setting
        playerName = s;
        playerUn.text = s;
        GameObject.Find("GameManager").GetComponent<GameManager>().displayName = s;
    }

    public void ReadTowerHealth(string s) {
        // Reads in towers health when set by host
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
        if (IsLobbyHost()) { // Only start the game if lobby host hit the button
            try {
                PrintPlayers(); // Log all players entering the game
                ActivateMenu(3); // Activate the appropriate menu
                Debug.Log("Start Game");

                //Create a relay and have all players instantiated to a specific room using the created relay code
                string relayCode = await TestRelay.Instance.CreateRelay(joinedLobby.Players.Count);

                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions {
                    Data = new Dictionary<string, DataObject> {
                        { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                    }
                });

                joinedLobby = lobby;

                AudioManager.Instance.PlayBGM(battleBGM, bgmVol);
                GameObject.Find("TTRunner").GetComponent<TTRunner>().Init(); //UNCOMMENT THIS LINE FOR SUPPOSED REJOIN LOBBY
                
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
            ppc.SetActive(true); //Activate camera
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void ReturnToLobby() {
        // Called when game ends
        try {
            //NetworkManager.Singleton.Shutdown();
            ActivateMenu(2); //Activate lobby menu
            gameStarted = false; // Reset game started variable
            ppc.SetActive(true); // Activate camera
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }
}
