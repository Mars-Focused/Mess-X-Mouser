using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public float sensX = 0.2f;
    public float sensY = 0.2f;

    public Transform orientation;
    public ProtagControls protagControls;
    private InputAction look;

    float xRotation;
    float yRotation;

    private void Awake()
    {
        protagControls = new ProtagControls(); // <-Nessesary for NewInput Controller
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        look = protagControls.Player.Look;
        look.Enable();
    }

    private void OnDisable()
    {
        look.Disable();
    }

    private void Update()
    {
        float mouseX = look.ReadValue<Vector2>().x * sensX;
        float mouseY = look.ReadValue<Vector2>().y * sensY;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //this is used to rotate the camera BOTH axis but to rotate the player around only the Y axis. 
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        orientation.rotation = Quaternion.Euler(0f, yRotation, 0);
    }
}
