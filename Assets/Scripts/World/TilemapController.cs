// TilemapController.cs

using UnityEngine;

public class TilemapController : MonoBehaviour
{
    [Header("Tilemap Roots")]
    public GameObject tilemapA;
    public GameObject tilemapB;

    [Header("Background (optional)")]
    public SpriteRenderer backgroundRenderer;
    public Sprite backgroundA;
    public Sprite backgroundB;

    private void OnEnable( ) => WorldManager.OnWorldChanged += HandleWorldChanged;
    private void OnDisable( ) => WorldManager.OnWorldChanged -= HandleWorldChanged;

    private void Start( )
    {
        if (WorldManager.Instance != null)
            HandleWorldChanged(WorldManager.Instance.CurrentWorld);
    }

    private void HandleWorldChanged(WorldState world)
    {
        if (tilemapA != null) tilemapA.SetActive(world == WorldState.WorldA);
        if (tilemapB != null) tilemapB.SetActive(world == WorldState.WorldB);

        SwapBackground(world);
        RiftLogger.Log($"Tilemap switched to {world}", this);
    }

    private void SwapBackground(WorldState world)
    {
        if (backgroundRenderer == null) return;

        backgroundRenderer.sprite = world == WorldState.WorldA
            ? backgroundA
            : backgroundB;
    }
}