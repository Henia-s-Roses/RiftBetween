// RiftNetworkManager.cs

using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class RiftNetworkManager : NetworkManager
{
    // ── INSPETCOR ─────────────────────────────────────────────────────────────

    [Header("Player Prefabs")]
    public GameObject playerPrefabA;    // LEO CHARACTER
    public GameObject playerPrefabB;    // DREI CHARACTER

    [Header("Stage Scene Names")]
    public string stage1Scene = "GameScene";    // should be "Stage1" for simplicity
                                                // replace "GameScene" after tesiting
    public string stage2Scene = "Stage2";       // "Stage2"




    // ── Runtime ───────────────────────────────────────────────────────────────

    // for tracking connected players and gmaeobjects in the game
    private Dictionary<NetworkConnection, GameObject> connectedPlayers = new Dictionary<NetworkConnection, GameObject>( );

    // will be used for lan dicovery, "avialble lobbies"
    private RiftNetworkDiscovery discovery;

    // cast the default NetworkManager singleton to RiftNetworkManager
    public static new RiftNetworkManager singleton => NetworkManager.singleton as RiftNetworkManager;

   
    
    // ── INITIALZIATIONS ─────────────────────────────────────────────────────────────

    public override void Awake( )
    {
        base.Awake( );

        discovery = GetComponent<RiftNetworkDiscovery>( );
    }



    // ── Host / Client controls ────────────────────────────────────────────────

    public void CreateLobby( )
    {
        RiftLogger.Log("CreateLobby called", this);
        GameSession.Reset( );   // reset session data

        // if already running or connect, stop host before creating new lobby (reset)
        if (NetworkServer.active || NetworkClient.active)
        {
            RiftLogger.Warn("Already active — stopping before restarting", this);
            StopHost( );
        }

        base.StartHost( );
        discovery.AdvertiseServer( );   // eto yung for "available lobbies"
        RiftLogger.Log("Host started, advertising on LAN", this);
    }

    // join the lobby as aclient
    public void JoinGame(string hostAddress)
    {
        RiftLogger.Log($"Joining host at {hostAddress}", this);

        networkAddress = hostAddress;
        base.StartClient( );
    }


    // called by host/server after clicking "start game"
    public void StartGame( )
    {
        // prevent start if not host/server
        if (!NetworkServer.active)
        {
            RiftLogger.Error("StartGame called but not server", this);

            return;

        }

        // mustbe 2 players befoer start
        if (connectedPlayers.Count < 2)
        {

            LobbyUI.Instance?.ShowStartError("Need 2 players to start.");
            return;
        }

        RiftNetworkPlayer[] players = FindObjectsByType<RiftNetworkPlayer>(FindObjectsSortMode.None);
        foreach (RiftNetworkPlayer plr in players)
        {
            // check if all players have selected a character before starting
            if (plr.selectedCharacter == CharacterChoice.None)
            {
                LobbyUI.Instance.ShowStartError("All players must select a character.");
                return;
            }
        }

        // prevent same character slection
        if (players[0].selectedCharacter == players[1].selectedCharacter)
        {
            LobbyUI.Instance.ShowStartError("Players must choose different characters.");
            return;
        }

        // Lock in characters and respawn with correct prefabs
        foreach (var p in players)
        {

            p.ConfirmCharacter( );  // set homeworld for buffs and other character-specific logic i
            RespawnWithCorrectPrefab(p);
        }

        discovery.StopDiscovery( );

        // set game session phase to stage 1 
        GameSession.CurrentPhase = GamePhase.Stage1;    // first stage

        RiftLogger.System("Starting game — loading Stage 1", this);
        ServerChangeScene(stage1Scene); // load firt stage after character selection and confirmation
    }


    public void LeaveLobby( )
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            StopHost( );
        else if (NetworkClient.isConnected)
            StopClient( );

        connectedPlayers.Clear( );
    }






    // ── Mirror overrides ──────────────────────────────────────────────────────

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        // prevent more than 2 players from connecting to the lobby
        if (connectedPlayers.Count >= 2)
        {
            RiftLogger.Warn($"Rejected connection — lobby full", this);
            conn.Disconnect( );

            return;
        }

        base.OnServerConnect(conn);
        RiftLogger.Log($"Client connected — conn {conn.connectionId}", this);
    }




    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        int playerIndex = connectedPlayers.Count + 1;

        // spawn player at start positions if meron, pag wala sa origin
        Transform startPos = GetStartPosition( );
        GameObject placeholder;
        if (startPos == null)
        {
            RiftLogger.Warn("No start positions defined — spawning player at origin", this);
            placeholder = Instantiate(playerPrefabA);

        } else
        {
            RiftLogger.Log($"Spawning player at {startPos.position}", this);
            placeholder = Instantiate(playerPrefabA, startPos.position, startPos.rotation);
        }


        // officially add player to the game with Mirror's method
        NetworkServer.AddPlayerForConnection(conn, placeholder);


        // add connected player to dictionary for tracking
        RiftNetworkPlayer netPlayer = placeholder.GetComponent<RiftNetworkPlayer>( );
        netPlayer.SetPlayerIndex(playerIndex);
        connectedPlayers[conn] = placeholder;




        RiftLogger.Log($"Player {playerIndex} added to lobby", this);

        // trigger UI update for player conections
        LobbyUI.Instance.OnPlayerCountChanged(connectedPlayers.Count);
    }


    // called when player disconnects from lobby
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (connectedPlayers.ContainsKey(conn))
        {

            RiftLogger.Log($"Player disconnected from lobby", this);
            connectedPlayers.Remove(conn);
        }

        // if player disconnects during game, trigger game over to other player
        bool inGame = GameSession.CurrentPhase == GamePhase.Stage1
                   || GameSession.CurrentPhase == GamePhase.Stage2;

        //if (inGame)
            //GameOverManager.Instance?.TriggerGameOver("A player disconnected.");
        //else
        //    LobbyUI.Instance.OnPlayerCountChanged(connectedPlayers.Count);


        base.OnServerDisconnect(conn);

    }

    public override void OnClientConnect( )
    {
        base.OnClientConnect( );
        RiftLogger.Log("OnClientConnect fired", this);

        LobbyUI.Instance.OnJoinedLobby( );
    }

    public override void OnClientDisconnect( )
    {
        RiftLogger.Log("OnClientDisconnect fired", this);
        LobbyUI.Instance.OnDisconnected( );

        base.OnClientDisconnect( );
    }



    // ── util/helpers ───────────────────────────────────────────────────────────────
    private void RespawnWithCorrectPrefab(RiftNetworkPlayer netPlayer)
    {

        NetworkConnectionToClient conn = netPlayer.connectionToClient;
        GameObject oldObj = netPlayer.gameObject;


        // get correct prefab based on char choice
        GameObject prefab;
        if (netPlayer.selectedCharacter == CharacterChoice.PixelKnight)
            prefab = playerPrefabA;
        else prefab = playerPrefabB;



        // spawn new plyrobj on correct pos
        Transform startPos = GetStartPosition( );
        GameObject newObj;
        if (startPos != null) newObj = Instantiate(prefab, startPos.position, startPos.rotation);
        else newObj = Instantiate(prefab);

        // replace player object for the connection with the new prefab
        NetworkServer.ReplacePlayerForConnection(conn, newObj, true);
        connectedPlayers[conn] = newObj;


        RiftNetworkPlayer newNetPlayer = newObj.GetComponent<RiftNetworkPlayer>( );
        newNetPlayer.SetPlayerIndex(netPlayer.playerIndex);
        newNetPlayer.ConfirmCharacter( );



        NetworkServer.Destroy(oldObj);

        RiftLogger.Log($"Respawned player {netPlayer.playerIndex} with correct prefab", this);
    }



    // pub properties for lobby state
    public int PlayerCount => connectedPlayers.Count;

    public bool LobbyFull => connectedPlayers.Count >= 2;

}