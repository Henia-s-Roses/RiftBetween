// FountainPen.cs
// Auto-fire ink gun. While active, fires ink orbs at the nearest enemy
// automatically on a timer. Low damage, high fire rate.

using Mirror;
using UnityEngine;
using System.Collections;

public class FountainPen : PassiveItem
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Fountain Pen Settings")]
    public GameObject inkOrbPrefab;   // Assign OrbProjectile prefab — isEnemy = false
    public float damage = 8f;
    public float fireRate = 0.4f;    // Seconds between shots — fast fire rate
    public float range = 10f;     // Max range to detect an enemy target

    // ── State ─────────────────────────────────────────────────────────────────

    private Coroutine fireCoroutine;

    // ── PassiveItem implementation ────────────────────────────────────────────

    [Server]
    protected override void OnActivate(GameObject player)
    {
        fireCoroutine = StartCoroutine(AutoFireRoutine( ));
    }

    [Server]
    protected override void OnExpire(GameObject player)
    {
        if (fireCoroutine != null)
            StopCoroutine(fireCoroutine);
    }

    // ── Auto fire loop ────────────────────────────────────────────────────────

    private IEnumerator AutoFireRoutine( )
    {
        while (true)
        {
            yield return new WaitForSeconds(fireRate);

            if (ownerPlayer == null) yield break;

            GameObject target = FindNearestEnemy( );
            if (target == null) continue;

            FireAt(target.transform.position);
        }
    }

    private void FireAt(Vector3 targetPos)
    {
        if (inkOrbPrefab == null)
        {
            RiftLogger.Error("FountainPen: inkOrbPrefab not assigned", this);
            return;
        }

        Vector2 dir = ( targetPos - ownerPlayer.transform.position ).normalized;

        GameObject orb = Instantiate(inkOrbPrefab, ownerPlayer.transform.position, Quaternion.identity);
        orb.GetComponent<OrbProjectile>( )?.Initialize(dir, damage, isEnemy: false);
        NetworkServer.Spawn(orb);

        RiftLogger.Log($"FountainPen fired at enemy at {targetPos}", this);
    }

    // Finds the nearest enemy within range of the owning player.
    private GameObject FindNearestEnemy( )
    {
        EnemyBase[] enemies = FindObjectsOfType<EnemyBase>( );
        GameObject nearest = null;
        float minDist = range;

        foreach (var e in enemies)
        {
            float dist = Vector2.Distance(ownerPlayer.transform.position, e.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = e.gameObject;
            }
        }

        return nearest;
    }
}