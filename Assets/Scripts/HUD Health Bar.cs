using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDHealthBar : MonoBehaviour
{
    private Image barImage;
    PlayerMovementDashing pm;

    private void Awake()
    {
        barImage = transform.Find("Bar").GetComponent<Image>();
    }

    private void Start()
    {
        pm = GameObject.Find("Protagonist").GetComponent<PlayerMovementDashing>();
    }

    private void Update()
    {
        float stamina = pm.GetPlayerHealth();
        float maxStamina = pm.GetPlayerMaxHealth();
        barImage.fillAmount = stamina / maxStamina;
    }
}
