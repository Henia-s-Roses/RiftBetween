// LobbyUI.cs


// Manages everything ui related

using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net;
using System.Net.Sockets;

public class LobbyUI : MonoBehaviour
{
    // "singleton" kinda for easy access from other scripts
    public static LobbyUI Instance { get; private set; }


    // screen roots
    [Header("Screens")]
    public GameObject mainMenuScreen;
    public GameObject lobbyScreen;


    // main menu buttons and direct connect inputs
    [Header("Main Menu BUTTONS")]
    public Button createLobbyButton;
    public Button directJoinButton;
    public TMP_InputField ipInputField;     // client types host IP here
    public TMP_InputField portInputField;

    [Header("Player 1 Objs")]
    public TMP_Text player1NameLabel;
    public Button p1PickKnightButton;
    public Button p1PickWandererButton;
    public Image p1SelectedIcon;
    public TMP_Text p1StatusLabel;

    [Header("Player 2 ui objs")]
    public TMP_Text player2NameLabel;
    public Button p2PickKnightButton;
    public Button p2PickWandererButton;
    public Image p2SelectedIcon;
    public TMP_Text p2StatusLabel;


    // lobby (inside)
    [Header("Lobby Screen")]
    public TMP_Text playerCountText;
    public TMP_Text serverDetailsLabel;     // shows host IP and port after joining
    public Button startButton;
    public Button leaveLobbyButton;
    public TMP_Text errorLabel;


    // assign character sprites in Inspector
    [Header("Character Icons")]
    public Sprite noneIcon;
    public Sprite pixelKnightIcon;
    public Sprite inkWandererIcon;


    private bool _uiReady = false;

    // initialize singleton instance
    private void Awake( ) => Instance = this;



    private void Start( )
    {
        ShowMainMenu( );

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

        if (ipInputField != null) ipInputField.text = "";
    }

    // switch to lobby room screen
    private void ShowLobbyRoom( )
    {
        mainMenuScreen.SetActive(false);
        lobbyScreen.SetActive(true);
        errorLabel.text = "";

        // clear server details until populated by OnJoinedLobby
        if (serverDetailsLabel != null) serverDetailsLabel.text = "";

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
            if (slotNumber == 1)
                nameLabel.text = "Waiting...";
            else
                nameLabel.text = "Waiting for player 2";
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

        // disable interaction if taken
        pickKnight.interactable = slotOccupied && isLocalSlot && !knightTaken;
        pickWanderer.interactable = slotOccupied && isLocalSlot && !wandererTaken;


        // empty slot — dim the icon and blank the status
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
            statusLabel.text = "Pixel Knight";
            icon.sprite = pixelKnightIcon;
            icon.color = Color.white;
        } else if (netPlayer.selectedCharacter == CharacterChoice.InkWanderer)
        {
            statusLabel.text = "Rendered Mage";
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


    // ── Button handlers — wired via Unity Inspector onClick events ────────────


    // host creates the lobby and becomes the only one who sees the start button
    public void OnCreateLobby( )
    {
        AudioManager.Instance.PlayButtonClick( );

        RiftNetworkManager.singleton.CreateLobby( );
        ShowLobbyRoom( );

        // show host IP and port in server details
        if (serverDetailsLabel != null)
            serverDetailsLabel.text = $"Your IP: {GetIP()}  |  Port: {RiftNetworkManager.singleton.hostPort}";

        startButton.gameObject.SetActive(true);
        startButton.interactable = false;
    }


    // client reads IP from input field and connects directly
    public void OnDirectJoin( )
    {
        string ip = ipInputField != null ? ipInputField.text.Trim( ) : "";
        string port = portInputField.text.Trim( );
        if (string.IsNullOrEmpty(ip))
        {
            return;
        }

        AudioManager.Instance.PlayButtonClick( );

        RiftNetworkManager.singleton.JoinGame(ip, port);
    }


    public void OnStartGame( )
    {
        RiftNetworkManager.singleton.StartGame( );
    }


    // BUTTONS CALL CMDSELECTCHARACTER from networkplayer for selection
    public void OnP1PickKnight( ) => LocalPlayer?.CmdSelectCharacter(CharacterChoice.PixelKnight);
    public void OnP1PickWanderer( ) => LocalPlayer?.CmdSelectCharacter(CharacterChoice.InkWanderer);
    public void OnP2PickKnight( ) => LocalPlayer?.CmdSelectCharacter(CharacterChoice.PixelKnight);
    public void OnP2PickWanderer( ) => LocalPlayer?.CmdSelectCharacter(CharacterChoice.InkWanderer);


    public void OnLeaveLobby( )
    {
        AudioManager.Instance.PlayButtonClick( );

        RiftNetworkManager.singleton.LeaveLobby( );
        ShowMainMenu( );
    }


    // ------------------ listener calls


    // called by RiftNetworkManager when this client successfully joins a lobby
    public void OnJoinedLobby( )
    {
        ShowLobbyRoom( );

        // display the host address the client connected to
        if (serverDetailsLabel != null)
            serverDetailsLabel.text = $"Host: {RiftNetworkManager.singleton.networkAddress}  |  Port: {RiftNetworkManager.singleton.hostPort}";
    }

    // called by RiftNetworkManager when this client disconnects
    public void OnDisconnected( ) => ShowMainMenu( );

    // player count changes are handled by RefreshLobbyState
    public void OnPlayerCountChanged(int count)
    {
        RefreshLobbyState( );
    }


    // shows a validation error below the start button
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
        Debug.Log($"IP: {ip}");
        return ip;
    }
}