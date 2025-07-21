using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Assets.Interfaces;
using Unity.VisualScripting;
//using System.Collections.Generic;
//using TMPro;
//using System;

public class PlayerMovementDashing : MonoBehaviour , IDamageable
{
    // This movement controller could be called FRAPs "frenetic random activity periods" or Zoomies
    /* TODO'S
     * - Potential to connect to an audio controller
     * - Respawn Button & Death Screen
    */
    [Header("Import")]
    public Transform orientation;
    public Transform playerCamOrientation;
    public AudioManagerBrackeys audioManager;

    [Header("Ground Check")]
    public LayerMask whatIsGround;
    public float groundCheckLength;
    public float groundCheckWidth;
    bool grounded;

    [Header("Debugging")]
    public MovementState state;
    public float moveSpeed; //These values are changed consistantly
    public float rigidbodySpeed;
    public float speedChangeFactor; //can be made public to show in inspector for debugging.
    public bool useGravity;
    public bool decending;

    [Header("Walking")]
    private readonly float WALK_SPEED = 12f;
    private readonly float WALK_SPEED_CHANGE = 20f;
    private readonly float GROUND_DRAG = 4f;

    [Header("Jumping")]
    public bool enableDashJump = true;
    public bool enableAirDashJump = false;
    public bool enableDoubleJump = false;
    public float normalJumpHeight = 3f;
    public float superJumpHeight = 10f;
    public float superJumpChargeTime = 0.4f;
    public float doubleJumpStamina = 1f;
    public float superJumpStamina = 1f;
    private readonly float CUSTOM_GRAVITY = -18f;
    private readonly float JUMP_COOLDOWN = 0.1f;
    private readonly float AIR_HANDLING = 0.2f;
    private readonly float AIR_SPEED_CHANGE = 5f;
    private bool mayDoubleJump;
    private bool doubleJumpLocked;
    private bool readyToJump;
    private float superJumpJuice;
    private float jumpForce;
    private float usedJumpHeight;
    private float usedDashDuration;
    [HideInInspector] public bool jumping;

    [Header("Crouching")]
    public float slideStamina = 0f;
    public float slideHandlingAdjust = 0.1f;
    public float maxSlideHandling = 1f;
    public float minSlideHandling = 0.01f;
    public float maxHandlingSpeed = 6;
    public float superJumpSpeedThreshold = 6;
    private readonly float CROUCH_SPEED = 5f;
    private readonly float CROUCH_Y_SCALE = 0.5f;
    private readonly float CROUCH_DOWN_FORCE = 10f; // it's a high number to be able to change direction Mid-air
    private float startYScale;
    private bool crouching;
    private float adjustedCrouchSpeed;
    private float crouchSpeedChange;
    private bool slidingDown;

    [Header("Dashing")]
    public float dashStamina = 1;
    private readonly float DASH_END_SPEED_CHANGE = 200f;
    private readonly float DASH_SPEED = 30f;
    private readonly float DASH_SPEED_CHANGE = 10f;
    private readonly float DASH_FORCE = 30f;
    private readonly float DASH_DURATION = 0.18f;
    private readonly float superDashDuration = 0.3f;
    private readonly float DASH_END_DURATION = 0.02f;
    private readonly float DASH_CD = 0.21f;
    private float dashCdTimer;
    private Vector3 delayedForceToApply; // Nessesary for Dash to function properly.
    [HideInInspector] public bool dashEnd;
    [HideInInspector] public bool dashing;
    private bool superDashing;
    private bool maySuperDash = false;
    private Vector3 usedDashDirection;

    [Header("Stamina")]
    public bool enableAirbourneRegen;
    public float staminaRegen = 1.5f;
    public float maxStamina = 3f;
    private float stamina;

    [Header("Health")]
    public float maxHealth = 1000f;
    private float health;
    public bool alive;
    
    private readonly float PLAYER_HEIGHT = 2f;
   
    private readonly float MAX_SLOPE_ANGLE = 45f;

    private RaycastHit slopeHit;
    private RaycastHit groundHit;
    private bool exitingSlope;

    private bool drag;
    float horizontalInput;
    float verticalInput;
    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;
    private bool keepMomentum = false;
    private bool groundedLastFrame;
    private bool takeOff;
    private bool touchDown;

    Vector3 moveDirection;

    // The code below is nessesary for NewInput Controller
    public ProtagControls protagControls;
    private InputAction move;
    private InputAction dash;
    private InputAction jump;
    private InputAction crouch;

    Rigidbody rb; // <-RIGIDBODY

