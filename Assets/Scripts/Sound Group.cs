using UnityEngine.Audio;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundGroup", menuName = "ScriptableObjects/SoundGroup")]
public class SoundGroup : ScriptableObject
{
    public string groupName;
    public Sound[] sounds;
}
