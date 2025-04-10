using UnityEngine;

public class FootstepNoise : MonoBehaviour
{
    public float walkNoiseRadius = 5f;
    public float runNoiseRadius = 10f;
    public float crouchNoiseRadius = 1.5f;
    public float noiseInterval = 0.5f;

    private float noiseTimer;
    private CharacterController controller;
    private FirstPersonController playerController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerController = GetComponent<FirstPersonController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!controller.isGrounded || controller.velocity.magnitude < 0.1f)
        {
            noiseTimer = 0f;
            return;
        }

        float currentInterval = noiseInterval;
        float currentRadius = walkNoiseRadius;

        if (playerController.isCrouching)
        {
            currentRadius = crouchNoiseRadius;
            currentInterval *= 1.5f;
        } 
        else if (playerController.isSprinting)
        {
            currentRadius = runNoiseRadius;
            currentInterval *= 0.75f;
        }

        noiseTimer -= Time.deltaTime;

        if (noiseTimer <= 0f)
        {
            EmitFootstepNoise(currentRadius);
            noiseTimer = currentInterval;
        } 
    }

    void EmitFootstepNoise(float radius)
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius, LayerMask.GetMask("Enemy"));

        foreach (var hit in hitColliders)
        {
            hit.GetComponent<EnemyAI>()?.OnHearNoise(transform.position);
        }

        Debug.DrawLine(transform.position, transform.position + Vector3.up * 2f, Color.yellow, 0.2f);
        DebugExtension.DrawCircle(transform.position, Vector3.up, Color.yellow, radius);
    }

}