    public enum MovementState
    {
        walking,
        crouching,
        dashing,
        dashend,
        air,
        dead
    }

    private void Awake()
    {
        protagControls = new ProtagControls(); // <-Nessesary for NewInput Controller
    }

    private void OnEnable() // all controls that need to be enabled for new movement system.
    {
        move = protagControls.Player.Move; // <-Nessesary for NewInput Controller
        move.Enable(); // <-Nessesary for NewInput Controller

        dash = protagControls.Player.Dash;
        dash.Enable();
        dash.performed += Dash;

        jump = protagControls.Player.Jump;
        jump.Enable();
        jump.performed += Jump;

        crouch = protagControls.Player.Crouch;
        crouch.Enable();
        crouch.performed += Crouch;
        crouch.started += Crouch;
        crouch.canceled += Crouch;
    }

    private void OnDisable() // all controls that need to be disabled for new movement system.
    {
        move.Disable(); // <-Nessesary for NewInput Controller
        dash.Disable();
        jump.Disable();
        crouch.Disable();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        stamina = maxStamina;
        health = maxHealth;
        alive = true;
        mayDoubleJump = enableDoubleJump;
        rb.freezeRotation = true;
        dashEnd = false;
        dashing = false;
        groundedLastFrame = true;
        readyToJump = true;
        startYScale = transform.localScale.y;
        Physics.gravity = new Vector3(0f, CUSTOM_GRAVITY, 0f);
    }

    private void Update()
    {
        groundedLastFrame = grounded;
        GroundCheck();
        decending = GoingDown();
        rigidbodySpeed = rb.velocity.magnitude;
        StartAndEndSlideDown();
        CrouchSpeedAdjuster();
        SlideAudioHandler();
        
        if (alive)
        {
            TotalProtagControl();
        }
        else if (!alive)
        {
            Dying();
        }
    }

    private void TotalProtagControl()
    {
        MyInput();
        DashUpdates();
        SpeedControl();
        StateHandler();
        StaminaRegenerator();
        SuperJumpCharger();
        MomentumHandler();
        DragHandler();
    }

    private void GroundCheck()
    {
        grounded = Physics.SphereCast(transform.position, groundCheckWidth , Vector3.down, out groundHit, PLAYER_HEIGHT * 0.5f + groundCheckLength, whatIsGround);
        if (grounded && !mayDoubleJump && !doubleJumpLocked && enableDoubleJump)
        {
            mayDoubleJump = true;
            //Debug.Log("Double Jump Active");
        }
        //Debug.Log("Grounded: " + grounded);
        TakeOffAndTouchDownChecks();
    }

    private void TakeOffAndTouchDownChecks()
    {
        if (!grounded && groundedLastFrame)
        {
            Debug.Log("Takeoff True");
            takeOff =  true;
        }
        else
        {
            takeOff = false;
        }

        if (grounded && !groundedLastFrame)
        {
            Debug.Log("TouchDown True");
            touchDown = true;
        }
        else
        {
            touchDown = false;
        }
    }

    private void SuperJumpCharger()
    {
        if (grounded && crouching && rigidbodySpeed <= superJumpSpeedThreshold)
        {
            superJumpJuice += Time.deltaTime;
            superJumpJuice = Mathf.Clamp(superJumpJuice, 0f, superJumpChargeTime);
        }
        else 
        {
            superJumpJuice = 0f;
        }
    }

    private void LockOutDoubleJump()
    {
        mayDoubleJump = false;
        doubleJumpLocked = true;
        Invoke(nameof(UnlockDoubleJump), 0.2f);
    }

    private void UnlockDoubleJump()
    {
        doubleJumpLocked = false;
    }

    private bool Juiced()
    {
        if (superJumpJuice == superJumpChargeTime)
        {
            return true;
        }
        else
        { 
            return false;
        }
    } //"Juice" is aquired by standing still and crouching. like a cat ready to pounce

    private void StaminaRegenerator()
    {
        if (stamina < maxStamina && RegenWhileAirbourne() && !Sliding() && dashCdTimer == 0f)
        {
            stamina += staminaRegen * Time.deltaTime;
        }

        if (stamina > maxStamina)
        {
            MaxOutStamina();
        }
    }

    private bool RegenWhileAirbourne()
    {
        if (enableAirbourneRegen)
        {
            return true;
        }
        else
        {
            return grounded;
        }
    }

    private void MaxOutStamina()
    {
        stamina = maxStamina;
    }

    private void StaminaConsume(float ammount)
    {
        stamina -= ammount;
    }

