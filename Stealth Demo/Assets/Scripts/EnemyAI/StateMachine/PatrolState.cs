using UnityEngine;

public class PatrolState : EnemyState
{
    private float waitTimer = 0f;
    private float waitDuration = 2f;
    private bool isWaiting = false;
    private bool isReturningToForward = false;

    //Rotation
    private float rotationSpeed = 50f;
    private float maxAngle = 30f;
    private float rotationDirection = 1f;
    private float currentAngle = 0f;
    private Quaternion originalRotation;
    
    public PatrolState(EnemyStateManager enemy) : base(enemy) { }

    

    public override void Enter()
    {
        enemy.agent.isStopped = false;
        enemy.animator.SetBool("isWalking", true);

        if (enemy.patrolPoints.Length > 0)
        {
            enemy.agent.SetDestination(enemy.patrolPoints[enemy.currentPatrolIndex].position);
        }

        waitTimer = 0f;
        isWaiting = false;
        currentAngle = 0f;
        rotationDirection = 1f;
    }

    public override void Update()
    {
        if (enemy.CanSeePlayer())
        {
            enemy.TransitionToState(new ChaseState(enemy));
            return;
        }

        if (!enemy.agent.pathPending && enemy.agent.remainingDistance <= enemy.agent.stoppingDistance)
        {
            if (!isWaiting)
            {
                isWaiting = true;
                waitTimer = 0f;
                enemy.agent.isStopped = true;
                enemy.animator.SetBool("isWalking", false);
                originalRotation = enemy.transform.rotation;
            }

            if (isWaiting)
            {
                RotateSideToSide();

                waitTimer += Time.deltaTime;
                if (waitTimer >= waitDuration)
                {
                    isWaiting = false;
                    isReturningToForward = true;
                }
            }
            if (isReturningToForward)
            {
                ReturnToForward();
            }
        }
    }

    private void RotateSideToSide()
    {
        float angleStep = rotationSpeed * Time.deltaTime * rotationDirection;
        enemy.transform.Rotate(0f, angleStep, 0f);
        currentAngle += angleStep;

        if (Mathf.Abs(currentAngle) >= maxAngle)
        {
            rotationDirection *= -1f; // switch direction
        }
    }

    private void ReturnToForward()
    {
        enemy.transform.rotation = Quaternion.RotateTowards(enemy.transform.rotation, originalRotation, rotationSpeed * Time.deltaTime);

        if (Quaternion.Angle(enemy.transform.rotation, originalRotation) < 1f)
        {
            enemy.PatrolToNextPoint();
            enemy.agent.isStopped = false;
            enemy.animator.SetBool("isWalking", true);
            ResetLookState();
        }
    }

    private void ResetLookState()
    {
        waitTimer = 0f;
        isWaiting = false;
        isReturningToForward = false;
        currentAngle = 0f;
        rotationDirection = 1f;
    }
    

    public override void Exit()
    {
        enemy.animator.SetBool("isWalking", false);
    }

}
