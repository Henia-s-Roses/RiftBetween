// RiftNetworkManager.cs    (NETWORK MANAGER)

using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using kcp2k;
using System.Net.Sockets;
using System.Net;

public class RiftNetworkManager : NetworkManager
{
    // ── INSPETCOR ─────────────────────────────────────────────────────────────

    [Header("Player Prefabs")]
    public GameObject lobbyPlayerPrefab;
    public GameObject playerPrefabA;    // LEO CHARACTER
    public GameObject playerPrefabB;    // DREI CHARACTER

    [Header("Stage Scene Names")]
    public string stage1Scene = "Stage1";
    public string stage2Scene = "Stage2";
    public string lobbyScene = "LobbyScene";

    [Header("Network Config")]
    public ushort hostPort = 67;        // predetermined port — must match on both machines
    private KcpTransport _transport;




    // for tracking connected players and gameobjects in the game
    private Dictionary<NetworkConnection, GameObject> connectedPlayers = new Dictionary<NetworkConnection, GameObject>( );

    // save player character choices based on netconn
    private Dictionary<NetworkConnection, (CharacterChoice choice, int index)> _confirmedChoices = new Dictionary<NetworkConnection, (CharacterChoice, int)>( );



    // cast the default NetworkManager singleton to RiftNetworkManager
    public static new RiftNetworkManager singleton => NetworkManager.singleton as RiftNetworkManager;



    // pub properties for lobby state
    public int PlayerCount => connectedPlayers.Count;
    public bool LobbyFull => connectedPlayers.Count >= 2;

    // ── INITIALZIATIONS ─────────────────────────────────────────────────────────────

    public override void Awake( )
    {
        base.Awake( );

        // cache transport — same GameObject as NetworkManager
        _transport = GetComponent<KcpTransport>( );
    }



    // ── Host / Client methods  ────────────────────────────────────────────────

    public void CreateLobby( )
    {
        GameSession.Reset( );   // reset session data

        // if already running or connected, stop host before creating new lobby (reset)
        if (NetworkServer.active || NetworkClient.active)
        {
            StopHost( );
        }

        // set port before starting host
        if (_transport != null) _transport.port = hostPort;

        base.StartHost( );
    }



    // join the lobby as a client using typed IP and fixed port
    public void JoinGame(string hostAddress, string portNumber)
    {
        _transport.port = ushort.Parse(portNumber);
        networkAddress = hostAddress;
        base.StartClient( );
    }


    // called by host/server after clicking "start game"
    public void StartGame( )
    {
        if (!NetworkServer.active) return;


        // require 2 players to start
        if (connectedPlayers.Count < 2)
        {
            LobbyUI.Instance.ShowStartError("Need 2 players to start.");
            return;
        }

        // get players in game then check if they have selection
        RiftNetworkPlayer[] players = FindObjectsByType<RiftNetworkPlayer>(FindObjectsSortMode.None);
        foreach (RiftNetworkPlayer plr in players)
        {
            if (plr.selectedCharacter == CharacterChoice.None)
            {
                LobbyUI.Instance.ShowStartError("All players must select a character.");
                return;
            }
        }

        // no duplicates
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



        // set game session phase to stage 1
        GameSession.CurrentPhase = GamePhase.Stage1;

        // AUDIO
        AudioManager.Instance.PlayGameStart( );
        AudioManager.Instance.PlayMusicWorldA( );

        ServerChangeScene(stage1Scene);
    }



    //// LEAVE LOBBY
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
        // create player index based on the player dict
        int playerIndex = connectedPlayers.Count + 1;

        // temp player lobby objs
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

        // work only for game scenes
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


            // LOCK IN
            RiftNetworkPlayer newNetPlayer = character.GetComponent<RiftNetworkPlayer>( );
            newNetPlayer.SetPlayerIndex(index);
            newNetPlayer.selectedCharacter = choice;
            newNetPlayer.ConfirmCharacter( );

        }


        // clear choices after stage 2 loads — stage 1 keeps them for stage 2 spawn
        if (sceneName == stage2Scene)
            _confirmedChoices.Clear( );

        if (sceneName == stage1Scene)
            AudioManager.Instance?.PlayMusicWorldA( );
        else if (sceneName == stage2Scene)
            AudioManager.Instance?.PlayMusicWorldB( );
    }


    // called when player disconnects from lobby
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (connectedPlayers.ContainsKey(conn))
            connectedPlayers.Remove(conn);


        // DISCONNECT ALL PLAYERS BACK TO LOBBY IF ONE DCs
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


    public static string GetIP( )
    {
        string ip = "";
        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName( ));
        foreach (IPAddress address in host.AddressList)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                ip = address.ToString( );
            }
        }
        return ip;
    }


}