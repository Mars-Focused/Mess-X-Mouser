using UnityEngine.Audio;
using System;
using UnityEngine;


public class AudioManagerBrackeys : MonoBehaviour
{
    public Sound[] sounds;

    // Start is called before the first frame update
    void Awake()
    {
        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;

            s.source.volume = 1f;
            s.source.pitch = 1f;
        }
    }

    public void Play (string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.pitch = UnityEngine.Random.Range(0.95f, 1.1f);
        s.source.Play();
    }
}
