using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class EnemyAI : MonoBehaviour
{
    //public enum EnemyState { Patrolling, Suspicious, Chasing, Returning}
    //private EnemyState currentState = EnemyState.Patrolling;


    [Header("References")]
    public Transform player;
    private Rigidbody playerRb;
    private Animator animator;
    private NavMeshAgent agent;
    public Slider suspicionSlider;

    [Header("Patrolling")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    public float waitDuration = 2f;
    public float lookAngle = 30f;
    public float lookSpeed = 2f;
    private int currentPointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private Quaternion initialRotation;
    private bool isReturnRotation = false;
    private float rotationReturnSpeed = 180f;

    [Header("Vision")]
    public float viewDistance = 8f;
    public float viewAngle = 90f;
    public LayerMask playerLayer;
    public LayerMask obstructionMask;

    [Header("Suspicion")]
    public float suspicionDuration = 4f;
    public float maxSuspicion = 100f;
    public float suspicionIncreaseRate = 40f;
    public float suspicionDecreaseRate = 20f;
    private float currentSuspicion = 0f;
    private float suspicionTimer = 0f;
    private bool playerInSusRange = false;

    [Header("Chasing")]
    public float chaseSpeed = 4f;
    public float loseSightTime = 3f;
    public float predictionFactor = 0.4f;
    private Vector3 lastKnownPosition;
    private float timeSinceLastSeen = 0f;
    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity;

    [Header("Combat")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
    public float attackWindupTimer = 0.5f;
    private float lastAttackTime = -Mathf.Infinity;
    private bool isAttacking = false;

    [Header("Noise Reaction")]
    private float currentNoisePriority = 0f;

    [Header("Noise Investigation")]
    public float noiseLookDuration = 2f;
    private Vector3 noiseDirection;
    private float noiseLookTimer = 0f;
    private bool isInvestigatingNoise = false;


    private void Start()
    {
       agent = GetComponent<NavMeshAgent>();
        if (agent == null )
        {
            Debug.LogError("no nav agent");
        }
        
        animator = GetComponent<Animator>();

        playerRb = player.GetComponent<Rigidbody>();

        

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

        if (!isAttacking && Time.time - lastAttackTime >= attackCooldown && InAttackRange())
        {
            StartCoroutine(PerformAttack());
        }

        playerVelocity = (player.position - lastPlayerPosition) / Time.deltaTime;
        lastPlayerPosition = player.position;

        

        UpdateSuspicionMeter();
        UpdateSuspicionMeterColor();
        //Debug.Log("Enemy State: " +  currentState);

    }

    bool MoveTowardsTarget(Vector3 target, float stopDistance)
    {
        target.y = transform.position.y;
        float distance = Vector3.Distance(transform.position, target);

        if (distance > stopDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(target);
            return false;
        }
        else
        {
            agent.isStopped = true;
            agent.ResetPath();
            return true;
        }
    }

    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.Log("no patrolPoints");
            return;
        }
        
        if (isWaiting)
        {
            waitTimer += Time.deltaTime;

            if (!isReturnRotation)
            {
                float angleOffset = Mathf.Sin(waitTimer * lookSpeed) * lookAngle;
                Quaternion lookRotation = Quaternion.Euler(0f, initialRotation.eulerAngles.y + angleOffset, 0f);
                transform.rotation = lookRotation;
            }
            

            if (waitTimer >= waitDuration)
            {
                agent.isStopped = true;
                isReturnRotation = true;
            }

            if (isReturnRotation)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, initialRotation, rotationReturnSpeed * Time.deltaTime);

                if (Quaternion.Angle(transform.rotation, initialRotation) < 1f)
                {
                    isReturnRotation = false;
                    isWaiting = false;
                    waitTimer = 0;

                    agent.isStopped = false;
                    currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
                }
            }
            return;
        }
        
        Vector3 target = patrolPoints[currentPointIndex].position;
        if (MoveTowardsTarget(target, 0.2f))
        {
            isWaiting = true;
            waitTimer = 0;
            initialRotation = transform.rotation;
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
                if (currentState != EnemyState.Chasing) 
                {
                    currentState = EnemyState.Chasing; 
                    timeSinceLastSeen = 0f;
                }
            }
        }
    }

    void ChasePlayer()
    {
        if (!MoveTowardsTarget(player.position, attackRange))
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer > attackRange)
            {
                agent.isStopped = false;
                float predictionTime = 0.5f;
                Vector3 predictedPosition = player.position + playerVelocity * predictionTime; 
                agent.SetDestination(predictedPosition);
            }
            else
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

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
       if (isInvestigatingNoise)
        {
           
            Vector3 lookTarget = transform.position + noiseDirection;
            Vector3 lookDirection = (lookTarget - transform.position).normalized;
            lookDirection.y = 0f; // Prevent tilting
            Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, rotationReturnSpeed * Time.deltaTime);

            noiseLookTimer -= Time.deltaTime;
            if (noiseLookTimer <= 0f)
            {
                isInvestigatingNoise = false;
                currentNoisePriority = 0f;
                currentState = EnemyState.Returning;
                agent.isStopped = false;
            }

            return;
        }

        
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
        if (MoveTowardsTarget(target, 0.2f))
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

    public void OnHearNoise(Vector3 sourcePosition, float volume)
    {
        float distance = Vector3.Distance(transform.position, sourcePosition);
        float priority = volume / distance;

        if (currentState == EnemyState.Chasing || priority <= currentNoisePriority)
            return;

        currentNoisePriority = priority;
        noiseDirection = (sourcePosition - transform.position).normalized;
        noiseLookTimer = noiseLookDuration;
        isInvestigatingNoise = true;
        currentState = EnemyState.Suspicious;
        agent.isStopped = true;

    }

    void UpdateSuspicionMeter()
    {
        

        if (CanSeePlayer())
        {
            playerInSusRange = true;
            currentSuspicion += suspicionIncreaseRate * Time.deltaTime;

            if (currentSuspicion >= maxSuspicion || timeSinceLastSeen > 1f) 
            {
                currentSuspicion = maxSuspicion;
                if (currentState != EnemyState.Chasing) 
                {
                    currentState = EnemyState.Chasing;
                }
            }
            else if (currentState != EnemyState.Suspicious)
            {
                currentState = EnemyState.Suspicious;
            }
        }
        else
        {
            playerInSusRange = false;
            if (currentSuspicion > 0f)
            {
                currentSuspicion -= suspicionDecreaseRate * Time.deltaTime;
                if (currentSuspicion <= 0f && currentState == EnemyState.Suspicious)
                {
                    currentState = EnemyState.Returning;
                }
            }
        }

        if (suspicionSlider != null)
        {
            suspicionSlider.value = currentSuspicion;
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

    bool InAttackRange()
    {
        return Vector3.Distance(transform.position, player.position) <= attackRange;
    }

    
    IEnumerator PerformAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        agent.isStopped = true;

       
        Vector3 predictedPos = player.position;

        if (playerRb != null)
        {
            predictedPos += playerRb.linearVelocity * predictionFactor;
        }

        Vector3 lookDirection = (predictedPos - transform.position).normalized;
        lookDirection.y = 0f;

        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(attackWindupTimer);

        lastAttackTime = Time.time;
        isAttacking = false;
        agent.isStopped = false;
    }

    void UpdateSuspicionMeterColor()
    {
        if (suspicionSlider != null)
        {
            if (currentState == EnemyState.Suspicious)
            {
                suspicionSlider.fillRect.GetComponentInChildren<Image>().color = Color.yellow;
            }
            else if (currentState == EnemyState.Chasing)
            {
                suspicionSlider.fillRect.GetComponentInChildren<Image>().color = Color.red;
            }
            else
            {
                suspicionSlider.fillRect.GetComponentInChildren<Image>().color = Color.green;
            }
        }
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
