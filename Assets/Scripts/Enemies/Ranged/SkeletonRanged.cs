using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections), typeof(Damageable))]
public class SkeletonRanged : MonoBehaviour
{
    private Transform player;
    private DataPersistenceManager dataPersistenceManager;
    private Rigidbody2D rb;

    public Rigidbody2D Rb => rb;
    
    TouchingDirections touchingDirections;
    Animator animator;
    Damageable damageable;
    private AudioManager audioManager;

    public DetectionZone throwZone;
    public DetectionZone kickZone;
    public DetectionZone cliffDetectionZone;

    public LayerMask playerLayer;
    public float chaseRadius = 8f;
    private bool chasingPlayer = false;

    public enum WalkableDirection { Right, Left };
    private WalkableDirection _walkDirection;

    public float walkAcceleration = 20f;
    public float maxSpeed = 3f;
    public float walkStopRate = 0.2f;

    private Vector2 walkDirectionVector = Vector2.right;
    public bool canMove => animator.GetBool(AnimationStrings.canMove);

    public WalkableDirection walkDirection
    {
        get => _walkDirection;
        set
        {
            if (_walkDirection != value)
            {
                transform.localScale = new Vector2(-transform.localScale.x, transform.localScale.y);
                walkDirectionVector = value == WalkableDirection.Right ? Vector2.right : Vector2.left;
            }
            _walkDirection = value;
        }
    }

    private void FlipDirection()
    {
        walkDirection = walkDirection == WalkableDirection.Right ? WalkableDirection.Left : WalkableDirection.Right;
    }

    public void onCliffDetected()
    {
        if(touchingDirections.IsGrounded)
        {
            FlipDirection();
        }
    }

    public void onHit(int damage, Vector2 knockBackForce)
    {
        rb.linearVelocity = new Vector2(knockBackForce.x, rb.linearVelocity.y + knockBackForce.y);
    }

    public bool hasTarget
    {
        get => animator.GetBool(AnimationStrings.hasTarget);
        private set => animator.SetBool(AnimationStrings.hasTarget, value);
    }

    public bool canKick
    {
        get => animator.GetBool(AnimationStrings.canKick);
        set => animator.SetBool(AnimationStrings.canKick, value);
    }

    public float attackCooldown
    {
        get => animator.GetFloat(AnimationStrings.attackCooldown);
        private set => animator.SetFloat(AnimationStrings.attackCooldown, Mathf.Max(value, 0));
    }

    private void Awake()
    {
        audioManager = AudioManager.instance;
        rb = GetComponent<Rigidbody2D>();
        touchingDirections = GetComponent<TouchingDirections>();
        animator = GetComponent<Animator>();
        damageable = GetComponent<Damageable>();
    }

    private void Start()
    {
        dataPersistenceManager = DataPersistenceManager.instance;

        if (dataPersistenceManager == null)
        {
            Debug.LogWarning($"[Breakable:{gameObject.name}] Start(): DataPersistenceManager.instance is STILL null!");
        }
        else
        {
            Debug.Log($"[Breakable:{gameObject.name}] Start(): Got DataPersistenceManager instance.");
        }
    }

    void Update()
    {
        hasTarget = throwZone.DetectedColliders.Count > 0;
        canKick = kickZone.DetectedColliders.Count > 0;
        attackCooldown -= Time.deltaTime;

        Vector2 boxSize = new Vector2(chaseRadius * 2f, chaseRadius);
        Vector2 boxCenter = (Vector2)transform.position + Vector2.up * (boxSize.y / 2f);
        
        Collider2D playerCollider = Physics2D.OverlapBox(boxCenter, boxSize, 0f, playerLayer);

        if (playerCollider != null)
        {
            player = playerCollider.transform;

            if (player.position.y - transform.position.y <= 1f)
            {
                chasingPlayer = true;
                maxSpeed = 6f;
            }
            else
            {
                chasingPlayer = false;
                maxSpeed = 3f;
            }
        }
        else
        {
            chasingPlayer = false;
            maxSpeed = 3f;
        }
    }

    private void FixedUpdate()
    {
        bool nearCliff = cliffDetectionZone.DetectedColliders.Count == 0;

        if (touchingDirections.IsGrounded && (touchingDirections.IsOnWall || nearCliff))
        {
            FlipDirection();
        }

        if (!damageable.lockVelocity)
        {
            if (chasingPlayer && player != null)
            {
                float directionToPlayer = player.position.x - transform.position.x;

                walkDirection = directionToPlayer > 0 ? WalkableDirection.Right : WalkableDirection.Left;
            }

            if (nearCliff)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                
                return;
            }

            if (canMove && touchingDirections.IsGrounded)
            {
                rb.linearVelocity = new Vector2(Mathf.Clamp(rb.linearVelocity.x + (walkAcceleration * walkDirectionVector.x * Time.fixedDeltaTime), -maxSpeed, maxSpeed), rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = new Vector2(Mathf.Lerp(rb.linearVelocity.x, 0, walkStopRate), rb.linearVelocity.y);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 boxSize = new Vector2(chaseRadius, chaseRadius);
        Vector2 boxCenter = (Vector2)transform.position + Vector2.up * (boxSize.y / 2f - 1f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(boxCenter, boxSize);
    }

    public void PlaySkeletonKickSound()
    {
        if (audioManager != null)
        {
            audioManager.PlaySFX(audioManager.SkeletonKickSound, 1.5f);
        }
    }

    public void PlayRockThrowSound()
    {
        if (audioManager != null)
        {
            audioManager.PlaySFX(audioManager.RockThrowSound, 0.2f);
        }
    }

    public void PlayRockHitSound()
    {
        if (audioManager != null)
        {
            audioManager.PlaySFX(audioManager.RockHitSound, 0.1f);
        }
    }

    public void PlaySkeletonHurtSound()
    {
        if (audioManager != null) 
        {
            audioManager.PlaySFX(audioManager.SkeletonHurtSound, 0.15f);
        }
    }

    public void PlaySkeletonDeathSound()
    {
        if (audioManager != null) 
        {
            audioManager.PlaySFX(audioManager.SkeletonDeathSound, 0.5f);
        }
    }

    public void AddToEliminationCounter()
    {
        if (dataPersistenceManager != null)
        {
            GameData gameData = dataPersistenceManager.GameData;

            if (gameData == null) return;

            gameData.eliminationsTotal++;

            gameData.score += 200;

            dataPersistenceManager.SaveGame();
        }
    }

    #if UNITY_EDITOR || TEST_MODE
    public void SetDataPersistenceManagerForTesting(DataPersistenceManager manager)
    {
        this.dataPersistenceManager = manager;
    }
    #endif

    #if UNITY_EDITOR || TEST_MODE
    public void SetTouchingDirections(TouchingDirections directions)
    {
        this.touchingDirections = directions;
    }
    #endif

    #if UNITY_EDITOR || TEST_MODE
    public void SetRigidbody(Rigidbody2D rb)
    {
        this.rb = rb;
    }
    #endif
}