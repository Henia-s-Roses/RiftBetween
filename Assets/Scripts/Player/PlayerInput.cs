// PlayerInput.cs

using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : NetworkBehaviour
{
    // ── Sibling component references ──────────────────────────────────────────

    private PlayerMovement _movement;
    private PlayerAttack _attack;

    // ── Cached input state ────────────────────────────────────────────────────

    private Vector2 _moveInput;
    private bool _reviveHeld;

    // ── Cached action references ──────────────────────────────────────────────


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
    }

    public override void OnStartLocalPlayer( )
    {
        CacheActions( );
        SubscribeInputEvents( );
        RiftLogger.Log("Input enabled for local player", this);
    }

    private void OnDestroy( )
    {
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

    }

    private void UnsubscribeInputEvents( )
    {

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
    }

    // ── Input handlers ────────────────────────────────────────────────────────

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