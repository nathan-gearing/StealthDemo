using System.Collections.Generic;
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
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null )
        {
            Debug.LogError("no rigidbody");
        }

        
    }

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
        //Debug.Log("Enemy State: " +  currentState);

    }

    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.Log("no patrolPoibts");
            return;
        }
        
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

        float currentViewDistance = FirstPersonController.Instance.isCrouching ? viewDistance * 0.5f : viewDistance;
        float currentViewAngle = FirstPersonController.Instance.isCrouching ? viewAngle * 0.7f : viewAngle;

        if (Vector3.Distance(transform.position, player.position) < currentViewDistance && angle < currentViewAngle / 2f)
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
        rb.MovePosition(transform.position + dir * speed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
    }

    bool CanSeePlayer()
    {
        if (FirstPersonController.Instance == null) return false;
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        float currentViewDistance = FirstPersonController.Instance.isCrouching ? viewDistance * 0.5f : viewDistance;
        float currentViewAngle = FirstPersonController.Instance.isCrouching ? viewAngle * 0.7f : viewAngle;

        if (Vector3.Distance(transform.position, player.position) < currentViewDistance && angle < currentViewAngle / 2f)
        {
            if (!Physics.Linecast(transform.position, player.position, obstructionMask))
            {
                return true;
            }
        }
        return false;
    }

    public void OnHearNoise(Vector3 sourcePosition)
    {
        if (currentState == EnemyState.Patrolling)
        {
            lastKnownPosition = sourcePosition;
            currentState = EnemyState.Chasing;
            Debug.Log("Enemy heard noise!");
        }
    }


    void OnDrawGizmosSelected()
    {
        
        if (FirstPersonController.Instance == null) return; 
        if (player == null) return;

        float currentViewDistance = FirstPersonController.Instance != null && FirstPersonController.Instance.isCrouching
            ? viewDistance * 0.5f
            : viewDistance;

        float currentViewAngle = FirstPersonController.Instance != null && FirstPersonController.Instance.isCrouching
            ? viewAngle * 0.7f
            : viewAngle;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, currentViewDistance);

        Vector3 forward = transform.forward;

        int segments = 30;
        float angleStep = currentViewAngle / segments;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < segments; i++)
        {
            float angleA = -currentViewAngle / 2 + angleStep * i;
            float angleB = angleA + angleStep;

            Vector3 dirA = Quaternion.Euler(0, angleA, 0) * forward;
            Vector3 dirB = Quaternion.Euler(0, angleB, 0) * forward;

            Vector3 pointA = transform.position + dirA * currentViewDistance;
            Vector3 pointB = transform.position + dirB * currentViewDistance;

            Gizmos.DrawLine(transform.position, pointA);
            Gizmos.DrawLine(pointA, pointB);
        }



        // Line to player if visible
        if (CanSeePlayer())
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}
