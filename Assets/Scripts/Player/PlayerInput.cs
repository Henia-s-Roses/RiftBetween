// PlayerInput.cs
// Reads from the RiftInputActions asset and dispatches to sibling components.
// Only active on the local player — all input is gated by isLocalPlayer.
//
// This script does NOT implement any logic itself.
// It purely reads input and calls methods on other components.

using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : NetworkBehaviour
{
    // ── Sibling component references ──────────────────────────────────────────

    private PlayerMovement _movement;
    private PlayerAttack _attack;
    private PlayerRevive _revive;   // Optional — may not be on prefab during early testing

    // ── Cached input state ────────────────────────────────────────────────────

    private Vector2 _moveInput;
    private bool _reviveHeld;

    // ── Cached action references ──────────────────────────────────────────────
    // Stored so we can unsubscribe the exact same delegate in OnDestroy.
    // Lambda subscriptions can't be unsubscribed — named methods are required.

    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _sprintAction;
    private InputAction _attackAction;
    private InputAction _ability1Action;
    private InputAction _ability2Action;
    private InputAction _reviveAction;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake( )
    {
        _movement = GetComponent<PlayerMovement>( );
        _attack = GetComponent<PlayerAttack>( );
        _revive = GetComponent<PlayerRevive>( );  // Null-safe — not required to exist yet
    }

    public override void OnStartLocalPlayer( )
    {
        CacheActions( );
        SubscribeInputEvents( );
        RiftLogger.Log("Input enabled for local player", this);
    }

    private void OnDestroy( )
    {
        // CRITICAL: always unsubscribe on destroy.
        // The Input System holds references to these delegates — if we don't
        // unsubscribe, callbacks fire on the destroyed object next input frame.
        UnsubscribeInputEvents( );
        RiftLogger.Log("Input unsubscribed on destroy", this);
    }

    // ── Action caching ────────────────────────────────────────────────────────

    private void CacheActions( )
    {
        // Cache each action by name once — avoids repeated FindAction calls per frame
        _moveAction = InputSystem.actions.FindAction("Move");
        _jumpAction = InputSystem.actions.FindAction("Jump");
        _sprintAction = InputSystem.actions.FindAction("Sprint");
        _attackAction = InputSystem.actions.FindAction("Attack");
        _ability1Action = InputSystem.actions.FindAction("Ability1");
        _ability2Action = InputSystem.actions.FindAction("Ability2");
        _reviveAction = InputSystem.actions.FindAction("Revive");
    }

    // ── Input subscriptions ───────────────────────────────────────────────────

    private void SubscribeInputEvents( )
    {
        // Move — continuous axis
        _moveAction.performed += OnMovePerformed;
        _moveAction.canceled += OnMoveCanceled;

        // Jump — press only
        _jumpAction.started += OnJumpStarted;

        // Sprint — held
        _sprintAction.started += OnSprintStarted;
        _sprintAction.canceled += OnSprintCanceled;

        // Attack — press only
        _attackAction.started += OnAttackStarted;

        // Abilities — stubs until PlayerAbilities is implemented
        _ability1Action.started += OnAbility1Started;
        _ability2Action.started += OnAbility2Started;

        // Revive — held
        _reviveAction.started += OnReviveStarted;
        _reviveAction.canceled += OnReviveCanceled;
    }

    private void UnsubscribeInputEvents( )
    {
        // Guard — if CacheActions never ran (e.g. object destroyed before OnStartLocalPlayer),
        // these will be null and would throw on unsubscribe
        if (_moveAction == null) return;

        _moveAction.performed -= OnMovePerformed;
        _moveAction.canceled -= OnMoveCanceled;

        _jumpAction.started -= OnJumpStarted;

        _sprintAction.started -= OnSprintStarted;
        _sprintAction.canceled -= OnSprintCanceled;

        _attackAction.started -= OnAttackStarted;

        _ability1Action.started -= OnAbility1Started;
        _ability2Action.started -= OnAbility2Started;

        _reviveAction.started -= OnReviveStarted;
        _reviveAction.canceled -= OnReviveCanceled;
    }

    // ── Per-frame dispatch ────────────────────────────────────────────────────

    private void Update( )
    {
        if (!isLocalPlayer) return;

        _movement.SetMoveInput(_moveInput);

        // Null-safe — PlayerRevive may not exist on the prefab yet
        _revive?.SetReviveInput(_reviveHeld);
    }

    // ── Input handlers ────────────────────────────────────────────────────────
    // Named methods so they can be unsubscribed precisely in OnDestroy.

    private void OnMovePerformed(InputAction.CallbackContext ctx)
        => _moveInput = ctx.ReadValue<Vector2>( );

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
        => _moveInput = Vector2.zero;

    private void OnJumpStarted(InputAction.CallbackContext ctx)
        => _movement?.Jump( );

    private void OnSprintStarted(InputAction.CallbackContext ctx)
        => _movement?.SetSprint(true);

    private void OnSprintCanceled(InputAction.CallbackContext ctx)
        => _movement?.SetSprint(false);

    private void OnAttackStarted(InputAction.CallbackContext ctx)
    {
        // Re-cache if null — handles the post-respawn case where
        // the component ref may have changed after ReplacePlayerForConnection
        if (_attack == null) _attack = GetComponent<PlayerAttack>( );
        _attack?.TryAttack( );
    }

    private void OnReviveStarted(InputAction.CallbackContext ctx)
        => _reviveHeld = true;

    private void OnReviveCanceled(InputAction.CallbackContext ctx)
        => _reviveHeld = false;

    // ── Ability stubs ─────────────────────────────────────────────────────────

    private void OnAbility1Started(InputAction.CallbackContext ctx)
    {
        RiftLogger.Log("Ability 1 pressed (not yet implemented)", this);
        // GetComponent<PlayerAbilities>()?.TryAbility1();
    }

    private void OnAbility2Started(InputAction.CallbackContext ctx)
    {
        RiftLogger.Log("Ability 2 pressed (not yet implemented)", this);
        // GetComponent<PlayerAbilities>()?.TryAbility2();
    }

    // ── Public reads ──────────────────────────────────────────────────────────

    public Vector2 MoveInput => isLocalPlayer ? _moveInput : Vector2.zero;
    public bool ReviveHeld => isLocalPlayer && _reviveHeld;
}