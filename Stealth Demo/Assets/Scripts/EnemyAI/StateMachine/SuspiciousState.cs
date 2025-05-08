using UnityEngine;

public class SuspiciousState : EnemyState
{
    public SuspiciousState(EnemyStateManager enemy) : base(enemy) { }

    public override void Enter()
    {
        enemy.agent.isStopped = true;
        enemy.animator.SetTrigger("isSuspicious");
    }

    public override void Update()
    {
        if (enemy.CanSeePlayer())
        {
            enemy.TransitionToState(new ChaseState(enemy));
            return;
        }

        // Drain suspicion
        enemy.suspicionTimer -= Time.deltaTime * enemy.suspicionDecayRate;
        enemy.SetSuspicion(enemy.suspicionTimer);

        if (enemy.suspicionTimer <= 0)
        {
            enemy.TransitionToState(new PatrolState(enemy));
        }
    }

    public override void Exit()
    {
        //reset any suspicious animation state
    }
}
