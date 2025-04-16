using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDStaminaBar : MonoBehaviour
{
    private Image barImage;
    PlayerMovementDashing pm;

    private void Awake()
    {
        barImage = transform.Find("Bar").GetComponent<Image>();
        barImage.fillAmount = 0.3f;
    }

    private void Start()
    {
        pm = GameObject.Find("Protagonist").GetComponent<PlayerMovementDashing>();
    }

    private void Update()
    {
        float stamina = pm.GetPlayerStamina();
        float maxStamina = pm.GetPlayerMaxStamina();
        barImage.fillAmount = stamina / maxStamina;
    }
}
