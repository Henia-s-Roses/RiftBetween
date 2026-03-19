// ReviveZone.cs
// Small trigger on a child GameObject of each player.
// Detects when the other player is within revive range.
// Reports proximity to PlayerRevive — does not accumulate progress itself.
//
// Setup:
//   1. Create an empty child GameObject on your player prefab — name it "ReviveZone"
//   2. Add CircleCollider2D — set as Trigger, radius = GameConfig.REVIVE_RANGE (~1.2)
//   3. Add this script
//   4. Set the child's layer to match its player (PlayerA or PlayerB)
//   5. Use Physics2D layer collision matrix to only detect the OPPOSITE player layer

using UnityEngine;

public class ReviveZone : MonoBehaviour
{
    // Set automatically from parent — no Inspector wiring needed
    private PlayerRevive _ownerRevive;

    private void Awake( )
    {
        // Walk up to parent to get the PlayerRevive component
        _ownerRevive = GetComponentInParent<PlayerRevive>( );

        if (_ownerRevive == null)
            RiftLogger.Error("ReviveZone has no PlayerRevive in parent", this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check the other collider belongs to a player (not enemy, not item)
        if (other.GetComponent<RiftNetworkPlayer>( ) == null) return;

        RiftLogger.Log("Partner entered revive range", this);
        _ownerRevive.SetPartnerInRange(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<RiftNetworkPlayer>( ) == null) return;

        RiftLogger.Log("Partner left revive range", this);
        _ownerRevive.SetPartnerInRange(false);
    }
}