    private bool StaminaCheck(float ammount)
    {
        if (stamina >= ammount)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public float GetPlayerStamina()
    {
        return stamina;
    }

    public float GetPlayerMaxStamina()
    {
        return maxStamina;
    }

    private void MaxOutHealth()
    {
        health = maxHealth;
    }

    public void Damage(float ammount)
    {
        health -= ammount;
        health = Mathf.Clamp(health, 0, maxHealth);
        if (health == 0) 
        {  
            alive = false;
            Dying();
        }
    }

    public void Dying()
    {
        if (!alive)
        {
            protagControls.Disable();
        }
        else if (alive)
        {
            protagControls.Enable();
        }
    }

    private void HealthGain(float ammount)
    {
        health += ammount;
        Mathf.Clamp(health, 0, maxHealth);
    }

    public float GetPlayerHealth() 
    { 
        return health; 
    }

    public float GetPlayerMaxHealth()
    {
        return maxHealth;
    }

    private void DragHandler()
    {
        if (drag == true)
            rb.drag = GROUND_DRAG;
        else
            rb.drag = 0f;
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = move.ReadValue<Vector2>().x;
        verticalInput = move.ReadValue<Vector2>().y;
    }

    private bool StickNeutral()
    {
        if (horizontalInput < 0.1f && horizontalInput > -0.1f && verticalInput < 0.1f && verticalInput > -0.1f)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool Sliding()
    {
        if (rb.velocity.magnitude > WALK_SPEED && state == MovementState.crouching)
        {
            return true;
        }
        else 
        { 
            return false;
        }
    }

    private float SpeedScaledFromZeroToOne()
    {
        //NOTICE: Any speed below maxHandlingSpeed (6f) is considered a Zero, and DASH_SPEED + 5 (35F) is a 1
        return Mathf.Clamp01((rigidbodySpeed - maxHandlingSpeed) / (DASH_SPEED + 5 - maxHandlingSpeed));
    }

    private void StateHandler()
    {
        switch (state)
        {
            case MovementState.dead:
                desiredMoveSpeed = 0;
                speedChangeFactor = 0;
                useGravity = true;
                drag = true;
                //TODO: Add Dying to code.
                //if (alive) state = MovementState.walking;
                break;
            case MovementState.dashend:
                desiredMoveSpeed = WALK_SPEED;
                speedChangeFactor = DASH_END_SPEED_CHANGE;
                drag = true;
                useGravity = false;
                if (!alive) state = MovementState.dead;
                break;
            case MovementState.walking:
                desiredMoveSpeed = WALK_SPEED;
                speedChangeFactor = WALK_SPEED_CHANGE;
                useGravity = false;
                drag = true;
                if (!alive) state = MovementState.dead;
                if (crouching) state = MovementState.crouching;
                if (!grounded) state = MovementState.air;
                break;
            case MovementState.crouching:
                desiredMoveSpeed = adjustedCrouchSpeed;
                drag = false;
                useGravity = OnSlope();
                SlideAudioHandler();
                transform.localScale = new Vector3(transform.localScale.x, CROUCH_Y_SCALE, transform.localScale.z);
                if (!alive) state = MovementState.dead;
                if (!grounded) state = MovementState.air;
                break;
            case MovementState.dashing:
                desiredMoveSpeed = DASH_SPEED;
                speedChangeFactor = DASH_SPEED_CHANGE;
                drag = false;
                useGravity = false;
                if (!alive) state = MovementState.dead;
                if (crouching) state = MovementState.crouching;
                if (dashEnd) state = MovementState.dashend;
                break;
            case MovementState.air:
                desiredMoveSpeed = WALK_SPEED;
                speedChangeFactor = AIR_SPEED_CHANGE;
                drag = false;
                useGravity = true;
                if (!alive) state = MovementState.dead;
                if (grounded)
                {
                    if (crouching) state = MovementState.crouching;
                    else if (dashEnd) state = MovementState.dashend;
                    else state = MovementState.walking;
                }
                break;
        }
    }

    private void Crouch(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (StickNeutral())
            {
                ResetVelocity();
            }

            if (dashing && grounded)
            {
                if (StaminaCheck(slideStamina))
                {
                    StaminaConsume(slideStamina);
                }
                else
                {
                    moveSpeed = WALK_SPEED;
                    return;
                }
            }
            ResetYVel();
            rb.AddForce(Vector3.down * CROUCH_DOWN_FORCE, ForceMode.Impulse);
            audioManager.Play("Player Slide");
            crouching = true;
        }

        if (context.performed && grounded)
        {
                state = MovementState.crouching;
        }

        if (context.canceled)
        {
            crouching = false;
            state = MovementState.walking;
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            audioManager.Stop("Player Slide");
        }
    }

    private void SlideAudioHandler()
    {
        if (crouching && touchDown)
        {
            audioManager.Play("Player Slide");
        }

        if (crouching && takeOff)
        {
            audioManager.Stop("Player Slide");
            Debug.Log("SlideAudioHandler stopped slide sound");
        }

        audioManager.SetVolume("Player Slide", Mathf.Pow(SpeedScaledFromZeroToOne(), 2.5f));
        audioManager.SetPitch("Player Slide", Mathf.Lerp(0.7f, 1.1f, SpeedScaledFromZeroToOne()));
    }

    private void Jump(InputAction.CallbackContext context)
    {
        if (!readyToJump) return;
        if (dashing || !grounded)
        {
            if (!StaminaCheck(doubleJumpStamina)) return;
            if (dashing && !grounded && !AirDashJumpChecks()) // Air Dash Jump False
            {
                return;
            }
            else if (dashing && DashJumpChecks()) // Dash Jump True (Air or Ground)
            {
                StaminaConsume(doubleJumpStamina);
                LockOutDoubleJump();
            }
            else if (!grounded && DoubleJumpChecks()) // Double Jump True
            {
                StaminaConsume(doubleJumpStamina);
                LockOutDoubleJump();
            }
            else  // Anything else
            {
                return;
            }
        }
        if (Juiced()) // Super Jump
        {
            if (StaminaCheck(superJumpStamina))
            {
                StaminaConsume(superJumpStamina);
                usedJumpHeight = superJumpHeight;
                LockOutDoubleJump();
            }
            else
            {
                return;
            }
        }
        else
        {
            usedJumpHeight = normalJumpHeight;
        }
        state = MovementState.air;
        readyToJump = false;
        exitingSlope = true;
        jumping = true;
        jumpForce = Mathf.Sqrt(-2f * usedJumpHeight * Physics.gravity.y); // Typically about 19 for a super Jump
        Invoke(nameof(ResetJump), JUMP_COOLDOWN);
        ResetYVel();
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        audioManager.Play("Player Jump");
    }

    private void Dash(InputAction.CallbackContext context)
    {
        if (dashCdTimer > 0 || !StaminaCheck(dashStamina)) return;

        //Transform forwardT;
        //forwardT = orientation;

        if (Juiced() && maySuperDash)
        {
            usedDashDuration = superDashDuration;
            LockOutDoubleJump();
            Vector3 superDashDirection = GetCameraDirection();
            //Debug.Log("Dash Direction: " + superDashDirection.x);
            usedDashDirection = superDashDirection;
            superDashing = true;
            Invoke(nameof(ResetDash), superDashDuration + DASH_END_DURATION);
            //Debug.Log("SUPERDASH!!!");
        }
        else if (grounded && !StickNeutral())
        {
            DashDirector(GetSlopeMoveDirection());
        }
        else
        {
            DashDirector(GetDirection(orientation));
        }

        StaminaConsume(dashStamina);
        dashCdTimer = DASH_CD;
        state = MovementState.dashing;

        dashing = true;

        //cam.DoFov(dashFov);
        Vector3 forceToApply = usedDashDirection * DASH_FORCE;

        delayedForceToApply = forceToApply;
        //Invoke(nameof(DelayedDashForce), 0.025f);
        state = MovementState.dashing;
        DelayedDashForce();
    }

    private void ResetVelocity()
    {
        rb.velocity = Vector3.zero;
    }

    private void CrouchSpeedAdjuster()
    {
        if ( StickNeutral())
        {
            adjustedCrouchSpeed = 0;
        }
        else
        {
            adjustedCrouchSpeed = CROUCH_SPEED;
        }
    }

    private float CrouchHandlingAdjustedForSpeed()
    {
        return Mathf.Lerp(maxSlideHandling, minSlideHandling, Mathf.Pow(SpeedScaledFromZeroToOne(), slideHandlingAdjust));
    }

    private void StartAndEndSlideDown()
    {
        if (!slidingDown && crouching && OnSlope() && GoingDown())
        {
            slidingDown = true;
        }
        else
        {
            slidingDown = false;
        }
    }

    private void MomentumHandler()
    {
        if (moveSpeed >= desiredMoveSpeed && state != MovementState.dashend)
        {
            keepMomentum = true;
        }
        else
        {
            keepMomentum = false;
        }

        bool desiredMoveSpeedHasChanged = desiredMoveSpeed != lastDesiredMoveSpeed;
        if (desiredMoveSpeedHasChanged)
        {
            if (keepMomentum)
            {
                StopAllCoroutines();
                StartCoroutine(SmoothlyLerpMoveSpeed());
            }
            else
            {
                StopAllCoroutines();
                moveSpeed = desiredMoveSpeed;
            }
        }

        lastDesiredMoveSpeed = desiredMoveSpeed;
    }

    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        // smoothly lerp movementSpeed to desired value
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        while (time < difference)
        {
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);

            float usedSpeedChangeFactor = speedChangeFactor;

            if (!slidingDown)
            {
                time += Time.deltaTime * usedSpeedChangeFactor;
            }
            else
            {
                time = 0;
                startValue += Time.deltaTime * usedSpeedChangeFactor * 2f;
                startValue = Mathf.Clamp(startValue, 0f, DASH_SPEED + 5);
            }

                yield return null;
        }

