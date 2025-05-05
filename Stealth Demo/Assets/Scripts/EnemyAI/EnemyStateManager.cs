using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyStateManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;
    public Slider suspicionSlider;
    public LayerMask playerMask, obstacleMask;
    public Transform eye;

    [Header("Stats")]
    public float viewRadius = 10f;
    [Range(0, 360)] public float viewAngle = 120f;
    public float suspicionTime = 5f;
    public float suspicionDecayRate = 1f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
    public float suspicion = 0f;

    [Header("Patrol")]
    public Transform[] patrolPoints;

    [HideInInspector] public float suspicionTimer = 0f;
    private float attackTimer = 0f;
    private int currentPatrolIndex = 0;

    private EnemyState currentState;

    private void Start()
    {
        TransitionToState(new PatrolState(this));
    }

    private void Update()
    {
        currentState?.Update();
        attackTimer += Time.deltaTime;
    }

    public void TransitionToState(EnemyState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public bool CanSeePlayer()
    {
        Vector3 dirToPlayer = (player.position - eye.position).normalized;
        if (Vector3.Distance(eye.position, player.position) < viewRadius && Vector3.Angle(eye.forward, dirToPlayer) < viewAngle / 2)
        {
            if (!Physics.Raycast(eye.position, dirToPlayer, Vector3.Distance(eye.position, player.position), obstacleMask))
            {
                return true;
            }
        }
        return false;
    }

    public bool IsInAttackRange()
    {
        return Vector3.Distance(transform.position, player.position) <= attackRange;
    }

    public bool CanAttack()
    {
        return attackTimer >= attackCooldown;
    }

    public void ResetAttackTimer()
    {
        attackTimer = 0;
    }

    public void PatrolToNextPoint()
    {
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    public void SetSuspicion(float value)
    {
        suspicion = Mathf.Clamp(value, 0, suspicionTime);
        suspicionSlider.value = suspicion / suspicionTime;
    }
}