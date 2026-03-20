// ReviveZone.cs
// Child trigger on each player prefab.
// Only reports partner-in-range when that partner is actually downed.
// Attach to a child GameObject named "ReviveZone".

using UnityEngine;

public class ReviveZone : MonoBehaviour
{
    private PlayerRevive _ownerRevive;

    private void Awake( )
    {
        _ownerRevive = GetComponentInParent<PlayerRevive>( );

        if (_ownerRevive == null)
            RiftLogger.Error("ReviveZone: no PlayerRevive found in parent", this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!ShouldReact(other)) return;

        // Only signal in-range if the partner is actually downed
        var partnerRevive = other.GetComponentInParent<PlayerRevive>( );
        if (partnerRevive != null && partnerRevive.isDown)
        {
            RiftLogger.Log("Downed partner entered revive range", this);
            _ownerRevive.SetPartnerInRange(true);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Handles the case where partner was already in range when they went down
        if (!ShouldReact(other)) return;

        var partnerRevive = other.GetComponentInParent<PlayerRevive>( );
        if (partnerRevive != null && partnerRevive.isDown && !_ownerRevive.IsPartnerInRange)
        {
            RiftLogger.Log("Partner downed while already in range", this);
            _ownerRevive.SetPartnerInRange(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!ShouldReact(other)) return;

        RiftLogger.Log("Partner left revive range", this);
        _ownerRevive.SetPartnerInRange(false);
    }

    // Check the collider belongs to a player — look for PlayerRevive in parent
    // rather than RiftNetworkPlayer, since the collider may be on a child object
    private bool ShouldReact(Collider2D other)
    {
        if (_ownerRevive == null) return false;

        var otherRevive = other.GetComponentInParent<PlayerRevive>( );
        if (otherRevive == null) return false;           // Not a player
        if (otherRevive == _ownerRevive) return false;   // Own collider — ignore self

        return true;
    }
}