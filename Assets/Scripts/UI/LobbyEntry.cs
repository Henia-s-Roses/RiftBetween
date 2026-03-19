// LobbyEntry.cs


// script attached to lobbyentry prefabs in the lobby list
// for setting up lobby details (UI)

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LobbyEntry : MonoBehaviour
{
    public TMP_Text hostNameText;
    public Button joinButton;


    // Called by LobbyUI when a lobby entry is created, sets up the lobby details
    public void Setup(string hostName, Action onJoin)
    {
        hostNameText.text = hostName;

        // bind aciton to button listener
        joinButton.onClick.AddListener(( ) => onJoin( ));
    }
}