        moveSpeed = desiredMoveSpeed;
        keepMomentum = false;
    }

    private void MovePlayer()
    {
        if (state == MovementState.dashing || state == MovementState.dashend) return;

        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // on slope
        if (grounded && !exitingSlope)
        {
            if (crouching)
                rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 10f * CrouchHandlingAdjustedForSpeed(), ForceMode.Force);
            else 
                rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * AIR_HANDLING, ForceMode.Force);
        }

        // turn gravity off while on slope
        rb.useGravity = useGravity;
    }

    private void SpeedControl()
    {

            // limiting speed on slope
        if (OnSlope() && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;
        }

        // limiting speed on ground or in air
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            // limit velocity if needed
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }

        }

    }

    public bool GoingDown()
    {
        if (rb.velocity.y < 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void ResetYVel()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
    }

    private bool DoubleJumpChecks()
    {
        return (mayDoubleJump && enableDoubleJump);
    }

    private bool DashJumpChecks()
    {
        return (enableDashJump);
    }

    private bool AirDashJumpChecks()
    {
        return (DoubleJumpChecks() && DashJumpChecks() && enableAirDashJump);
    }

    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
        jumping = false;
    }

    private bool OnSlope()
    {
        if (Physics.SphereCast(transform.position, 0.5f , Vector3.down, out slopeHit, PLAYER_HEIGHT * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < MAX_SLOPE_ANGLE && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
            return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

    private Vector3 GetDirection(Transform forwardT)
    {
        Vector3 direction = new Vector3();

        direction = forwardT.forward * verticalInput + forwardT.right * horizontalInput;

        if (StickNeutral())
            direction = forwardT.forward;

        return direction.normalized;
    }

    public static float Round(float value, int digits)
    {
        float mult = Mathf.Pow(10.0f, (float)digits);
        return Mathf.Round(value * mult) / mult;
    }

    private void DashUpdates()
    {
        if (dashCdTimer > 0)
            dashCdTimer -= Time.deltaTime;
            dashCdTimer = Mathf.Clamp(dashCdTimer, 0f, maxStamina);

        if (jumping == true || state == MovementState.crouching)
            CancelInvoke(nameof(DashEnd));
    }

    private void DashDirector(Vector3 direction)
    {
        usedDashDuration = DASH_DURATION;
        usedDashDirection = direction;
        Invoke(nameof(DashEnd), DASH_DURATION);
        Invoke(nameof(ResetDash), DASH_DURATION + DASH_END_DURATION);
    }

    private Vector3 GetCameraDirection()
    {
        Vector3 direction = new Vector3();
        Transform camera = playerCamOrientation;
        direction = camera.forward;
        return direction;
    }

    private Vector3 VerticalLimiter(Vector3 vector) // This function limits vertical height to prevent player from jumping higher than super Jump with some abilities. TODO: Add more flexibility
    {
        Vector3 returnedVector;
        returnedVector = vector;
        returnedVector.y = Mathf.Clamp(vector.y, -300f, 18.9f); 
        return returnedVector;
    }

    private void DelayedDashForce()
    {
        rb.velocity = Vector3.zero;

        rb.AddForce(VerticalLimiter(delayedForceToApply), ForceMode.Impulse);
        audioManager.Play("Player Dash");
    }

    private void DashEnd()
    {
        //Debug.Log("Dashend");
        state = MovementState.dashend;
        dashing = false;
        dashEnd = true;
    }

    private void ResetDash()
    {
        dashing = false;
        superDashing = false;
        dashEnd = false;
        rb.useGravity = true;

        if (!grounded)
        {
            state = MovementState.air;
        }
        else if (state == MovementState.crouching)
        {
            return;
        }
        else
        {
            state = MovementState.walking;
        }
    }

}
