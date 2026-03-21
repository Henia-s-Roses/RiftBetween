// RiftNetworkManager.cs    (NETWORK MANAGER)

using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

public class RiftNetworkManager : NetworkManager
{
    // ── INSPETCOR ─────────────────────────────────────────────────────────────

    [Header("Player Prefabs")]
    public GameObject lobbyPlayerPrefab;
    public GameObject playerPrefabA;    // LEO CHARACTER
    public GameObject playerPrefabB;    // DREI CHARACTER

    [Header("Stage Scene Names")]
    public string stage1Scene = "Stage1";    // should be "Stage1" for simplicity
                                                // replace "GameScene" after tesiting
    public string stage2Scene = "Stage2";       // "Stage2"
    public string lobbyScene = "LobbyScene";



    // for tracking connected players and gmaeobjects in the game
    private Dictionary<NetworkConnection, GameObject> connectedPlayers = new Dictionary<NetworkConnection, GameObject>( );

    // save player character choices based on netconn
    private Dictionary<NetworkConnection, (CharacterChoice choice, int index)> _confirmedChoices = new Dictionary<NetworkConnection, (CharacterChoice, int)>( );

    // will be used for lan dicovery, "avialble lobbies"
    private RiftNetworkDiscovery discovery;

    // cast the default NetworkManager singleton to RiftNetworkManager
    public static new RiftNetworkManager singleton => NetworkManager.singleton as RiftNetworkManager;





    // pub properties for lobby state
    public int PlayerCount => connectedPlayers.Count;

    public bool LobbyFull => connectedPlayers.Count >= 2;

    // ── INITIALZIATIONS ─────────────────────────────────────────────────────────────

    public override void Awake( )
    {
        base.Awake( );

        discovery = GetComponent<RiftNetworkDiscovery>( );
    }



    // ── Host / Client methods  ────────────────────────────────────────────────

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


        // require 2 palyers to start
        if (connectedPlayers.Count < 2)
        {
            LobbyUI.Instance.ShowStartError("Need 2 players to start.");
            return;
        }

        // get players in game then check if theey have slection
        RiftNetworkPlayer[] players = FindObjectsByType<RiftNetworkPlayer>(FindObjectsSortMode.None);
        foreach (RiftNetworkPlayer plr in players)
        {
            if (plr.selectedCharacter == CharacterChoice.None)
            {
                LobbyUI.Instance.ShowStartError("All players must select a character.");
                return;
            }
        }
        
        // no dup
        if (players[0].selectedCharacter == players[1].selectedCharacter)
        {
            LobbyUI.Instance.ShowStartError("Players must choose different characters.");
            return;
        }



        _confirmedChoices.Clear( );
        foreach (var p in players)
        {
            p.ConfirmCharacter( );
            _confirmedChoices[p.connectionToClient] = (p.selectedCharacter, p.playerIndex);
        }



        discovery.StopDiscovery( );

        // set game session phase to stage 1 
        GameSession.CurrentPhase = GamePhase.Stage1;    // first stage
        
        // AUDIO
        AudioManager.Instance.PlayGameStart( );
        AudioManager.Instance.PlayMusicWorldA( );
        
        ServerChangeScene(stage1Scene); // load firt stage after character selection and confirmation
    }



    //// LEEAVE LOBBY
    public void LeaveLobby( )
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            StopHost( );
        } else if (NetworkClient.isConnected)
        {
            StopClient( );
        }

        // clear connected players
        connectedPlayers.Clear( );

        // go back to lobby scene if from stage scenes
        if (SceneManager.GetActiveScene( ).name != lobbyScene)
        {
            SceneManager.LoadScene(lobbyScene);
        }
    }






    // ── Mirror meth overrides ──────────────────────────────────────────────────

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
        // create player index basd on the playre dict
        int playerIndex = connectedPlayers.Count + 1;

        // temp plyr lobbt objs
        GameObject lobbyObj = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, lobbyObj);


        // set player index for player tracking on network player
        RiftNetworkPlayer netPlayer = lobbyObj.GetComponent<RiftNetworkPlayer>( );
        netPlayer.SetPlayerIndex(playerIndex);
        connectedPlayers[conn] = lobbyObj;


        StartCoroutine(PrepUI( ));
    }

    private IEnumerator PrepUI( )
    {
        yield return null;
        LobbyUI.Instance?.OnPlayerCountChanged(connectedPlayers.Count);
    }


    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        // work only for lobbies that can start
        if (sceneName != stage1Scene && sceneName != stage2Scene) return;
        if (_confirmedChoices.Count == 0) return;


        // get start positions 
        List<Vector3> spawnPositions = new List<Vector3>( );
        if (startPositions != null && startPositions.Count > 0)
        {
            foreach (Transform sp in startPositions)
                spawnPositions.Add(sp.position);
        }



        while (spawnPositions.Count < _confirmedChoices.Count)
            spawnPositions.Add(new Vector3(spawnPositions.Count * 3f, 0f, 0f));



        var sorted = new List<KeyValuePair<NetworkConnectionToClient, (CharacterChoice choice, int index)>>( );


        // add the confirmed choices of clients with index
        foreach (var keyvalpair in _confirmedChoices)
        {
            var conn = keyvalpair.Key as NetworkConnectionToClient;
            if (conn != null)
                sorted.Add(new KeyValuePair<NetworkConnectionToClient, (CharacterChoice, int)>(conn, keyvalpair.Value));
        }

        // sort properly for no mixups
        sorted.Sort((a, b) => a.Value.index.CompareTo(b.Value.index));


        for (int i = 0; i < sorted.Count; i++)
        {
            var conn = sorted[i].Key;
            var choice = sorted[i].Value.choice;
            var index = sorted[i].Value.index;

            GameObject prefab;

            if (choice == CharacterChoice.PixelKnight)
                prefab = playerPrefabA;
            else
                prefab = playerPrefabB;


            // START SPAWNING PLAYER OBJECTS
                Vector3 spawnPos = spawnPositions[i];
            GameObject character = Instantiate(prefab, spawnPos, Quaternion.identity);


            NetworkServer.ReplacePlayerForConnection(conn, character, true);
            connectedPlayers[conn] = character;


            // LOCKI N
            RiftNetworkPlayer newNetPlayer = character.GetComponent<RiftNetworkPlayer>( );
            newNetPlayer.SetPlayerIndex(index);
            newNetPlayer.selectedCharacter = choice;
            newNetPlayer.ConfirmCharacter( );

        }


        // for scene change  to scene 2
        if (sceneName == stage2Scene)
            _confirmedChoices.Clear( );

        if (sceneName == stage2Scene)
            AudioManager.Instance?.PlayMusicWorldB( );
    }


    // called when player disconnects from lobby
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (connectedPlayers.ContainsKey(conn))
            connectedPlayers.Remove(conn);


        // DISCONNECT ALL PALYERS BaCK TO LOBBY iF ONE DCs
        bool inGame = GameSession.CurrentPhase == GamePhase.Stage1 || GameSession.CurrentPhase == GamePhase.Stage2;

        base.OnServerDisconnect(conn);

        if (inGame)
        {

            GameSession.Reset( );
            ServerChangeScene(lobbyScene);
        }
    }

    public override void OnClientConnect( )
    {
        base.OnClientConnect( );

        LobbyUI.Instance.OnJoinedLobby( );
    
    
    }

    public override void OnClientDisconnect( )
    {
        LobbyUI.Instance.OnDisconnected( );



        if (SceneManager.GetActiveScene( ).name != lobbyScene)
        {
            SceneManager.LoadScene(lobbyScene);
        }

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



}