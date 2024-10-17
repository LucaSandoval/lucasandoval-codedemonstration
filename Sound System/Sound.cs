using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// An object representing a Sound in a game and specific data about that Sound,
// such as its base volume, pitch, mixer channel (ie. SFX vs Music etc...), and its looping
// options. 

// In unity, this Sound is ScriptableObject which allows me to create and manage these in the Unity Game Engine
// as assets for convenient read and write. 

[CreateAssetMenu(fileName = "New Sound", menuName = "Music and Sounds/Sound Object")]
public class Sound : ScriptableObject
{
    public string soundName; // The name of the sound to be called in any of the SoundController functions.

    public AudioClip clip; // The clip itself (.wav, .mp3, etc.) 

    [Range(0, 3f)]
    public float baseVolume = 1; // Base volume of the clip (can be lowered by fade in/out, but this value will not change.)
    [Range(0.2f, 3f)]
    public float basePitch = 1; // Base pitch of the clip (can be lowered or raised by various functions, but this value will not change.) 

    public bool loop; // Should this sound loop itself? 

    public AudioMixerGroup audioMixerGroup; // Which audio mixer group (SFX vs. Music does this belong to?) 
}
