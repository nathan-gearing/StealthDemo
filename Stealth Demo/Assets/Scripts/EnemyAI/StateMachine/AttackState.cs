using UnityEngine;

public class AttackState : EnemyState
{
    public AttackState(EnemyStateManager enemy) : base(enemy) { }

    public override void Enter()
    {
        enemy.agent.isStopped = true;
        enemy.animator.SetTrigger("Attack");
        enemy.ResetAttackTimer();
    }

    public override void Update()
    {
        //Back to chase
        if (!enemy.CanSeePlayer() || !enemy.IsInAttackRange())
        {
            enemy.TransitionToState(new ChaseState(enemy));
            return;
        }

        // Cooldown between attacks
        if (enemy.CanAttack())
        {
            enemy.animator.SetTrigger("Attack");
            enemy.ResetAttackTimer();
           
        }
    }

    public override void Exit()
    {
        // No  exit logic needed
    }
}
