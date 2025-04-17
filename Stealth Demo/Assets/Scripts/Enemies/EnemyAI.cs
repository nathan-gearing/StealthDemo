using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Patrolling, Suspicious, Chasing, Returning}
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
    public float suspicionDuration = 4f;
    public float maxSuspicion = 100f;
    public float suspicionIncreaseRate = 40f;
    public float suspicionDecreaseRate = 20f;

    private NavMeshAgent agent;
    private float currentSuspicion = 0f;
    private bool playerInSusRange = false;
    private float suspicionTimer = 0f;
    private int currentPointIndex = 0;
    private Vector3 lastKnownPosition;
    private float timeSinceLastSeen = 0f;
    private Animator animator;


    private void Start()
    {
       agent = GetComponent<NavMeshAgent>();
        if (agent == null )
        {
            Debug.LogError("no nav agent");
        }
        
        animator = GetComponent<Animator>();
        
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentState)
        {
            case EnemyState.Patrolling:
                agent.speed = patrolSpeed;
                Patrol();
                LookForPlayer();
                break;
            case EnemyState.Suspicious:
                agent.speed = patrolSpeed;
                InvestigateSuspicion();
                LookForPlayer();
                break;
            case EnemyState.Chasing:
                agent.speed = chaseSpeed;
                ChasePlayer();
                lastKnownPosition = player.position;
                break;
            case EnemyState.Returning:
                agent.speed = patrolSpeed;
                ReturnToPatrol();
                break;

        }

        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);

        UpdateSuspicionMeter();
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
        target.y = transform.position.y;
        agent.SetDestination(target);

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
                //lastKnownPosition = player.position;
                timeSinceLastSeen = 0f;
            }
        }
    }

    void ChasePlayer()
    {
        agent.SetDestination(player.position);

        if (!CanSeePlayer())
        {
            timeSinceLastSeen += Time.deltaTime;

            if (timeSinceLastSeen >= loseSightTime)
            {
                currentState = EnemyState.Suspicious;
                suspicionTimer = suspicionDuration;
                agent.SetDestination(lastKnownPosition);
            }
        }
        else
        {
            //lastKnownPosition = player.position;
            timeSinceLastSeen = 0f;
        }
    }

    void InvestigateSuspicion()
    {
        agent.SetDestination(lastKnownPosition);

        suspicionTimer -= Time.deltaTime;
        if (Vector3.Distance(transform.position, lastKnownPosition) < 0.5f || suspicionTimer <= 0f)
        {
            currentState = EnemyState.Returning;
        }
    }

    void ReturnToPatrol()
    {
        Vector3 target = patrolPoints[currentPointIndex].position;
        target.y = transform.position.y;
        agent.SetDestination(target);

        if (Vector3.Distance(transform.position, target) < 0.2f)
        {
            currentState = EnemyState.Patrolling;
        }
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
                lastKnownPosition = player.position;
                return true;
            }
        }
        return false;
    }

    public void OnHearNoise(Vector3 sourcePosition)
    {
        if (currentState == EnemyState.Patrolling || currentState == EnemyState.Returning)
        {
            lastKnownPosition = sourcePosition;
            suspicionTimer = suspicionDuration;
            currentState = EnemyState.Suspicious;
            currentSuspicion = Mathf.Clamp(currentSuspicion + 25f, 0, maxSuspicion);
            Debug.Log("Enemy heard noise!");
        }
    }

    void UpdateSuspicionMeter()
    {
        if (currentState == EnemyState.Patrolling || currentState == EnemyState.Suspicious)
        {
            if (CanSeePlayer())
            {
                playerInSusRange = true;
                currentSuspicion += suspicionIncreaseRate * Time.deltaTime;
                

                if (currentSuspicion >= maxSuspicion)
                {
                    currentSuspicion = maxSuspicion;
                    currentState = EnemyState.Chasing;
                    timeSinceLastSeen = 0f;
                }
                else if (currentState != EnemyState.Suspicious)
                {
                    currentState = EnemyState.Suspicious;
                }
            }
            else
            {
                playerInSusRange = false;
                if (currentSuspicion > 0)
                {
                    currentSuspicion -= suspicionDecreaseRate * Time.deltaTime;
                    if (currentSuspicion <= 0)
                    {
                        currentSuspicion = 0;
                        if (currentState == EnemyState.Suspicious && !playerInSusRange)
                        {
                            currentState = EnemyState.Returning;
                        }
                    }
                }
            }
        }
    }

    Transform GetClosestPatrolPoint()
    {
        Transform closest = patrolPoints[0];
        float closestDist = Vector3.Distance(transform.position, closest.position);

        foreach (Transform point in patrolPoints)
        {
            float dist = Vector3.Distance(transform.position, point.position);
            if (dist < closestDist)
            {
                closest = point;
                closestDist = dist;
            }
        }

        return closest;
    }

    private void OnGUI()
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        if (screenPos.z > 0)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(screenPos.x, Screen.height - screenPos.y, 100, 20), $"Suspicion: {currentSuspicion:F0}");
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
