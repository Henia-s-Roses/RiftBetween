// LobbyUI.cs


// Manages everything ui related

using Mirror;
using Mirror.Discovery;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    // "singleton" kinda for eassy access from other scripts 
    public static LobbyUI Instance { get; private set; }


    // screen roots
    [Header("Screens")]
    public GameObject mainMenuScreen;
    public GameObject lobbyScreen;


    // main menu buttons and lobby list
    [Header("Main Menu")]
    public Button createLobbyButton;
    public Button refreshButton;
    public Transform lobbyListParent;
    public GameObject lobbyEntryPrefab;


    // player 1 panel — name, character buttons, icon, status
    [Header("Player 1 Panel")]
    public TMP_Text player1NameLabel;
    public Button p1PickKnightButton;
    public Button p1PickWandererButton;
    public Image p1SelectedIcon;
    public TMP_Text p1StatusLabel;

    // player 2 panel — same layout as player 1
    [Header("Player 2 Panel")]
    public TMP_Text player2NameLabel;
    public Button p2PickKnightButton;
    public Button p2PickWandererButton;
    public Image p2SelectedIcon;
    public TMP_Text p2StatusLabel;


    // bottom bar — player count, start (host only), leave, error message
    [Header("Lobby Bottom Bar")]
    public TMP_Text playerCountText;
    public Button startButton;
    public Button leaveLobbyButton;
    public TMP_Text errorLabel;


    // assign character sprites in Inspector
    [Header("Character Icons")]
    public Sprite noneIcon;
    public Sprite pixelKnightIcon;
    public Sprite inkWandererIcon;


    // tracks discovered lobbies from network discovery
    private Dictionary<long, ServerResponse> discoveredServers = new Dictionary<long, ServerResponse>( );

    private bool _uiReady = false;

    // initialize singleton instance
    private void Awake( ) => Instance = this;



    private void OnEnable( )
    {
        RiftNetworkDiscovery.OnServerFound += OnServerDiscovered;
    }

    private void OnDisable( )
    {
        RiftNetworkDiscovery.OnServerFound -= OnServerDiscovered;
    }



    private void Start( )
    {
        ShowMainMenu( );

        // wire up buttons
        createLobbyButton.onClick.AddListener(OnCreateLobby);
        refreshButton.onClick.AddListener(OnRefresh);
        startButton.onClick.AddListener(OnStartGame);
        leaveLobbyButton.onClick.AddListener(OnLeaveLobby);


        // both panels send CmdSelectCharacter — server checks if choice is taken
        p1PickKnightButton.onClick.AddListener(( ) => LocalPlayer.CmdSelectCharacter(CharacterChoice.PixelKnight));
        p1PickWandererButton.onClick.AddListener(( ) => LocalPlayer.CmdSelectCharacter(CharacterChoice.InkWanderer));
        p2PickKnightButton.onClick.AddListener(( ) => LocalPlayer.CmdSelectCharacter(CharacterChoice.PixelKnight));
        p2PickWandererButton.onClick.AddListener(( ) => LocalPlayer.CmdSelectCharacter(CharacterChoice.InkWanderer));


        startButton.gameObject.SetActive(false);
        errorLabel.text = "";

        _uiReady = true;

    }

    private void OnDestroy( )
    {
        _uiReady = false;
        Instance = null;
    }

    // switch to main menu screen
    private void ShowMainMenu( )
    {
        AudioManager.Instance.PlayMusicMainMenu( );
        mainMenuScreen.SetActive(true);
        lobbyScreen.SetActive(false);
        discoveredServers.Clear( );
        ClearLobbyList( );
    }

    // switch to lobby room screen
    private void ShowLobbyRoom( )
    {
        mainMenuScreen.SetActive(false);
        lobbyScreen.SetActive(true);
        errorLabel.text = "";
        RefreshLobbyState( );
    }


    // called by RiftNetworkPlayer's SyncVar hooks whenever any player state changes
    public void RefreshLobbyState( )
    {
        RiftNetworkPlayer[] players = FindObjectsByType<RiftNetworkPlayer>(FindObjectsSortMode.None);


        // sort by playerIndex so P1 is always left, P2 always right
        System.Array.Sort(players, (a, b) => a.playerIndex.CompareTo(b.playerIndex));

        RiftNetworkPlayer p1 = players.Length > 0 ? players[0] : null;
        RiftNetworkPlayer p2 = players.Length > 1 ? players[1] : null;


        UpdatePlayerPanel(player1NameLabel, p1StatusLabel, p1SelectedIcon, p1PickKnightButton, p1PickWandererButton, p1, 1);
        UpdatePlayerPanel(player2NameLabel, p2StatusLabel, p2SelectedIcon, p2PickKnightButton, p2PickWandererButton, p2, 2);



        playerCountText.text = $"{players.Length} / 2 players";

        // only update start button if its visible (host only)
        if (startButton.gameObject.activeSelf)
            startButton.interactable = CanStartGame(p1, p2);

        if (CanStartGame(p1, p2))
            errorLabel.text = "";
    }


    // draws one player's panel based on their current state
    private void UpdatePlayerPanel(
        TMP_Text nameLabel, TMP_Text statusLabel, Image icon,
        Button pickKnight, Button pickWanderer,
        RiftNetworkPlayer netPlayer, int slotNumber)
    {

        bool slotOccupied = netPlayer != null;
        bool isLocalSlot = slotOccupied && netPlayer.isLocalPlayer;

        // show "You" tag if this is the local player's slot
        if (!slotOccupied)
        {
            nameLabel.text = slotNumber == 1 ? "Waiting..." : "Waiting for Player 2...";
        } else if (isLocalSlot)
        {
            nameLabel.text = $"Player {slotNumber} (You)";
        } else
        {
            nameLabel.text = $"Player {slotNumber}";
        }


        // only the local player can click their own panel buttons
        // also disable a button if that character is already taken by the other player
        bool knightTaken = IsCharacterTakenByOther(netPlayer, CharacterChoice.PixelKnight);
        bool wandererTaken = IsCharacterTakenByOther(netPlayer, CharacterChoice.InkWanderer);

        pickKnight.interactable = slotOccupied && isLocalSlot && !knightTaken;
        pickWanderer.interactable = slotOccupied && isLocalSlot && !wandererTaken;


        // empty slot...  dim the icon and blank the status
        if (!slotOccupied)
        {
            statusLabel.text = "—";
            icon.sprite = noneIcon;
            icon.color = new Color(1, 1, 1, 0.3f);
            return;
        }

        // update icon and status based on the player's current character pick
        if (netPlayer.selectedCharacter == CharacterChoice.PixelKnight)
        {
            statusLabel.text = "Pixel Knight ✓";
            icon.sprite = pixelKnightIcon;
            icon.color = Color.white;
        } else if (netPlayer.selectedCharacter == CharacterChoice.InkWanderer)
        {
            statusLabel.text = "Ink Wanderer ✓";
            icon.sprite = inkWandererIcon;
            icon.color = Color.white;
        } else
        {
            statusLabel.text = "No selection";
            icon.sprite = noneIcon;
            icon.color = new Color(1, 1, 1, 0.3f);
        }
    }

    // check if a character is already picked by someone other than the given player
    private bool IsCharacterTakenByOther(RiftNetworkPlayer self, CharacterChoice choice)
    {
        foreach (var p in FindObjectsOfType<RiftNetworkPlayer>( ))
        {
            if (p != self && p.selectedCharacter == choice)
                return true;
        }
        return false;
    }

    // game can only start if both players are in and picked different characters
    private bool CanStartGame(RiftNetworkPlayer p1, RiftNetworkPlayer p2)
    {
        if (p1 == null || p2 == null) return false;
        if (p1.selectedCharacter == CharacterChoice.None) return false;
        if (p2.selectedCharacter == CharacterChoice.None) return false;
        if (p1.selectedCharacter == p2.selectedCharacter) return false;
        return true;
    }



    // host creates the lobby and becomes the only one who sees the start button
    private void OnCreateLobby( )
    {
        RiftNetworkManager.singleton.CreateLobby( );
        AudioManager.Instance.PlayButtonClick( );
        ShowLobbyRoom( );
        AudioManager.Instance.PlayButtonClick( );

        startButton.gameObject.SetActive(true);
        startButton.interactable = false;
    }

    // clear discovered servers and scan again
    private void OnRefresh( )
    {
        discoveredServers.Clear( );
        AudioManager.Instance.PlayButtonClick( );

        ClearLobbyList( );
        FindObjectOfType<RiftNetworkDiscovery>( ).StartDiscovery( );
    }

    private void OnStartGame( )
    {
        RiftNetworkManager.singleton.StartGame( );
    }

    private void OnLeaveLobby( )
    {
        RiftNetworkManager.singleton.LeaveLobby( );
        AudioManager.Instance.PlayButtonClick( );

        ShowMainMenu( );
    }

    // called when network discovery finds a new server
    private void OnServerDiscovered(ServerResponse response)
    {
        if (discoveredServers.ContainsKey(response.serverId)) return;
        discoveredServers[response.serverId] = response;
        AddLobbyEntry(response);
    }

    // spawn a lobby list entry for the discovered server
    private void AddLobbyEntry(ServerResponse response)
    {
        GameObject entry = Instantiate(lobbyEntryPrefab, lobbyListParent);
        entry.GetComponent<LobbyEntry>( ).Setup(
            hostName: $"Lobby  {response.EndPoint.Address}",
            onJoin: ( ) => JoinLobby(response)
        );
    }

    private void JoinLobby(ServerResponse response)
    {
        FindObjectOfType<RiftNetworkDiscovery>( ).StopDiscovery( );
        RiftNetworkManager.singleton.JoinGame(response.EndPoint.Address.ToString( ));
    }

    private void ClearLobbyList( )
    {
        foreach (Transform child in lobbyListParent)
            Destroy(child.gameObject);
    }

    // called by RiftNetworkManager when this client successfully joins a lobby
    public void OnJoinedLobby( ) => ShowLobbyRoom( );

    // called by RiftNetworkManager when this client disconnects
    public void OnDisconnected( ) => ShowMainMenu( );

    // player count changes are handled by RefreshLobbyState
    public void OnPlayerCountChanged(int count)
    {
        RefreshLobbyState( );
    }

    // shows a validation error below the start button (e.g. "pick different characters")
    public void ShowStartError(string message)
    {
        errorLabel.text = message;
    }

    public void SetLocalPlayerLabel(int index) { }

    // get the local player's RiftNetworkPlayer so UI buttons can send Commands
    private RiftNetworkPlayer LocalPlayer
    {
        get
        {
            foreach (var p in FindObjectsOfType<RiftNetworkPlayer>( ))
            {
                if (p.isLocalPlayer) return p;
            }
            return null;
        }
    }
}

