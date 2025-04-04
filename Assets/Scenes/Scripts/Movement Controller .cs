using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
//using System.Collections.Generic;
//using TMPro;
//using System;
public class PlayerMovementDashing : MonoBehaviour
{
    [Header("Walking")]
    public float walkSpeed = 15f;
    public float walkSpeedChange = 5f;
    public float groundDrag = 4f;
    private float moveSpeed; //This value is changed consistantly

    [Header("Jumping")]
    public float jumpForce = 10f; //TODO: change value to jump height and do some Maths
    public float jumpCooldown = 0.2f;
    public float airMultiplier = 0.3f;
    bool readyToJump;
    [HideInInspector] public bool jumping;

    [Header("Crouching")]
    public float crouchSpeed = 5f;
    public float crouchYScale = 0.5f;
    public float crouchMultiplier = 0.1f;
    public float crouchDownForce = 10f; // it's a high number to be able to change direction Mid-air
    private float startYScale;
    private bool crouching;

    [Header("Dashing")]
    public float dashEndSpeedChange = 200f;
    public float dashSpeed = 30f;
    public float dashSpeedChange = 10f;
    public float dashForce = 30f;
    public float dashDuration = 0.18f;
    public float dashEndDuration = 0.02f;
    public float dashStamina = 1;
    public float dashCd = 0.21f;
    private float dashCdTimer;
    private Vector3 delayedForceToApply; // Nessesary for Dash to function.
    [HideInInspector] public bool dashEnd;
    [HideInInspector] public bool dashing;

    [Header("Stamina")]
    public float maxStamina = 2f;
    private float stamina;
    public float staminaRegen = 0.5f;

    [Header("Ground Check")]
    public float playerHeight = 2f;
    public LayerMask whatIsGround;
    bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle = 45f;
    public Transform orientation;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    private bool drag;
    float horizontalInput;
    float verticalInput;
    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;
    private bool keepMomentum = false;
    private float speedChangeFactor;
    private bool useGravity;

    Vector3 moveDirection;

    // The code below is nessesary for NewInput Controller
    public ProtagControls protagControls;
    private InputAction move; 
    private InputAction dash; 
    private InputAction jump; 
    private InputAction crouch;

    Rigidbody rb; // <-RIGIDBODY

    public MovementState state;
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
    }

    private void Update()
    {
        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        MyInput();
        DashUpdates();
        SpeedControl();
        StateHandler();
        StaminaRegenerator();
        MomentumHandler();
        DragHandler();
    }
    private void  StaminaRegenerator()
    {
        if (stamina < maxStamina && grounded)
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
            rb.drag = groundDrag;
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

    private void FixedUpdate()
    {
        MovePlayer(); 
    }

    private void MyInput()
    {
        horizontalInput = move.ReadValue<Vector2>().x;
        verticalInput = move.ReadValue<Vector2>().y;
    }

    private void Crouch(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            crouching = true;
            ResetYVel();
            rb.AddForce(Vector3.down * crouchDownForce, ForceMode.Impulse);
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
                desiredMoveSpeed = walkSpeed;
                speedChangeFactor = walkSpeedChange;
                useGravity = false;
                drag = true;
                if (!grounded) state = MovementState.air;
                if (crouching) state = MovementState.crouching;
                break;
            case MovementState.crouching:
                desiredMoveSpeed = crouchSpeed;
                drag = false;
                useGravity = true;
                transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
                break;
            case MovementState.dashing:
                desiredMoveSpeed = dashSpeed;
                speedChangeFactor = dashSpeedChange;
                drag = false;
                useGravity = false;
                break;
            case MovementState.dashend:
                desiredMoveSpeed = walkSpeed;
                speedChangeFactor = dashEndSpeedChange;
                drag = true;
                useGravity = false;
                break;
            case MovementState.air:
                desiredMoveSpeed = walkSpeed;
                drag = false;
                useGravity = true;
                if (grounded)
                {
                    if (crouching) state = MovementState.crouching;
                    else state = MovementState.walking;
                }
                break;
        }
    }

    private void MomentumHandler()
    {
        if (lastDesiredMoveSpeed > desiredMoveSpeed && state != MovementState.dashend)
        {
            keepMomentum = true;
        }
        else
        {
            keepMomentum = false;
        }

        bool desiredMoveSpeedHasChanged = desiredMoveSpeed != lastDesiredMoveSpeed;
        lastDesiredMoveSpeed = desiredMoveSpeed;
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

        float boostFactor = speedChangeFactor;

        while (time < difference)
        {
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);

            time += Time.deltaTime * boostFactor;

            yield return null;
        }

        moveSpeed = desiredMoveSpeed;
        speedChangeFactor = 1f;
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
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * crouchMultiplier, ForceMode.Force);

        // on ground
        else if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        // in air
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

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
        Invoke(nameof(ResetJump), jumpCooldown);
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
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
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

        if (jumping == true || state == MovementState.crouching)
            CancelInvoke(nameof(DashEnd));
    }

    private void Dash(InputAction.CallbackContext context)
    {
        if (dashCdTimer > 0 || !StaminaCheck(dashStamina)) return;
        else 
        StaminaConsume(dashStamina);
        dashCdTimer = dashCd;
        state = MovementState.dashing;

        dashing = true;

        //cam.DoFov(dashFov);

        Transform forwardT;

        forwardT = orientation; /// where you're facing (no up or down)

        Vector3 direction = GetDirection(forwardT);

        Vector3 forceToApply = direction * dashForce;

        rb.useGravity = false;

        delayedForceToApply = forceToApply;
        Invoke(nameof(DelayedDashForce), 0.025f);

        Invoke(nameof(DashEnd), dashDuration);
        Invoke(nameof(ResetDash), dashDuration + dashEndDuration);
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
