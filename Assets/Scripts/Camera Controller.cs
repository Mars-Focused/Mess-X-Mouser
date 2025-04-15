using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public float sensX;
    public float sensY;

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
        //Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        look = protagControls.Player.Look;
    }

    private void OnDisable()
    {
        look.Disable();
    }

    private void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //this is used to rotate the camera BOTH axis but to rotate the player around only the Y axis. 
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        orientation.rotation = Quaternion.Euler(0f, yRotation, 0);


    }
}
