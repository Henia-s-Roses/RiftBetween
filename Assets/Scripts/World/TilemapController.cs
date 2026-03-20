// TilemapController.cs
// Enables the correct tilemap root based on the active world.
// Attach this to any GameObject in the stage scene.
// Assign tilemap roots in the Inspector — leave Rift null for now if not needed.

using UnityEngine;

public class TilemapController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Tilemap Roots")]
    [Tooltip("Root GameObject containing all World A tiles")]
    public GameObject tilemapA;

    [Tooltip("Root GameObject containing all World B tiles")]
    public GameObject tilemapB;

    [Tooltip("Root GameObject for Rift tiles — leave empty for MVP")]
    public GameObject tilemapRift;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable( )
    {
        WorldManager.OnWorldChanged += HandleWorldChanged;
    }

    private void OnDisable( )
    {
        WorldManager.OnWorldChanged -= HandleWorldChanged;
    }

    private void Start( )
    {
        // Apply immediately in case WorldManager already fired before we subscribed
        if (WorldManager.Instance != null)
            HandleWorldChanged(WorldManager.Instance.CurrentWorld);
    }

    // ── World change handler ──────────────────────────────────────────────────

    private void HandleWorldChanged(WorldState world)
    {
        // Disable all first, then enable only the active one
        if (tilemapA != null) tilemapA.SetActive(false);
        if (tilemapB != null) tilemapB.SetActive(false);
        if (tilemapRift != null) tilemapRift.SetActive(false);

        switch (world)
        {
            case WorldState.WorldA:
                if (tilemapA != null) tilemapA.SetActive(true);
                break;

            case WorldState.WorldB:
                if (tilemapB != null) tilemapB.SetActive(true);
                break;

            case WorldState.Rift:
                // Rift shows both tilemaps simultaneously
                if (tilemapA != null) tilemapA.SetActive(true);
                if (tilemapB != null) tilemapB.SetActive(true);
                if (tilemapRift != null) tilemapRift.SetActive(true);
                break;
        }

        RiftLogger.Log($"TilemapController switched to {world}", this);
    }
}