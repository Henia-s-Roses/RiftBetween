// RiftNetworkManager.cs

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


    // ── Runtime ───────────────────────────────────────────────────────────────

    // for tracking connected players and gmaeobjects in the game
    private Dictionary<NetworkConnection, GameObject> connectedPlayers = new Dictionary<NetworkConnection, GameObject>( );

    private Dictionary<NetworkConnection, (CharacterChoice choice, int index)> _confirmedChoices
        = new Dictionary<NetworkConnection, (CharacterChoice, int)>( );

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

        _confirmedChoices.Clear( );
        foreach (var p in players)
        {
            p.ConfirmCharacter( );
            _confirmedChoices[p.connectionToClient] = (p.selectedCharacter, p.playerIndex);
            RiftLogger.Log($"Confirmed: Player {p.playerIndex} = {p.selectedCharacter}", this);
        }


        discovery.StopDiscovery( );

        // set game session phase to stage 1 
        GameSession.CurrentPhase = GamePhase.Stage1;    // first stage
        AudioManager.Instance.PlayGameStart( );
        AudioManager.Instance.PlayMusicWorldA( );
        ServerChangeScene(stage1Scene); // load firt stage after character selection and confirmation
    }


    public void LeaveLobby( )
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            StopHost( );
        } else if (NetworkClient.isConnected)
        {
            StopClient( );
        }

        connectedPlayers.Clear( );

        if (SceneManager.GetActiveScene( ).name != lobbyScene)
        {
            SceneManager.LoadScene(lobbyScene);
        }
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

        GameObject lobbyObj = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, lobbyObj);

        RiftNetworkPlayer netPlayer = lobbyObj.GetComponent<RiftNetworkPlayer>( );
        netPlayer.SetPlayerIndex(playerIndex);
        connectedPlayers[conn] = lobbyObj;

        RiftLogger.Log($"Lobby player {playerIndex} connected", this);
        StartCoroutine(NotifyUINextFrame( ));
    }

    private IEnumerator NotifyUINextFrame( )
    {
        yield return null;
        LobbyUI.Instance?.OnPlayerCountChanged(connectedPlayers.Count);
    }


    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        // Only spawn character prefabs when loading a game scene
        if (sceneName != stage1Scene && sceneName != stage2Scene) return;
        if (_confirmedChoices.Count == 0) return;

        // Get start positions — collect them all so each player gets a unique one
        Transform pos1 = GetStartPosition( );
        Transform pos2 = GetStartPosition( );

        // If both returned the same (round-robin with only 1 position), offset pos2
        bool samePos = pos1 == pos2 || pos1 == null;

        int spawnIndex = 0;
        foreach (var kvp in _confirmedChoices)
        {
            NetworkConnectionToClient conn = kvp.Key as NetworkConnectionToClient;
            if (conn == null) continue;

            CharacterChoice choice = kvp.Value.choice;
            int index = kvp.Value.index;

            GameObject prefab = choice == CharacterChoice.PixelKnight
                ? playerPrefabA
                : playerPrefabB;

            // Pick spawn position — offset second player if positions are the same
            Vector3 spawnPos = Vector3.zero;
            if (pos1 != null)
                spawnPos = spawnIndex == 0
                    ? pos1.position
                    : ( samePos ? pos1.position + Vector3.right * 2f : pos2.position );

            GameObject character = Instantiate(prefab, spawnPos, Quaternion.identity);

            // Replace the lobby player object with the real character
            NetworkServer.ReplacePlayerForConnection(conn, character, true);
            connectedPlayers[conn] = character;

            // Apply identity to the new character
            RiftNetworkPlayer newNetPlayer = character.GetComponent<RiftNetworkPlayer>( );
            newNetPlayer.SetPlayerIndex(index);
            newNetPlayer.ConfirmCharacter( );

            RiftLogger.Log($"Spawned Player {index} as {choice} ({prefab.name})", this);
            spawnIndex++;
        }

        _confirmedChoices.Clear( );
    }


    // called when player disconnects from lobby
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (connectedPlayers.ContainsKey(conn))
            connectedPlayers.Remove(conn);

        bool inGame = GameSession.CurrentPhase == GamePhase.Stage1
                   || GameSession.CurrentPhase == GamePhase.Stage2;

        base.OnServerDisconnect(conn);

        // If someone disconnects mid-game → return everyone to lobby
        if (inGame)
        {
            RiftLogger.System("Player disconnected mid-game. Returning to lobby...", this);

            GameSession.Reset( );
            ServerChangeScene(lobbyScene);
        }
    }

    public override void OnClientConnect( )
    {
        base.OnClientConnect( );

        LobbyUI.Instance?.OnJoinedLobby( );
    }

    public override void OnClientDisconnect( )
    {
        LobbyUI.Instance?.OnDisconnected( );

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



    // pub properties for lobby state
    public int PlayerCount => connectedPlayers.Count;

    public bool LobbyFull => connectedPlayers.Count >= 2;

}