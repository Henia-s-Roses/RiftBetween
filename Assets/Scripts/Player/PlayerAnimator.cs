// PlayerAnimator.cs

using Mirror;
using UnityEngine;

public class PlayerAnimator : NetworkBehaviour
{
    // ── References ────────────────────────────────────────────────────────────

    [SerializeField] private Animator _animator;
    private PlayerMovement _movement;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake( )
    {
        _movement = GetComponent<PlayerMovement>( );

        if (_animator == null)
            _animator = GetComponent<Animator>( );
    }

    private void Update( )
    {
        if (!isLocalPlayer) return;
        if (_movement == null) return;

        _animator.SetBool("IsMoving", _movement.IsMoving);
        _animator.SetBool("IsGrounded", _movement.IsGrounded);
        _animator.SetFloat("VelocityY", _movement.VerticalSpeed);
        _animator.SetBool("IsSprinting", _movement._sprintHeld);
    }


    public void SetMoving(bool val) => _animator.SetBool("IsMoving", val);
    public void SetSprinting(bool val) => _animator.SetBool("IsSprinting", val);
    public void SetGrounded(bool val) => _animator.SetBool("IsGrounded", val);
    public void SetVelocityY(float val) => _animator.SetFloat("VelocityY", val);

    // ── Called by other scripts ───────────────────────────────────────────────

    public void SetDowned(bool val) => _animator.SetBool("IsDowned", val);
    public void SetDead(bool val) => _animator.SetBool("IsDead", val);
    public void SetKnockback(bool val) => _animator.SetBool("IsKnockedBack", val);
    public void TriggerAttack( ) => _animator.SetTrigger("AttackTrigger");
}