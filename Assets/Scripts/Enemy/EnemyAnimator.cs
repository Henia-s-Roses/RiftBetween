// EnemyAnimator.cs


using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{


    private static readonly int P_IsMoving = Animator.StringToHash("isMoving");
    private static readonly int P_HitTrigger = Animator.StringToHash("hitTrigger");
    private static readonly int P_AttackTrigger = Animator.StringToHash("attackTrigger");
    private static readonly int P_DieTrigger = Animator.StringToHash("dieTrigger");


    private Animator _animator;
    private bool _isDead = false;


    private void Awake( )
    {
        _animator = GetComponent<Animator>( );
    }


    public void SetMoving(bool moving)
    {
        if (_isDead) return;
        SafeSetBool(P_IsMoving, moving);
    }

    public void TriggerAttack( )
    {
        if (_isDead) return;
        SafeSetTrigger(P_AttackTrigger);
    }

    public void TriggerHit( )
    {
        if (_isDead) return;
        SafeSetTrigger(P_HitTrigger);
    }

    public void TriggerDeath( )
    {
        if (_isDead) return;
        _isDead = true;
        SafeSetTrigger(P_DieTrigger);
    }


    private void SafeSetBool(int hash, bool val)
    {
            _animator.SetBool(hash, val);
    }

    private void SafeSetTrigger(int hash)
    {
            _animator.SetTrigger(hash);
    }
}