// PassiveItem.cs
// Base class for all timed passive items (FountainPen, PixelShield, RiftShard).
// Manages the duration countdown and expiry.
// Subclasses implement OnActivate() and OnExpire() for their specific behavior.

using Mirror;
using UnityEngine;
using System.Collections;

public abstract class PassiveItem : ItemPickup
{
    // ── State ─────────────────────────────────────────────────────────────────

    // Duration synced so the HUD can display a depleting bar later
    [SyncVar]
    public float remainingDuration = GameConfig.PASSIVE_DURATION;

    protected GameObject ownerPlayer;   // The player who picked this up

    // ── Pickup flow ───────────────────────────────────────────────────────────

    [Server]
    protected override void ApplyEffect(GameObject player)
    {
        ownerPlayer = player;

        // If the player already has a passive item, remove the old one first
        var existing = player.GetComponent<PassiveItem>( );
        if (existing != null && existing != this)
        {
            existing.ForceExpire( );
            RiftLogger.Log($"Replaced existing passive item on {player.name}", this);
        }

        // Parent this item to the player so it follows them
        // NetworkServer.Spawn already happened in Crate — just reparent
        transform.SetParent(player.transform);
        transform.localPosition = Vector3.zero;

        OnActivate(player);
        RiftLogger.Log($"{itemType} activated on {player.name} for {remainingDuration}s", this);

        StartCoroutine(DurationCountdown( ));
    }

    // ── Duration ──────────────────────────────────────────────────────────────

    private IEnumerator DurationCountdown( )
    {
        while (remainingDuration > 0f)
        {
            yield return new WaitForSeconds(0.1f);
            remainingDuration -= 0.1f;
        }

        ForceExpire( );
    }

    [Server]
    public void ForceExpire( )
    {
        OnExpire(ownerPlayer);
        RiftLogger.Log($"{itemType} expired on {ownerPlayer?.name}", this);
        NetworkServer.Destroy(gameObject);
    }

    // ── Interface for subclasses ──────────────────────────────────────────────

    // Called server-side when the item is first picked up.
    protected abstract void OnActivate(GameObject player);

    // Called server-side when duration ends or item is force-expired.
    protected abstract void OnExpire(GameObject player);
}