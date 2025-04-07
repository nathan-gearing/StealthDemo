using Unity.VisualScripting;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Patrolling, Chasing, Returning}
    private EnemyState currentState = EnemyState.Patrolling;

    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float viewDistance = 8f;
    public float viewAngle = 90f;
    public LayerMask playerLayer;
    public LayerMask obstructionMask;
    public Transform player;
    public float loseSightTime = 3f;

    private int currentPointIndex = 0;
    private Vector3 lastKnownPosition;
    private float timeSinceLastSeen = 0f;
    
    // Update is called once per frame
    void Update()
    {
        switch (currentState)
        {
            case EnemyState.Patrolling:
                Patrol();
                LookForPlayer();
                break;
            case EnemyState.Chasing:
                ChasePlayer();
                break;
            case EnemyState.Returning:
                ReturnToPatrol();
                break;

        }
    }

    void Patrol()
    {
        Vector3 target = patrolPoints[currentPointIndex].position;
        MoveTowards(target, patrolSpeed);

        if (Vector3.Distance(transform.position, target) < 0.2f)
        {
            currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
        }
    }

    void LookForPlayer()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        if (Vector3.Distance(transform.position, player.position) < viewDistance && angle < viewAngle / 2f)
        {
            if (!Physics.Linecast(transform.position, player.position, obstructionMask))
            {
                currentState = EnemyState.Chasing;
                lastKnownPosition = player.position;
                timeSinceLastSeen = 0f;
            }
        }
    }

    void ChasePlayer()
    {
        MoveTowards(player.position, chaseSpeed);

        if (!CanSeePlayer())
        {
            timeSinceLastSeen += Time.deltaTime;

            if (timeSinceLastSeen >= loseSightTime)
            {
                currentState = EnemyState.Returning;
            }
        }
        else
        {
            lastKnownPosition = player.position;
            timeSinceLastSeen = 0f;
        }
    }

    void ReturnToPatrol()
    {
        MoveTowards(lastKnownPosition, patrolSpeed);

        if (Vector3.Distance(transform.position, lastKnownPosition) < 0.2f)
        {
            currentState = EnemyState.Patrolling;
        }
    }

    void MoveTowards(Vector3 target, float speed)
    {
        Vector3 dir = (target - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    bool CanSeePlayer()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        if (Vector3.Distance(transform.position, player.position) < viewDistance && angle < viewAngle / 2f)
        {
            if (!Physics.Linecast(transform.position, player.position, obstructionMask))
            {
                return true;
            }
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        // Vision radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        // Vision cone
        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * viewDistance);

        // Line to player if visible
        if (player != null && CanSeePlayer())
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}
