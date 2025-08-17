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

    public void Play(string name)
    {
        Sound s = FindSoundByName(name);
        s.source.Play();
    }

    public void SetVolume(string name, float volume)
    {
        Sound s = FindSoundByName(name);
        s.source.volume = volume;
    }

    public void SetPitch(string name, float pitch)
    {
        Sound s = FindSoundByName(name);
        s.source.pitch = pitch;
    }

    public void RandomizePitch(string name)
    {
        Sound s = FindSoundByName(name);
        s.source.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
    }

    public void Stop(string name) 
    {
        Sound s = FindSoundByName(name);
        s.source.Stop();
    }

    private Sound FindSoundByName(string name)
    {
        return Array.Find(sounds, sound => sound.name == name);
    }
}
