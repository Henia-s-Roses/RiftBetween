// PixelShield.cs
// Grants the player a shield with 100 HP.
// Absorbs incoming damage until the shield is depleted or the duration expires.

using Mirror;
using UnityEngine;

public class PixelShield : PassiveItem
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Shield Settings")]
    public float shieldHP = 100f;

    // ── State ─────────────────────────────────────────────────────────────────

    // Synced so a shield bar can be displayed on the HUD later
    [SyncVar(hook = nameof(OnShieldHPChanged))]
    private float currentShieldHP;

    private PlayerHealth ownerHealth;

    // ── PassiveItem implementation ────────────────────────────────────────────

    [Server]
    protected override void OnActivate(GameObject player)
    {
        currentShieldHP = shieldHP;
        ownerHealth = player.GetComponent<PlayerHealth>( );

        if (ownerHealth == null)
        {
            RiftLogger.Error("PixelShield: PlayerHealth not found on owner", this);
            return;
        }

        // Hook into the damage pipeline
        ownerHealth.OnBeforeDamage += AbsorbDamage;
        RiftLogger.Log($"PixelShield activated — {shieldHP} shield HP", this);
    }

    [Server]
    protected override void OnExpire(GameObject player)
    {
        if (ownerHealth != null)
            ownerHealth.OnBeforeDamage -= AbsorbDamage;

        RiftLogger.Log("PixelShield expired", this);
    }

    // ── Damage absorption ─────────────────────────────────────────────────────

    // Called by PlayerHealth before applying damage to the player's real HP.
    // Modifies the damage ref — any damage absorbed reduces shield HP instead.
    [Server]
    private void AbsorbDamage(ref float damage)
    {
        if (currentShieldHP <= 0) return;

        if (damage <= currentShieldHP)
        {
            // Shield fully absorbs this hit
            currentShieldHP -= damage;
            RiftLogger.Log($"Shield absorbed {damage} — shield HP now {currentShieldHP}", this);
            damage = 0f;
        } else
        {
            // Shield breaks — remaining damage bleeds through to player
            float bleedThrough = damage - currentShieldHP;
            RiftLogger.Log($"Shield broken — {bleedThrough} damage bleeds through", this);
            currentShieldHP = 0f;
            damage = bleedThrough;
            ForceExpire( );
        }
    }

    // ── SyncVar hook ──────────────────────────────────────────────────────────

    private void OnShieldHPChanged(float oldVal, float newVal)
    {
        // Hook available for a shield HP bar on the HUD
        // e.g. ShieldHUD.Instance?.UpdateShield(newVal, shieldHP);
    }
}