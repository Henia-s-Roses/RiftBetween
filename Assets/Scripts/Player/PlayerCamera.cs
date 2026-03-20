// PlayerCamera.cs
// Attach this to the Camera GameObject inside each player prefab.
// On start, checks if this player is the local player.
// If yes — enables the camera and follows the player.
// If no  — disables the camera entirely so only one camera is active per machine.

using Mirror;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Follow Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);
    [SerializeField] private float smoothSpeed = 8f;

    [Header("Bounds (optional)")]
    [Tooltip("Clamp the camera within these world-space X bounds. Set both to 0 to disable.")]
    [SerializeField] private float minX = 0f;
    [SerializeField] private float maxX = 0f;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private Camera _cam;
    private bool _follow = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake( )
    {
        _cam = GetComponent<Camera>( );
    }

    public override void OnStartClient( )
    {
        if (isLocalPlayer)
        {
            // This is our player — enable camera and start following
            _cam.enabled = true;
            _follow = true;
            RiftLogger.Log("Camera enabled for local player", this);
        } else
        {
            // This is the remote player's object on our machine — kill their camera
            // so we don't have two active cameras rendering at once
            _cam.enabled = false;
            _follow = false;
            RiftLogger.Log("Remote player camera disabled", this);
        }
    }

    // ── Follow ────────────────────────────────────────────────────────────────

    private void LateUpdate( )
    {
        // LateUpdate runs after all player movement — smoother follow
        if (!_follow) return;

        Vector3 target = transform.parent.position + offset;

        // Clamp X within stage bounds if set
        if (minX != 0f || maxX != 0f)
            target.x = Mathf.Clamp(target.x, minX, maxX);

        transform.position = Vector3.Lerp(
            transform.position,
            target,
            smoothSpeed * Time.deltaTime
        );
    }
}