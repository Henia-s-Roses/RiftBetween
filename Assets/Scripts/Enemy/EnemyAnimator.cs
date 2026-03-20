// EnemyAnimator.cs
// Drives the Animator Controller for all enemy types.
// Attach this to every enemy prefab alongside EnemyBase.
// EnemyBase and subclasses call into this — it never reads state itself.

using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    // ── Parameter hashes ──────────────────────────────────────────────────────
    // Must match exactly what is typed in the Animator Controller Parameters tab.

    private static readonly int P_IsMoving = Animator.StringToHash("isMoving");
    private static readonly int P_HitTrigger = Animator.StringToHash("hitTrigger");
    private static readonly int P_AttackTrigger = Animator.StringToHash("attackTrigger");
    private static readonly int P_DieTrigger = Animator.StringToHash("dieTrigger");

    // ── References ────────────────────────────────────────────────────────────

    private Animator _animator;
    private bool _isDead = false; // Prevents any trigger from firing after death

    // ── Valid parameter cache — prevents hash errors ───────────────────────────

    private System.Collections.Generic.HashSet<int> _validParams
        = new System.Collections.Generic.HashSet<int>( );

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake( )
    {
        _animator = GetComponent<Animator>( );
        BuildValidParams( );
    }

    private void BuildValidParams( )
    {
        if (_animator == null) return;
        _validParams.Clear( );
        foreach (var p in _animator.parameters)
            _validParams.Add(p.nameHash);
    }

    // ── Public API — called by EnemyBase and subclasses ───────────────────────

    // Call every frame from FixedUpdate — pass true if enemy is actively moving
    public void SetMoving(bool moving)
    {
        if (_isDead) return;
        SafeSetBool(P_IsMoving, moving);
    }

    // Call when the enemy successfully lands a hit on a player
    public void TriggerAttack( )
    {
        if (_isDead) return;
        SafeSetTrigger(P_AttackTrigger);
    }

    // Call when the enemy takes damage
    public void TriggerHit( )
    {
        if (_isDead) return;
        SafeSetTrigger(P_HitTrigger);
    }

    // Call once when the enemy dies — locks out all further animation calls
    public void TriggerDeath( )
    {
        if (_isDead) return;
        _isDead = true;
        SafeSetTrigger(P_DieTrigger);
    }

    // ── Safe wrappers ─────────────────────────────────────────────────────────

    private void SafeSetBool(int hash, bool val)
    {
        if (_validParams.Contains(hash))
            _animator.SetBool(hash, val);
    }

    private void SafeSetTrigger(int hash)
    {
        if (_validParams.Contains(hash))
            _animator.SetTrigger(hash);
    }
}