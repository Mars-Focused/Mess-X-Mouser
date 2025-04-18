using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
//using System.Collections.Generic;
//using TMPro;
//using System;

public class PlayerMovementDashing : MonoBehaviour
{
    [Header("Import")]
    public Transform orientation;

    [Header("Debugging")]
    public MovementState state;
    public float moveSpeed; //These values are changed consistantly
    public float speedChangeFactor; //can be made public to show in inspector for debugging.

    [Header("Walking")]
    private readonly float WALK_SPEED = 12f;
    private readonly float WALK_SPEED_CHANGE = 20f;
    private readonly float GROUND_DRAG = 4f;

    [Header("Jumping")]
    public float jumpHeight = 2.5f;
    public readonly float CUSTOM_GRAVITY = -18f;
    private readonly float JUMP_COOLDOWN = 0.05f;
    private readonly float AIR_MULTIPLIER = 0.3f;
    private readonly float AIR_SPEED_CHANGE = 5f;
    private bool readyToJump;
    private float jumpForce;
    [HideInInspector] public bool jumping;

    [Header("Crouching")]
    private readonly float CROUCH_SPEED = 5f;
    private readonly float CROUCH_Y_SCALE = 0.5f;
    private readonly float CROUCH_MULTIPLIER = 0.1f;
    private readonly float CROUCH_DOWN_FORCE = 10f; // it's a high number to be able to change direction Mid-air
    private float startYScale;
    private bool crouching;

    [Header("Dashing")]
    private readonly float DASH_END_SPEED_CHANGE = 200f;
    private readonly float DASH_SPEED = 30f;
    private readonly float DASH_SPEED_CHANGE = 10f;
    private readonly float DASH_FORCE = 30f;
    private readonly float DASH_DURATION = 0.18f;
    private readonly float DASH_END_DURATION = 0.02f;
    private readonly float DASH_STAMINA = 1;
    private readonly float DASH_CD = 0.21f;
    private float dashCdTimer;
    private Vector3 delayedForceToApply; // Nessesary for Dash to function properly.
    [HideInInspector] public bool dashEnd;
    [HideInInspector] public bool dashing;

    [Header("Stamina")]
    public float staminaRegen = 2f;
    public float maxStamina = 3f;
    private float stamina;
    
    private readonly float PLAYER_HEIGHT = 2f;
    
    [Header("Ground Check")]
    public LayerMask whatIsGround;
    bool grounded;
   
    private readonly float MAX_SLOPE_ANGLE = 45f;

    private RaycastHit slopeHit;
    private bool exitingSlope;

    private bool drag;
    float horizontalInput;
    float verticalInput;
    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;
    private bool keepMomentum = false;
    private bool useGravity;

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
        air
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
        rb.freezeRotation = true;
        dashEnd = false;
        dashing = false;
        readyToJump = true;
        startYScale = transform.localScale.y;
        Physics.gravity = new Vector3(0f, CUSTOM_GRAVITY, 0f);
    }

    private void Update()
    {
        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, PLAYER_HEIGHT * 0.5f + 0.2f, whatIsGround);

        MyInput();
        DashUpdates();
        SpeedControl();
        StateHandler();
        StaminaRegenerator();
        MomentumHandler();
        DragHandler();
    }
    private void StaminaRegenerator()
    {
        if (stamina < maxStamina && grounded && !crouching && dashCdTimer == 0f)
        {
            stamina += staminaRegen * Time.deltaTime;
        }

        if (stamina > maxStamina)
        {
            MaxOutStamina();
        }
    }

    private void MaxOutStamina()
    {
        stamina = maxStamina;
    }

    private void DragHandler()
    {
        if (drag == true)
            rb.drag = GROUND_DRAG;
        else
            rb.drag = 0;
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
    private void Crouch(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            crouching = true;
            ResetYVel();
            rb.AddForce(Vector3.down * CROUCH_DOWN_FORCE, ForceMode.Impulse);
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
        }
    }

    private void StateHandler()
    {
        switch (state)
        {
            case MovementState.walking:
                desiredMoveSpeed = WALK_SPEED;
                speedChangeFactor = WALK_SPEED_CHANGE;
                useGravity = false;
                drag = true;
                if (!grounded) state = MovementState.air;
                if (crouching) state = MovementState.crouching;
                break;
            case MovementState.crouching:
                desiredMoveSpeed = CROUCH_SPEED;
                drag = false;
                useGravity = StickNeutral();
                transform.localScale = new Vector3(transform.localScale.x, CROUCH_Y_SCALE, transform.localScale.z);
                if (!grounded) state = MovementState.air;
                break;
            case MovementState.dashing:
                desiredMoveSpeed = DASH_SPEED;
                speedChangeFactor = DASH_SPEED_CHANGE;
                drag = false;
                useGravity = false;
                break;
            case MovementState.dashend:
                desiredMoveSpeed = WALK_SPEED;
                speedChangeFactor = DASH_END_SPEED_CHANGE;
                drag = true;
                useGravity = false;
                break;
            case MovementState.air:
                desiredMoveSpeed = WALK_SPEED;
                speedChangeFactor = AIR_SPEED_CHANGE;
                drag = false;
                useGravity = true;
                if (grounded)
                {
                    if (crouching) state = MovementState.crouching;
                    else if (dashEnd) state = MovementState.dashend;
                    else state = MovementState.walking;
                }
                break;
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

            float boostFactor = speedChangeFactor;

            time += Time.deltaTime * boostFactor;

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
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);

            if (rb.velocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }

        // DOTO: CONSOLIDATE THIS CODE
        // while crouching
        else if (crouching)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * CROUCH_MULTIPLIER, ForceMode.Force);

        // on ground
        else if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        // in air
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * AIR_MULTIPLIER, ForceMode.Force);

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

    private void ResetYVel()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
    }

    private void Jump(InputAction.CallbackContext context)
    {
        if (!grounded || !readyToJump) return;
        state = MovementState.air;
        readyToJump = false;
        exitingSlope = true;
        jumping = true;
        jumpForce = Mathf.Sqrt(-2f * jumpHeight * Physics.gravity.y);
        Invoke(nameof(ResetJump), JUMP_COOLDOWN);
        ResetYVel();
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
        jumping = false;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, PLAYER_HEIGHT * 0.5f + 0.3f))
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

    private void Dash(InputAction.CallbackContext context)
    {
        if (dashCdTimer > 0 || !StaminaCheck(DASH_STAMINA)) return;
        else
            StaminaConsume(DASH_STAMINA);
        dashCdTimer = DASH_CD;
        state = MovementState.dashing;

        dashing = true;

        //cam.DoFov(dashFov);

        Transform forwardT;

        forwardT = orientation; /// where you're facing (no up or down)

        Vector3 direction = GetDirection(forwardT);

        Vector3 forceToApply = direction * DASH_FORCE;



        rb.useGravity = false;

        delayedForceToApply = forceToApply;
        Invoke(nameof(DelayedDashForce), 0.025f);

        Invoke(nameof(DashEnd), DASH_DURATION);
        Invoke(nameof(ResetDash), DASH_DURATION + DASH_END_DURATION);
    }

    private void DelayedDashForce()
    {
        rb.velocity = Vector3.zero;

        rb.AddForce(delayedForceToApply, ForceMode.Impulse);
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

    private Vector3 GetDirection(Transform forwardT)
    {
        Vector3 direction = new Vector3();

        direction = forwardT.forward * verticalInput + forwardT.right * horizontalInput;

        if (verticalInput == 0 && horizontalInput == 0)
            direction = forwardT.forward;

        return direction.normalized;
    }
}
