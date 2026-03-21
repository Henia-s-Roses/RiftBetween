// RiftNetworkPlayer.cs

// pang "initialize" ng player to the lobby, char is replaced upon slection

using Mirror;
using System.Collections;
using UnityEngine;

public enum CharacterChoice { None = 0, PixelKnight = 1, InkWanderer = 2 }

public class RiftNetworkPlayer : NetworkBehaviour
{
    // ── SYNC VARS ─ ──────────────────────────────

    // player index or lobby slot, synced for both players
    [SyncVar(hook = nameof(OnPlayerIndexChanged))]
    public int playerIndex = 0;

    [SyncVar(hook = nameof(OnCharacterChanged))]
    public CharacterChoice selectedCharacter = CharacterChoice.None;


    // homeworld based on selected character, for character-world specific buffs
    public WorldState homeWorld { get; private set; }



    // ── Server-side setup ──────────────────────────────────────────────────

    [Server]
    public void SetPlayerIndex(int index)
    {
        playerIndex = index;
    }
    


    // Called from the client when a player clicks a character button.
    [Command]
    public void CmdSelectCharacter(CharacterChoice choice)
    {
        AudioManager.Instance.PlayCharacterSelect( );


        // check other players (with networkplayer script) if taken na yung character chosen
        foreach (var netPlayer in FindObjectsByType<RiftNetworkPlayer>(FindObjectsSortMode.None))
        {
            if (netPlayer != this && netPlayer.selectedCharacter == choice)
            {

                // return if already taken
                return;
            }

        }


        // set this player's character choice, which will sync to all clients 
        selectedCharacter = choice;
    }



    // ── SyncVar hooks ────────────────────

    private void OnPlayerIndexChanged(int oldVal, int newVal)
    {
        StartCoroutine(RefreshUINextFrame( ));
    }

    // when a player selects a character, update the lobby UI for all players to see the new selection
    private void OnCharacterChanged(CharacterChoice oldVal, CharacterChoice newVal)
    {
        // refresh lobby to show change
        StartCoroutine(RefreshUINextFrame( ));
    }

    private IEnumerator RefreshUINextFrame( )
    {
        yield return null; // Wait one frame
        LobbyUI.Instance?.RefreshLobbyState( );
    }



    // ── WORLD-CHARACTER CONFIRMATION ─────────

    [Server]
    public void ConfirmCharacter( )
    {
        switch (selectedCharacter)
        {
            case CharacterChoice.PixelKnight:
                homeWorld = WorldState.WorldA;
                break;

            case CharacterChoice.InkWanderer:
                homeWorld = WorldState.WorldB;
                break;
        }
        RpcConfigureAttackMode(selectedCharacter);

    }


    [ClientRpc]
    private void RpcConfigureAttackMode(CharacterChoice character)
    {
        // set attack version of char
        var attack = GetComponent<PlayerAttack>( );
        if (attack == null) return;

        AttackMode mode = character == CharacterChoice.InkWanderer ? AttackMode.Projectile : AttackMode.Melee;


        attack.SetAttackMode(mode);
    }
}