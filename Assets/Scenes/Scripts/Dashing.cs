using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Dashing : MonoBehaviour
{
    [Header("References")]
    public Transform orientation; // <- DON'T TRANSFER
    private Rigidbody rb; // <- DON'T TRANSFER
    private PlayerMovementDashing pm; // <- DON'T TRANSFER

    [Header("Dashing")]
    public float dashForce;
    public float maxDashYSpeed;
    public float dashDuration;
    public float dashEndDuration;

    [Header("Settings")]
    public bool allowAllDirections = true;
    public bool resetVel = true;

    [Header("Cooldown")]
    public float dashCd;
    private float dashCdTimer;

    [Header("Input")]
    public KeyCode dashKey = KeyCode.LeftShift;

    private void Start()
    {
        rb = GetComponent<Rigidbody>(); // <- DON'T TRANSFER
        pm = GetComponent<PlayerMovementDashing>(); // <- DON'T TRANSFER
        pm.dashing = false;
    }

    private void Update()
    {
        DashUpdates();
    }

    private void DashUpdates()
    {
        if (Input.GetKeyDown(dashKey))
            Dash();

        if (dashCdTimer > 0)
            dashCdTimer -= Time.deltaTime;

        if (pm.jumping == true)
            CancelInvoke(nameof(DashEnd));
    }

    private void Dash()
    {
        if (dashCdTimer > 0) return;
        else dashCdTimer = dashCd;

        pm.dashing = true;

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

    private Vector3 delayedForceToApply;
    private void DelayedDashForce()
    {
        if (resetVel)
            rb.velocity = Vector3.zero;

        rb.AddForce(delayedForceToApply, ForceMode.Impulse);
    }

    private void DashEnd()
    {
        pm.dashing = false;
        pm.dashEnd = true;
    }

    private void ResetDash()
    {
        pm.dashing = false;
        pm.dashEnd = false;
        rb.useGravity = true;
    }

    private Vector3 GetDirection(Transform forwardT)
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3();

        direction = forwardT.forward * verticalInput + forwardT.right * horizontalInput;

        if (verticalInput == 0 && horizontalInput == 0)
            direction = forwardT.forward;

        return direction.normalized;
    }
}
