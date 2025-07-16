using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SoundManager 
{
    public static void PlaySount()
    {
        GameObject soundGameObject = new GameObject("sound");
        AudioSource audioSource = soundGameObject.AddComponent<AudioSource>();
        audioSource.PlayOneShot(GameAssets.i.playerAttack);
    }
}
