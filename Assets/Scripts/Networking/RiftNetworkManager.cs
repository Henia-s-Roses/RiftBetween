// RiftNetworkManager.cs

using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class RiftNetworkManager : NetworkManager
{
    // ── INSPETCOR ─────────────────────────────────────────────────────────────

    [Header("Player Prefabs")]
    public GameObject playerPrefabA;    // LEO CHARACTER
    public GameObject playerPrefabB;    // DREI CHARACTER

    [Header("Stage Scene Names")]
    public string stage1Scene = "TestScene";    // should be "Stage1" for simplicity
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
        GameSession.Reset( );   // reset session data

        // if already running or connect, stop host before creating new lobby (reset)
        if (NetworkServer.active || NetworkClient.active)
        {
            StopHost( );
        }

        base.StartHost( );  
        discovery.AdvertiseServer( );   // eto yung for "available lobbies"
    }

    // join the lobby as aclient
    public void JoinGame(string hostAddress)
    {

        networkAddress = hostAddress;
        base.StartClient( );
    }


    // called by host/server after clicking "start game"
    public void StartGame( )
    {
        if (!NetworkServer.active) return;

        if (connectedPlayers.Count < 2)
        {
            LobbyUI.Instance?.ShowStartError("Need 2 players to start.");
            return;
        }

        RiftNetworkPlayer[] players = FindObjectsByType<RiftNetworkPlayer>(FindObjectsSortMode.None);

        foreach (RiftNetworkPlayer plr in players)
        {
            if (plr.selectedCharacter == CharacterChoice.None)
            {
                LobbyUI.Instance?.ShowStartError("All players must select a character.");
                return;
            }
        }

        if (players[0].selectedCharacter == players[1].selectedCharacter)
        {
            LobbyUI.Instance?.ShowStartError("Players must choose different characters.");
            return;
        }

        // Lock in characters and respawn with correct prefabs
        var respawnQueue = new List<(NetworkConnectionToClient conn, CharacterChoice choice, int index)>( );

        foreach (var p in players)
        {
            p.ConfirmCharacter( ); // lock in homeWorld on the placeholder before it's replaced

            respawnQueue.Add((
                conn: p.connectionToClient,
                choice: p.selectedCharacter,
                index: p.playerIndex
            ));
        }

        foreach (var (conn, choice, index) in respawnQueue)
        {
            RespawnWithCorrectPrefab(conn, choice, index);
        }

        discovery.StopDiscovery( );

        // set game session phase to stage 1 
        GameSession.CurrentPhase = GamePhase.Stage1;    // first stage

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
            conn.Disconnect( );

            return;
        }

        base.OnServerConnect(conn);
    }




    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        int playerIndex = connectedPlayers.Count + 1;

        // spawn player at start positions if meron, pag wala sa origin
        Transform startPos = GetStartPosition( );
        GameObject placeholder;
        if (startPos == null)
        {
            placeholder = Instantiate(playerPrefabA);

        } else
        {
            placeholder = Instantiate(playerPrefabA, startPos.position, startPos.rotation);
        }


        // officially add player to the game with Mirror's method
        NetworkServer.AddPlayerForConnection(conn, placeholder);


        // add connected player to dictionary for tracking
        RiftNetworkPlayer netPlayer = placeholder.GetComponent<RiftNetworkPlayer>( );
        netPlayer.SetPlayerIndex(playerIndex);
        connectedPlayers[conn] = placeholder;




        // trigger UI update for player conections
        StartCoroutine(NotifyUINextFrame( ));
    }

    private IEnumerator NotifyUINextFrame( )
    {
        yield return null;
        LobbyUI.Instance?.OnPlayerCountChanged(connectedPlayers.Count);
    }


    // called when player disconnects from lobby
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (connectedPlayers.ContainsKey(conn))
        {

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

        LobbyUI.Instance?.OnJoinedLobby( );
    }

    public override void OnClientDisconnect( )
    {
        LobbyUI.Instance?.OnDisconnected( );

        base.OnClientDisconnect( );
    }



    // ── util/helpers ───────────────────────────────────────────────────────────────
    private void RespawnWithCorrectPrefab(NetworkConnectionToClient conn, CharacterChoice choice, int index)
    {
        // get correct prefab from the confirmed character choice
        GameObject prefab = choice == CharacterChoice.PixelKnight
            ? playerPrefabA
            : playerPrefabB;

        Transform startPos = GetStartPosition( );
        GameObject newObj = startPos != null
            ? Instantiate(prefab, startPos.position, startPos.rotation)
            : Instantiate(prefab);


        NetworkServer.ReplacePlayerForConnection(conn, newObj, true);
        connectedPlayers[conn] = newObj;


        RiftNetworkPlayer newNetPlayer = newObj.GetComponent<RiftNetworkPlayer>( );
        newNetPlayer.SetPlayerIndex(index);
        newNetPlayer.ConfirmCharacter( );


    }



    // pub properties for lobby state
    public int PlayerCount => connectedPlayers.Count;

    public bool LobbyFull => connectedPlayers.Count >= 2;

}