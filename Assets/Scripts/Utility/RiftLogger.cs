// RiftLogger.cs
// Drop-in debug utility for Rift Between.
// Wraps Unity's Debug.Log with automatic network context stamps.
//
// Usage (from any MonoBehaviour or NetworkBehaviour):
//
//   RiftLogger.Log("Player jumped", this);
//   RiftLogger.Warn("HP is zero, triggering downed", this);
//   RiftLogger.Error("SyncVar hook fired with null reference", this);
//
// Output example:
//   [SERVER][P1-Knight][PlayerHealth] Player took 25 damage — HP now 75
//   [CLIENT][P2-Wanderer][PlayerRevive] Revive progress reset — partner left range
//   [LOCAL ][??][LobbyUI] Lobby refreshed — 2 players connected

using Mirror;
using UnityEngine;

public static class RiftLogger
{
    // ── Log levels ────────────────────────────────────────────────────────────

    // Standard info log — white in console
    public static void Log(string message, Object context = null)
    {
        if (!IsEnabled( )) return;
        Debug.Log(Format(message, context), context);
    }

    // Warning — yellow in console. Use for unexpected-but-recoverable states.
    public static void Warn(string message, Object context = null)
    {
        if (!IsEnabled( )) return;
        Debug.LogWarning(Format(message, context), context);
    }

    // Error — red in console. Use for states that should never happen.
    public static void Error(string message, Object context = null)
    {
        // Errors always print regardless of toggle
        Debug.LogError(Format(message, context), context);
    }

    // System-level log — for big lifecycle events (scene load, game start, boss phase).
    // Prints with a divider so it's easy to spot in a busy console.
    public static void System(string message, Object context = null)
    {
        if (!IsEnabled( )) return;
        Debug.Log($"══════ {Format(message, context)} ══════", context);
    }

    // ── Formatting ────────────────────────────────────────────────────────────

    private static string Format(string message, Object context)
    {
        string side = GetSideTag( );
        string player = GetPlayerTag(context);
        string source = GetSourceTag(context);
        string world = GetWorldTag( );

        return $"{side}{player}{source}{world} {message}";
    }

    // ── Tag builders ──────────────────────────────────────────────────────────

    // Which side is executing this code right now
    private static string GetSideTag( )
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            return "[HOST  ]"; // This machine is both server and client (host mode)
        if (NetworkServer.active)
            return "[SERVER]"; // Dedicated server (rare for this project but covered)
        if (NetworkClient.isConnected)
            return "[CLIENT]"; // Pure client
        return "[LOCAL ]";     // Not connected — editor or main menu
    }

    // Which player object the calling script is attached to, if any
    private static string GetPlayerTag(Object context)
    {
        if (context == null) return "[??     ]";

        // Try to get a NetworkBehaviour — works for any player/enemy/boss script
        NetworkBehaviour nb = null;

        if (context is NetworkBehaviour directNB)
            nb = directNB;
        else if (context is MonoBehaviour mb)
            nb = mb.GetComponent<NetworkBehaviour>( );

        if (nb == null) return "[??     ]";

        // Try to read RiftNetworkPlayer for a human-readable name
        var netPlayer = nb.GetComponent<RiftNetworkPlayer>( );
        if (netPlayer != null)
        {
            string charName = netPlayer.selectedCharacter switch
            {
                CharacterChoice.PixelKnight => "Knight ",
                CharacterChoice.InkWanderer => "Wandrer",
                _ => "NoChar "
            };
            string local = nb.isLocalPlayer ? "*" : " "; // * marks the local player
            return $"[P{netPlayer.playerIndex}-{charName}{local}]";
        }

        // For enemies / bosses — just show their netId
        if (nb.netIdentity != null)
            return $"[netId:{nb.netId,-3}]";

        return "[??     ]";
    }

    // The script name — trimmed to keep lines readable
    private static string GetSourceTag(Object context)
    {
        if (context == null) return "[???           ]";
        string name = context.GetType( ).Name;

        // Pad or trim to a fixed width so columns align in the console
        const int width = 20;
        if (name.Length > width) name = name.Substring(0, width);
        return $"[{name.PadRight(width)}]";
    }

    // Current world state from GameSession — useful context for almost every log
    private static string GetWorldTag( )
    {
        // GameSession may not be initialized yet in early startup
        try
        {
            return GameSession.CurrentPhase == GamePhase.Menu
                ? "[Menu  ]"
                : $"[{GameSession.ActiveWorld,-6}]";
        } catch
        {
            return "[------]";
        }
    }

    // ── Toggle ────────────────────────────────────────────────────────────────
    // Flip this false for release builds — errors still print regardless.

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static bool IsEnabled( ) => true;
#else
    private static bool IsEnabled() => false;
#endif
}