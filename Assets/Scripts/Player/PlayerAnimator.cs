// PlayerAnimator.cs
// Drives the Animator Controller from live component state.
// Pure MonoBehaviour — no networking. Reads siblings, writes to Animator.
// Add animator parameters in Unity matching the names below exactly.

using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    // ── Animator parameter name constants ─────────────────────────────────────
    // Must match exactly what you name the parameters in the Animator Controller

    private static readonly int IsMoving = Animator.StringToHash("isMoving");
    private static readonly int IsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int IsDowned = Animator.StringToHash("isDowned");
    private static readonly int IsAttacking = Animator.StringToHash("isAttacking");
    private static readonly int VelocityY = Animator.StringToHash("velocityY");

    // ── References ────────────────────────────────────────────────────────────

    private Animator _animator;
    private PlayerMovement _movement;
    private PlayerAttack _attack;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake( )
    {
        _animator = GetComponent<Animator>( );
        _movement = GetComponent<PlayerMovement>( );
        _attack = GetComponent<PlayerAttack>( );
    }

    private void Update( )
    {
        // Update animator every frame from live movement state
        _animator.SetBool(IsMoving, _movement.IsMoving);
        _animator.SetBool(IsGrounded, _movement.IsGrounded);
        _animator.SetFloat(VelocityY, _movement.VerticalSpeed);
    }

    // ── Called externally ────────────────────────────────────────────────────
    // These are called by other scripts rather than polled,
    // because they're event-driven (not continuous state).

    public void SetDowned(bool downed)
    {
        _animator.SetBool(IsDowned, downed);
    }

    public void SetAttacking(bool attacking)
    {
        _animator.SetBool(IsAttacking, attacking);
    }
}