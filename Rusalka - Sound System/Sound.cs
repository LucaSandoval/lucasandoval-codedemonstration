using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "New Sound", menuName = "Music and Sounds/Sound Object")]
public class Sound : ScriptableObject
{
    public AudioClip Clip; // The clip itself (.wav, .mp3, etc.) 

    [Range(0, 3f)]
    public float BaseVolume = 1; // Base volume of the clip (can be lowered by fade in/out, but this value will not change.)
    [Range(0.2f, 3f)]
    public float BasePitch = 1;

    public bool ShouldLoop; // Should this sound loop itself? 

    public AudioMixerGroup AudioMixerGroup;
}