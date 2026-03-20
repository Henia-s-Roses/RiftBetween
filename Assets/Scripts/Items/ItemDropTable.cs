// ItemDropTable.cs
// Defines drop weights and resolves what item to spawn.
// Separated from Crate so weights are easy to find and tweak in one place.
// Higher weight = more common.

using UnityEngine;

[System.Serializable]
public class DropEntry
{
    public ItemType type;
    public int weight;   // Relative chance — e.g. 40 vs 5 means 8x more likely
    public GameObject prefab;
}

public class ItemDropTable : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    // Assign all item prefabs here in the Inspector.
    // Default weights match the rarity spec:
    //   SmallVial(40) > SharedPotion(25) > FountainPen(15) = PixelShield(15) > RiftShard(5)

    [Header("Drop Table — higher weight = more common")]
    public DropEntry[] entries;

    // ── Singleton ─────────────────────────────────────────────────────────────

    public static ItemDropTable Instance { get; private set; }

    private void Awake( )
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    // Returns a prefab chosen by weighted random roll, or null if table is empty.
    public GameObject Roll( )
    {
        if (entries == null || entries.Length == 0)
        {
            RiftLogger.Warn("ItemDropTable has no entries configured", this);
            return null;
        }

        int totalWeight = 0;
        foreach (var e in entries)
            totalWeight += e.weight;

        int roll = Random.Range(0, totalWeight);
        int current = 0;

        foreach (var e in entries)
        {
            current += e.weight;
            if (roll < current)
            {
                RiftLogger.Log($"Drop rolled: {e.type} (roll {roll}/{totalWeight})", this);
                return e.prefab;
            }
        }

        // Fallback — should never reach here if weights are set correctly
        return entries[0].prefab;
    }
}