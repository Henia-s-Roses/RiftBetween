// PlayerMovement.cs
// Handles horizontal movement and jumping.
// Movement input comes from PlayerInput every frame via SetMoveInput().
// Server-authoritative position via NetworkTransform.
// Facing direction is synced via SyncVar — flips the entire GameObject
// so all child objects (hitbox, projectile origin) flip automatically.

using Mirror;
using System.Collections;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Movement")]
    [SerializeField] private float moveSpeed = GameConfig.PLAYER_MOVE_SPEED;
    [SerializeField] private float sprintSpeed = GameConfig.PLAYER_SPRINT_SPEED;
    [SerializeField] private float jumpForce = GameConfig.PLAYER_JUMP_FORCE;

    [Header("Ground Check")]
    [Tooltip("Empty child GameObject placed at the player's feet")]
    [SerializeField] private Transform groundCheck;
    [Tooltip("Layer(s) considered as ground — set in Inspector")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.15f;

    // ── Synced state ──────────────────────────────────────────────────────────

    // Flips the entire GameObject so hitbox and projectile origin follow automatically.
    // SyncVar ensures all clients see the correct facing direction.
    [SyncVar(hook = nameof(OnFacingChanged))]
    private bool _facingRight = true;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private Rigidbody2D _rb;
    private PlayerAttack _attack;

    private Vector2 _moveInput;
    private bool _isGrounded;
    private bool _movementLocked; // Set true when player is downed
    private bool _sprintHeld;


    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake( )
    {
        _rb = GetComponent<Rigidbody2D>( );
        _attack = GetComponent<PlayerAttack>( );
    }

    private void FixedUpdate( )
    {
        // Only the local player drives input — NetworkTransform syncs position
        if (!isLocalPlayer) return;

        CheckGrounded( );
        ApplyMovement( );
    }

    // ── Ground detection ──────────────────────────────────────────────────────

    private void CheckGrounded( )
    {
        _isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    // ── Movement ──────────────────────────────────────────────────────────────

    private void ApplyMovement( )
    {
        if (_movementLocked)
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            return;
        }

        float speed = _moveInput.x != 0f
            ? ( _sprintHeld ? sprintSpeed : moveSpeed )
            : 0f;
        _rb.linearVelocity = new Vector2(_moveInput.x * speed, _rb.linearVelocity.y);

        // Only send a Command when direction actually changes — not every frame.
        // CmdSetFacing updates the SyncVar which triggers OnFacingChanged on all clients.
        if (_moveInput.x > 0f && !_facingRight)
            CmdSetFacing(true);
        else if (_moveInput.x < 0f && _facingRight)
            CmdSetFacing(false);
    }

    // ── Facing sync ───────────────────────────────────────────────────────────

    // Local player tells the server which way they are facing.
    // Server updates the SyncVar which replicates to all clients.
    [Command]
    private void CmdSetFacing(bool facingRight)
    {
        _facingRight = facingRight;
    }

    // Fires on ALL clients (including host) whenever _facingRight changes.
    // This is the single place where the flip is applied — never set
    // localScale.x directly anywhere else.
    private void OnFacingChanged(bool oldVal, bool newVal)
    {
        ApplyFlip(newVal);
        _attack?.SetFacingDirection(newVal ? 1f : -1f);
    }

    // Flips the entire GameObject by inverting localScale.x.
    // All children (attack hitbox, projectile origin, sprite) flip with it.
    private void ApplyFlip(bool facingRight)
    {
        Vector3 scale = transform.localScale;
        scale.x = facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    // ── Called by PlayerInput ─────────────────────────────────────────────────

    public void SetMoveInput(Vector2 input)
    {
        if (!isLocalPlayer) return;
        _moveInput = input;
    }

    public void Jump( )
    {
        if (!isLocalPlayer) return;
        if (_movementLocked) return;
        if (!_isGrounded) return;

        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
        RiftLogger.Log("Jump", this);
    }

    public void SetSprint(bool held)
    {
        if (!isLocalPlayer) return;
        _sprintHeld = held;
    }

    // ── Called by PlayerRevive ────────────────────────────────────────────────

    public void SetMovementLocked(bool locked)
    {
        _movementLocked = locked;

        if (locked)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.bodyType = RigidbodyType2D.Kinematic;
        } else
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
        }

        RiftLogger.Log($"Movement locked: {locked}", this);
    }

    // ── Buffs ─────────────────────────────────────────────────────────────────

    public void ApplySpeedBuff(float multiplier, float duration)
    {
        StartCoroutine(SpeedBuffCoroutine(multiplier, duration));
    }

    private IEnumerator SpeedBuffCoroutine(float multiplier, float duration)
    {
        moveSpeed *= multiplier;
        sprintSpeed *= multiplier;
        RiftLogger.Log($"Speed buff active — {multiplier}x for {duration}s", this);

        yield return new WaitForSeconds(duration);

        moveSpeed /= multiplier;
        sprintSpeed /= multiplier;
        RiftLogger.Log("Speed buff expired", this);
    }

    // ── Public reads ──────────────────────────────────────────────────────────

    public bool IsGrounded => _isGrounded;
    public bool IsMoving => Mathf.Abs(_moveInput.x) > 0.01f;
    public float HorizontalSpeed => Mathf.Abs(_rb.linearVelocity.x);
    public float VerticalSpeed => _rb.linearVelocity.y;
    public float FacingDirection => _facingRight ? 1f : -1f;
}