using System.Buffers.Text;
using UnityEngine;

public class ChaseState : EnemyState
{
    public ChaseState(EnemyStateManager enemy) : base(enemy) { }

    public override void Enter()
    {
        enemy.agent.isStopped = false;
        enemy.agent.speed = enemy.walkSpeed;
        enemy.animator.SetBool("isWalking", true);
        enemy.animator.SetBool("isRunning", false);
    }

    public override void Update()
    {
        if (!enemy.CanSeePlayer())
        {
            enemy.TransitionToState(new SuspiciousState(enemy));
            return;
        }

        enemy.suspicionTimer += Time.deltaTime;
        enemy.SetSuspicion(enemy.suspicionTimer);

        float suspicionPercent = enemy.suspicionTimer / enemy.suspicionTime;

        if (suspicionPercent >= 0.5f)
        {
            enemy.agent.speed = enemy.runSpeed;
            enemy.animator.SetBool("isRunning", true);
        }
        else
        {
            enemy.agent.speed = enemy.walkSpeed;
            enemy.animator.SetBool("isRunning", false);
        }

        enemy.agent.SetDestination(enemy.player.position);

        if (enemy.suspicionTimer >= enemy.suspicionTime)
        {
            enemy.TransitionToState(new AttackState(enemy));
        }
    }

    public override void Exit()
    {
        enemy.agent.speed = enemy.walkSpeed;
        enemy.animator.SetBool("isWalking", false);
        enemy.animator.SetBool("isRunning", false);
    }
}
