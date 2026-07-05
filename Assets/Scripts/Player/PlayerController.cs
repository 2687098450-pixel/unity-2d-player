using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float runSpeed = 7f;
    [SerializeField] float animatorWalkSpeed = 1.5f;
    [SerializeField] float animatorRunSpeed = 5f;

    [Header("双击跑步")]
    [SerializeField] float doubleTapWindow = 0.5f;

    [Header("跳跃")]
    [SerializeField] float jumpForce = 12f;
    [SerializeField] float jumpGraceTime = 0.12f;
    [SerializeField] bool allowDoubleJump = true;

    [Header("地面检测")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.12f;
    [SerializeField] LayerMask groundLayer;

    [Header("攻击出生点")]
    [SerializeField] Transform attackPoint;

    Rigidbody2D rb;
    Animator animator;
    SpriteRenderer spriteRenderer;
    Collider2D bodyCollider;
    ContactFilter2D groundContactFilter;
    readonly Collider2D[] groundHits = new Collider2D[8];

    float lastTapTime = -1f;
    KeyCode lastTapKey;
    bool isRunning;
    bool isGrounded;
    bool hasDoubleJumped;
    bool jHeld;
    bool uHeld;
    bool iHeld;
    bool facingRight;
    float jumpGraceTimer;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    static readonly int JumpHash = Animator.StringToHash("Jump");
    static readonly int SAttackHash = Animator.StringToHash("SAttack");
    static readonly int MAttackHash = Animator.StringToHash("MAttack");
    static readonly int JAttackHash = Animator.StringToHash("JAttack");
    static readonly int BAttackHash = Animator.StringToHash("BAttack");

    static readonly int SAttackStateHash = Animator.StringToHash("Player_S_Attack");
    static readonly int MAttackStateHash = Animator.StringToHash("Player_M_Attack");
    static readonly int BAttackStateHash = Animator.StringToHash("Player_B_Attack");
    static readonly int JAttackStateHash = Animator.StringToHash("Player_J_Aattck");
    static readonly int IdleStateHash = Animator.StringToHash("Player_Idle");
    static readonly int JumpStateHash = Animator.StringToHash("Player_Jump");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();

        if (groundCheck == null)
        {
            var check = new GameObject("GroundCheck");
            check.transform.SetParent(transform);
            groundCheck = check.transform;
        }

        if (bodyCollider is BoxCollider2D box)
        {
            float feetY = box.offset.y - box.size.y * 0.5f + groundCheckRadius * 0.5f;
            groundCheck.localPosition = new Vector3(box.offset.x, feetY, 0f);
        }

        if (groundLayer.value == 0)
            groundLayer = LayerMask.GetMask("Default");

        groundContactFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = groundLayer,
            useTriggers = false,
        };

        if (attackPoint != null)
        {
            var p = attackPoint.localPosition;
            attackPoint.localPosition = new Vector3(Mathf.Abs(p.x), p.y, p.z);
        }

        if (spriteRenderer != null)
            facingRight = spriteRenderer.flipX;
    }

    void Update()
    {
        bool touchingGround = IsTouchingGround();
        jHeld = Input.GetKey(KeyCode.J);
        uHeld = Input.GetKey(KeyCode.U);
        iHeld = Input.GetKey(KeyCode.I);

        if (Input.GetKeyDown(KeyCode.K))
        {
            if (touchingGround && jumpGraceTimer <= 0f)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                jumpGraceTimer = jumpGraceTime;
                if (animator != null)
                {
                    if (IsInGroundAttackState())
                        CancelAttackTo(JumpStateHash);
                    else
                        animator.SetTrigger(JumpHash);
                }
            }
            else if (allowDoubleJump && !hasDoubleJumped && !touchingGround)
            {
                hasDoubleJumped = true;
                rb.velocity = new Vector2(rb.velocity.x, jumpForce * 0.9f);
                jumpGraceTimer = jumpGraceTime;
                if (animator != null)
                {
                    if (IsInAirAttackState())
                        CancelAttackTo(JumpStateHash);
                    else
                        animator.SetTrigger(JumpHash);
                }
            }
        }

        if (jumpGraceTimer > 0f)
            jumpGraceTimer -= Time.deltaTime;

        isGrounded = touchingGround
            && rb.velocity.y <= 0.05f
            && jumpGraceTimer <= 0f;

        if (isGrounded)
            hasDoubleJumped = false;

        if (Input.GetKeyDown(KeyCode.J) && animator != null){
            if(isGrounded)
                animator.SetTrigger(SAttackHash);
            else if(!isGrounded)
                animator.SetTrigger(JAttackHash);
        }

        if (Input.GetKeyDown(KeyCode.U) && animator != null)
            animator.SetTrigger(MAttackHash);

        if (Input.GetKeyDown(KeyCode.I) && animator != null)
            animator.SetTrigger(BAttackHash);

        if (animator != null)
        {
            animator.SetBool(IsGroundedHash, isGrounded);
            animator.SetFloat(VerticalSpeedHash, rb.velocity.y);
        }

        HandleDoubleTap();

        float move = Input.GetAxisRaw("Horizontal");

        if (move == 0f)
            isRunning = false;

        float speed = isRunning ? runSpeed : walkSpeed;
        rb.velocity = new Vector2(move * speed, rb.velocity.y);

        if (animator != null)
        {
            float animSpeed = 0f;
            if (move != 0f)
                animSpeed = isRunning ? animatorRunSpeed : animatorWalkSpeed;
            animator.SetFloat(SpeedHash, animSpeed);
        }

        if (move != 0f)
        {
            bool newFacingRight = move > 0f;
            bool currentFacingRight = spriteRenderer != null ? spriteRenderer.flipX : facingRight;

            if (newFacingRight != currentFacingRight && IsInAttackState())
                CancelAttackTo(IdleStateHash);

            facingRight = newFacingRight;

            if (spriteRenderer != null)
                spriteRenderer.flipX = facingRight;

            if (attackPoint != null)
            {
                var p = attackPoint.localPosition;
                float absX = Mathf.Abs(p.x);
                attackPoint.localPosition = new Vector3(
                    facingRight ? absX : -absX,
                    p.y,
                    p.z);
            }
        }
    }

    bool IsTouchingGround()
    {
        if (groundCheck == null)
            return false;

        int count = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundContactFilter,
            groundHits);

        for (int i = 0; i < count; i++)
        {
            if (groundHits[i] == null || groundHits[i] == bodyCollider)
                continue;
            if (bodyCollider != null && groundHits[i].transform.IsChildOf(transform))
                continue;
            return true;
        }

        return false;
    }

    void HandleDoubleTap()
    {
        if (Input.GetKeyDown(KeyCode.A))
            RegisterTap(KeyCode.A);
        else if (Input.GetKeyDown(KeyCode.D))
            RegisterTap(KeyCode.D);
    }

    void RegisterTap(KeyCode key)
    {
        if (key == lastTapKey && Time.time - lastTapTime < doubleTapWindow)
            isRunning = true;

        lastTapTime = Time.time;
        lastTapKey = key;
    }

    bool IsAnimatorInState(int stateHash)
    {
        if (animator == null)
            return false;

        if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash == stateHash)
            return true;

        if (animator.IsInTransition(0)
            && animator.GetNextAnimatorStateInfo(0).shortNameHash == stateHash)
            return true;

        return false;
    }

    bool IsInGroundAttackState()
    {
        return IsAnimatorInState(SAttackStateHash)
            || IsAnimatorInState(MAttackStateHash)
            || IsAnimatorInState(BAttackStateHash);
    }

    bool IsInAirAttackState() => IsAnimatorInState(JAttackStateHash);

    bool IsInAttackState() => IsInGroundAttackState() || IsInAirAttackState();

    void CancelAttackTo(int stateHash)
    {
        if (animator == null)
            return;

        animator.ResetTrigger(SAttackHash);
        animator.ResetTrigger(MAttackHash);
        animator.ResetTrigger(BAttackHash);
        animator.ResetTrigger(JAttackHash);
        animator.Play(stateHash, 0, 0f);
    }

    public void OnSAttackFinished()
    {
        if (!jHeld || !isGrounded || animator == null)
            return;
        animator.ResetTrigger(SAttackHash);
        animator.SetTrigger(SAttackHash);
    }
    public void OnMAttackFinished()
    {
        if (!uHeld || !isGrounded || animator == null)
            return;
        animator.ResetTrigger(MAttackHash);
        animator.SetTrigger(MAttackHash);
    }
    public void OnBAttackFinished()
    {
        if (!iHeld || !isGrounded || animator == null)
            return;
        animator.ResetTrigger(BAttackHash);
        animator.SetTrigger(BAttackHash);
    }
}
