// PlayerInput.cs
// Reads from the RiftInputActions asset and dispatches to sibling components.
// Only active on the local player — all input is gated by isLocalPlayer.
//
// This script does NOT implement any logic itself.
// It purely reads input and calls methods on other components.

using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAttack))]
[RequireComponent(typeof(PlayerRevive))]
public class PlayerInput : NetworkBehaviour
{
    // ── Sibling component references ──────────────────────────────────────────
    private PlayerMovement _movement;
    private PlayerAttack _attack;
    private PlayerRevive _revive;

    // ── Input action asset ────────────────────────────────────────────────────
    private RiftInputActions _actions;

    // Cached input values read each FixedUpdate
    private Vector2 _moveInput;
    private bool _reviveHeld;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake( )
    {
        _movement = GetComponent<PlayerMovement>( );
        _attack = GetComponent<PlayerAttack>( );
        _revive = GetComponent<PlayerRevive>( );

        _actions = new RiftInputActions( );
    }

    public override void OnStartLocalPlayer( )
    {
        // Only enable input on the machine that owns this player
        _actions.Player.Enable( );
        SubscribeInputEvents( );
        RiftLogger.Log("Input enabled for local player", this);
    }

    private void OnDestroy( )
    {
        // Always clean up input subscriptions to avoid memory leaks
        if (_actions != null)
        {
            UnsubscribeInputEvents( );
            _actions.Player.Disable( );
            _actions.Dispose( );
        }
    }

    // ── Input subscriptions ───────────────────────────────────────────────────
    // Buttons use started/canceled for precise press/release detection.
    // Move uses performed/canceled for held-axis reading.

    private void SubscribeInputEvents( )
    {
        // Move — continuous axis, cached and passed to movement each FixedUpdate
        _actions.Player.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>( );
        _actions.Player.Move.canceled += ctx => _moveInput = Vector2.zero;

        // Jump — fired on press only
        _actions.Player.Jump.started += ctx => _movement.Jump( );

        // Attack — fired on press
        _actions.Player.Attack.started += ctx => _attack.TryAttack( );

        // Abilities — input wired, implementations come later
        _actions.Player.Ability1.started += ctx => OnAbility1Pressed( );
        _actions.Player.Ability2.started += ctx => OnAbility2Pressed( );

        // Revive — held input, tracked as bool each frame
        _actions.Player.Revive.started += ctx => _reviveHeld = true;
        _actions.Player.Revive.canceled += ctx => _reviveHeld = false;
    }

    private void UnsubscribeInputEvents( )
    {
        _actions.Player.Move.performed -= ctx => _moveInput = ctx.ReadValue<Vector2>( );
        _actions.Player.Move.canceled -= ctx => _moveInput = Vector2.zero;
        _actions.Player.Jump.started -= ctx => _movement.Jump( );
        _actions.Player.Attack.started -= ctx => _attack.TryAttack( );
        _actions.Player.Ability1.started -= ctx => OnAbility1Pressed( );
        _actions.Player.Ability2.started -= ctx => OnAbility2Pressed( );
        _actions.Player.Revive.started -= ctx => _reviveHeld = true;
        _actions.Player.Revive.canceled -= ctx => _reviveHeld = false;
    }

    // ── Per-frame dispatch ────────────────────────────────────────────────────

    private void Update( )
    {
        // Only the local player processes input
        if (!isLocalPlayer) return;

        // Pass move axis to movement every frame
        _movement.SetMoveInput(_moveInput);

        // Revive is a held input — notify revive component each frame
        _revive.SetReviveInput(_reviveHeld);
    }

    // ── Ability stubs ─────────────────────────────────────────────────────────
    // Wired to input but empty until PlayerAbilities is implemented.

    private void OnAbility1Pressed( )
    {
        RiftLogger.Log("Ability 1 input received (not yet implemented)", this);
        // GetComponent<PlayerAbilities>()?.TryAbility1();
    }

    private void OnAbility2Pressed( )
    {
        RiftLogger.Log("Ability 2 input received (not yet implemented)", this);
        // GetComponent<PlayerAbilities>()?.TryAbility2();
    }

    // ── Public access ─────────────────────────────────────────────────────────
    // Other scripts can check if input is coming in — useful for animator

    public Vector2 MoveInput => isLocalPlayer ? _moveInput : Vector2.zero;
    public bool ReviveHeld => isLocalPlayer && _reviveHeld;
}