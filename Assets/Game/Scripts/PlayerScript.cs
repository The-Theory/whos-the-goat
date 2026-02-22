using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class PlayerScript : MonoBehaviour {
    #region Variables
    //##############################################################################
    private enum InputType { WASD, ArrowKeys }
    [Header("Movement options")]
    [SerializeField] private InputType inputType = InputType.WASD;  // Decides key layout

    // Movement variables
    private float moveSpeed = 10f;
    private float airMoveSpeed = 7f;
    private float jumpForce = 16f;
    private float dashSpeed = 18f;
    
    // Knockback variables
    private Vector2 knockbackVelocity = Vector2.zero;
    private float knockbackTimer = 0f;
    private float knockbackDuration = 0.4f;
    
    // Dash variables
    private float doubleTapTime = 0.3f;
    private float lastTapTimeLeft = -1f;
    private float lastTapTimeRight = -1f;   
    private float dashCooldown = 0f;
    private float dashCooldownTime = 0.5f;
    private bool isDashing = false;

    // Components
    private Rigidbody2D rb;
    private BoxCollider2D col;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private LayerMask platformLayer, playerLayer;
    private Vector2 startPosition;

    // Animations
    private Dictionary<string, RuntimeAnimatorController> animations = new Dictionary<string, RuntimeAnimatorController>();

    // Keys
    KeyCode upKey, leftKey, rightKey, dashKey;

    // Multipliers and boosts
    public float speedMultiplier       { get; set; } = 1f;
    public float jumpMultiplier        { get; set; } = 1f;
    public float knockbackInfluence    { get; set; } = 1f;
    public float scaleMultiplier       { get; set; } = 1f;
    public int doubleJumps             { get; set; } = 0;
    private int currentDoubleJumps = 0;

    private Dictionary<string, Coroutine> activePowerupExpiries = new Dictionary<string, Coroutine>();
    
    // State variables
    private bool isGrounded;
    private float inputX = 0f;
    #endregion

    #region Start logic
    //##############################################################################
    void Start() {
        // Get components
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        var spritesheet = transform.GetChild(0);
        animator = spritesheet.GetComponent<Animator>();
        spriteRenderer = spritesheet.GetComponent<SpriteRenderer>();
        
        // Set start position
        startPosition = transform.position;

        // Set keys based on input type
        if (inputType == InputType.WASD) {
            upKey = KeyCode.W;
            leftKey = KeyCode.A;
            rightKey = KeyCode.D;
            dashKey = KeyCode.S;
        } 
        else if (inputType == InputType.ArrowKeys) {
            upKey = KeyCode.UpArrow;
            leftKey = KeyCode.LeftArrow;
            rightKey = KeyCode.RightArrow;
            dashKey = KeyCode.DownArrow;
        }

        // Layers and collision
        Physics2D.queriesStartInColliders = false;
        platformLayer = LayerMask.GetMask("Platform");
        playerLayer = LayerMask.GetMask("Player");

        // Add each animator controller per color
        foreach (string color in new string[] { "Black", "Brown", "Gold", "Red", "White" })
            animations.Add(color, Resources.Load<RuntimeAnimatorController>($"Game/Animations/{color}GoatAnimator.controller"));
    }
    #endregion

    #region Update logic
    //##############################################################################
    void Update() {
        // Scale multiplier
        transform.localScale = new Vector3(scaleMultiplier, scaleMultiplier, 1f);
        rb.mass = scaleMultiplier * 10f;

        // Reset player if below -11.5f
        if (transform.position.y < -11.5f) transform.position = startPosition;
        
        // Update dash, movement, and animation
        DashInput();
        MovementInput();
        UpdateAnimation();
    }
    #endregion

    #region Dash logic
    //##############################################################################
    void ResetDash() => isDashing = false;
    void ResetDashPose() { rb.gravityScale = 3f; animator.SetBool("ShowDashPose", false); }
    void Dash(int direction) {
        // Animate dash
        animator.SetTrigger("Dash");
        animator.SetBool("ShowDashPose", true);

        // Disable gravity and reset y velocity
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        isDashing = true;
        knockbackTimer = 0f; // Cancel any knockback
        rb.linearVelocity = new Vector2(direction * dashSpeed * speedMultiplier, rb.linearVelocity.y);
        dashCooldown = dashCooldownTime;
        Invoke(nameof(ResetDash), 0.25f);
        Invoke(nameof(ResetDashPose), 0.25f);
    }
    void DashInput() {
        knockbackTimer -= Time.deltaTime;
        dashCooldown -= Time.deltaTime;

        // Double tap dash
        if (Input.GetKeyDown(leftKey)) {
            if (Time.time - lastTapTimeLeft < doubleTapTime && dashCooldown <= 0)
                Dash(-1);
            lastTapTimeLeft = Time.time;
        }
        if (Input.GetKeyDown(rightKey)) {
            if (Time.time - lastTapTimeRight < doubleTapTime && dashCooldown <= 0)
                Dash(1);
            lastTapTimeRight = Time.time;
        }

        // Dash using dashKey
        if (Input.GetKeyDown(dashKey) && dashCooldown <= 0)
            Dash(spriteRenderer.flipX ? -1 : 1);
    }
    #endregion

    #region Collision logic
    //##############################################################################
    void OnCollisionStay2D(Collision2D collision) => HandlePlayerCollision(collision);
    void OnCollisionEnter2D(Collision2D collision) => HandlePlayerCollision(collision);
    void HandlePlayerCollision(Collision2D collision) {
        if (!collision.gameObject.CompareTag("Player")) return;

        PlayerScript otherPlayer = collision.gameObject.GetComponent<PlayerScript>();
        
        // Calculate push direction
        Vector2 pushDir = (transform.position - collision.transform.position).normalized;
        
        // If both players are dashing
        if (isDashing && otherPlayer.isDashing) {
            ApplyKnockback(new Vector2(pushDir.x * 12f, 4f)); // Knock this player back
            otherPlayer.ApplyKnockback(new Vector2(-pushDir.x * 12f * speedMultiplier, 4f)); // Knock other player back
            
            isDashing = false;
            otherPlayer.isDashing = false;
            return;
        }

        // If this player is dashing
        else if (isDashing) {
            Vector2 otherPushDir = (collision.transform.position - transform.position).normalized;
            otherPlayer.ApplyKnockback(new Vector2(otherPushDir.x * 18f * speedMultiplier, 5f));
            
            isDashing = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.3f, rb.linearVelocity.y); // Slow down 
        }
    }
    #endregion

    #region Knockback logic
    //##############################################################################
    public void ApplyKnockback(Vector2 velocity) {
        animator.SetTrigger("Hit");

        knockbackVelocity = velocity * knockbackInfluence;
        knockbackTimer = knockbackDuration;
        rb.linearVelocity = knockbackVelocity; 
    }
    #endregion

    #region Powerup expiry
    //##############################################################################
    public void RunPowerupExpiry(string powerupTypeId, float duration, Action<PlayerScript> onExpired) {
        if (activePowerupExpiries.TryGetValue(powerupTypeId, out var existing)) {
            StopCoroutine(existing);
            activePowerupExpiries.Remove(powerupTypeId);
        }
        var routine = StartCoroutine(PowerupExpiryRoutine(powerupTypeId, duration, onExpired));
        activePowerupExpiries[powerupTypeId] = routine;
    }
    private IEnumerator PowerupExpiryRoutine(string powerupTypeId, float duration, Action<PlayerScript> onExpired) {
        yield return new WaitForSeconds(duration);
        activePowerupExpiries.Remove(powerupTypeId);
        onExpired(this);
    }
    #endregion

    #region Animation logic
    //##############################################################################
    void SetAnimationController(string color) => animator.runtimeAnimatorController = animations[color];
    void UpdateAnimation() {
        float inputSpeed = 0f;
        if (Input.GetKey(leftKey) || Input.GetKey(rightKey)) {
            inputSpeed = Mathf.Abs(rb.linearVelocity.x) / moveSpeed;
        }
        
        animator.SetFloat("SpeedRatio", inputSpeed);
        animator.SetBool("IsGrounded", isGrounded);
        
        // Flip sprite based on velocity
        if (rb.linearVelocity.x > 0.1f)
            spriteRenderer.flipX = false;
        else if (rb.linearVelocity.x < -0.1f)
            spriteRenderer.flipX = true;
    }
    #endregion

    #region Movement logic
    //##############################################################################
    void MovementInput() {
        /////////////// Grounded check ///////////////
        isGrounded = Physics2D.BoxCast(
            col.bounds.center,                              // Origin
            new Vector2(col.bounds.size.x, 0.1f),           // Size
            0f,                                             // Angle
            Vector2.down,                                   // Direction
            col.bounds.extents.y + 0.05f,                   // Distance
            platformLayer | playerLayer                     // Layer mask
        );

        // Reset double jumps
        if (isGrounded) currentDoubleJumps = 0;

        /////////////// Jump input ///////////////
        if (Input.GetKey(upKey) && (isGrounded || currentDoubleJumps < doubleJumps) && knockbackTimer <= 0) {
            if (!isGrounded) currentDoubleJumps++;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * jumpMultiplier);
        }
        
        /////////////// Movement input ///////////////
        inputX = 0f;
        if (Input.GetKey(leftKey))  inputX -= 1;
        if (Input.GetKey(rightKey)) inputX += 1;
        
        // Apply movement
        if (knockbackTimer <= 0 && !isDashing) {
            float currentMoveSpeed = isGrounded ? moveSpeed : airMoveSpeed;
            currentMoveSpeed *= speedMultiplier;
            rb.linearVelocity = new Vector2(inputX * currentMoveSpeed, rb.linearVelocity.y);
        }
    }
    #endregion
}
