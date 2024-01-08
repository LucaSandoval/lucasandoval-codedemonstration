using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// This is the master sound player. Sounds can be added to the sound player from the inspector, or done
// programmatically by adding sounds when they are needed. Contains a wide variety of functionality for playing
// different sounds in a game: Playing/Pausing/Stopping, Fading In/Out, Playing at a random pitch, CrossFading,
// playing layered sounds (useful if you have dynamic audio layers), 

public class SoundPlayer : MonoBehaviour
{
    public List<Sound> sounds; //The list of all sounds/songs used in this level, scene etc. There should only be 1 sound of each name.

    public LayeredSound[] layeredSounds; //The list of all the layered sounds/songs for this context. Composed of several sounds. 

    private List<string> loopingSounds; //All the sounds that are looping in given context (bg music or sfx etc...)

    //Should always be 1:1 in terms of sounds that should be fading and their fade rate (same size, same index)
    private List<string> soundsToFade;
    private List<float> soundFadeRate;
    private List<bool> fadeINOUTRequests;

    private GameObject oneShotParent;

    private bool backGroundSoundsMutedGlobally;

    private RoomController roomController;

    [System.Serializable]
    public struct LayeredSound
    {
        public string layeredSoundName;
        public Sound[] layers;
    }

    public void Awake()
    {
        roomController = GameObject.Find("GameController").GetComponent<RoomController>();
        oneShotParent = GameObject.Find("OneShotParent");

        loopingSounds = new List<string>();
        soundsToFade = new List<string>();
        soundFadeRate = new List<float>();
        fadeINOUTRequests = new List<bool>();

        //Create an audio source for each non-looping sound effect to share. 
        foreach (Sound s in sounds)
        {
            GenerateAudioSource(s);
        }

        //Layers in a layered sound must be looping, and each get their own audio source as a result. 
        for (int i = 0; i < layeredSounds.Length; i++)
        {
            foreach (Sound s in layeredSounds[i].layers)
            {
                GenerateAudioSource(s);
            }
        }
    }

    //Given a Sound, create an AudioSource and add it to the controller object with the specified settings
    public void GenerateAudioSource(Sound s)
    {
        s.source = gameObject.AddComponent<AudioSource>();
        s.source.clip = s.clip;
        s.source.volume = s.baseVolume;
        s.source.pitch = s.basePitch;
        s.source.loop = s.loop;
        s.source.outputAudioMixerGroup = s.audioMixerGroup;
    }

    public void FixedUpdate()
    {
        for (int i = 0; i < loopingSounds.Count; i++)
        {
            for (int x = 0; x < soundsToFade.Count; x++)
            {
                if (loopingSounds[i] == soundsToFade[x])
                {
                    //Determine if the fade is IN or OUT
                    if (fadeINOUTRequests[x] == true)
                    {
                        //IN

                        //If there is a looping sound that gets a fade request, decrease its volume by the rate
                        getSourceByName(loopingSounds[i]).volume += Time.deltaTime * soundFadeRate[x];
                        //If this makes it silent, pause the sound.
                        if (getSourceByName(loopingSounds[i]).volume >= getSoundBaseVolume(loopingSounds[i]))
                        {
                            soundsToFade.Remove(soundsToFade[x]);
                            soundFadeRate.Remove(soundFadeRate[x]);
                            fadeINOUTRequests.RemoveAt(x);
                        }
                    }
                    else
                    {
                        //OUT

                        //If there is a looping sound that gets a fade request, decrease its volume by the rate
                        getSourceByName(loopingSounds[i]).volume -= Time.deltaTime * soundFadeRate[x];
                        //If this makes it silent, pause the sound.
                        if (getSourceByName(loopingSounds[i]).volume <= 0)
                        {
                            //PauseSound(loopingSounds[i]);
                            soundsToFade.Remove(soundsToFade[x]);
                            soundFadeRate.Remove(soundFadeRate[x]);
                            fadeINOUTRequests.RemoveAt(x);
                        }
                    }
                }
            }
        }
    }

    // Cross fade two sounds given a rate. 
    public void CrossFade(string outSong, string inSong, float rate)
    {
        FadeOutSound(outSong, rate);
        FadeInSound(inSong, rate, 0);
    }

    // Plays a sound at a random pitch within given range. 
    public void PlaySoundRandomPitch(string name, float range)
    {
        foreach (Sound s in sounds)
        {
            if (s.soundName == name)
            {
                s.source.Stop();
                s.source.Play();
                s.source.pitch = s.basePitch + Random.Range(-range, range);

                s.source.volume = s.baseVolume;

                if (s.loop)
                {
                    if (!loopingSounds.Contains(s.soundName))
                    {
                        loopingSounds.Add(s.soundName);
                    }
                }
                return;
            }
        }
    }

    // Play sound as a one shot with a random pitch. 
    public void PlaySoundOneShotRandomPitch(string name, float range)
    {
        foreach (Sound s in sounds)
        {
            if (s.soundName == name)
            {
                GameObject newOneShot = new GameObject();
                newOneShot.transform.SetParent(oneShotParent.transform);
                AudioSource source = newOneShot.AddComponent<AudioSource>();
                source.outputAudioMixerGroup = s.audioMixerGroup;
                source.clip = s.clip;
                source.volume = s.baseVolume;
                source.pitch = s.basePitch + Random.Range(-range, range);
                source.Play();

                Destroy(newOneShot, s.clip.length);
            }
        }
    }

    //Play given looping or non-looping sound by name. Volume will be set to base volume. 
    public void PlaySound(string name)
    {
        foreach (Sound s in sounds)
        {
            if (s.soundName == name)
            {
                s.source.Stop();
                s.source.Play();
                s.source.pitch = s.basePitch;

                if (s.isBackgroundNoise)
                {
                    s.source.volume = backGroundSoundsMutedGlobally ? 0f : s.baseVolume;
                } else
                {
                    s.source.volume = s.baseVolume;
                }

                if (s.loop)
                {
                    if (!loopingSounds.Contains(s.soundName))
                    {
                        loopingSounds.Add(s.soundName);
                    }

                    //Prevents error after skipping music in cutscenes
                    //if (soundsToFade.Contains(s.soundName))
                    //{
                    //    soundsToFade.Remove(s.soundName);
                    //}
                }
                return;
            }
        }

        for (int i = 0; i < layeredSounds.Length; i++)
        {
            for (int x = 0; x < layeredSounds[i].layers.Length; x++)
            {
                if (layeredSounds[i].layers[x].soundName == name)
                {
                    layeredSounds[i].layers[x].source.Play();
                    layeredSounds[i].layers[x].source.volume = layeredSounds[i].layers[x].baseVolume;

                    if (layeredSounds[i].layers[x].loop)
                    {
                        if (!loopingSounds.Contains(layeredSounds[i].layers[x].soundName))
                        {
                            loopingSounds.Add(layeredSounds[i].layers[x].soundName);
                        }
                    }

                    return;
                }
            }
        }
    }

    //Pause given sound by name.
    public void PauseSound(string name)
    {
        foreach (Sound s in sounds)
        {
            if (s.soundName == name)
            {
                s.source.Pause();
                if (s.loop)
                {
                    loopingSounds.Remove(s.soundName);
                }
                return;
            }
        }

        for (int i = 0; i < layeredSounds.Length; i++)
        {
            for (int x = 0; x < layeredSounds[i].layers.Length; x++)
            {
                if (layeredSounds[i].layers[x].soundName == name)
                {
                    if (layeredSounds[i].layers[x].loop)
                    {
                        layeredSounds[i].layers[x].source.Pause();
                        loopingSounds.Remove(layeredSounds[i].layers[x].soundName);
                    }
                    return;
                }
            }
        }
    }

    //Plays a layered sound. All layers will be played with the corresponding volume number in the given array.
    public void PlayLayeredSound(string name, float[] volumeLevels)
    {
        foreach (LayeredSound s in layeredSounds)
        {
            if (s.layeredSoundName == name)
            {
                if (volumeLevels.Length == 0 || volumeLevels.Length > s.layers.Length)
                {
                    Debug.LogWarning("Given layer volume levels doesn't match number of layers for sound: " + name);
                    return;
                }

                for (int i = 0; i < s.layers.Length; i++)
                {
                    s.layers[i].source.Stop();
                    s.layers[i].source.Play();
                    s.layers[i].source.volume = volumeLevels[i];

                    if (s.layers[i].loop)
                    {
                        if (!loopingSounds.Contains(s.layers[i].soundName))
                        {
                            loopingSounds.Add(s.layers[i].soundName);
                        }
                    }
                }
                return;
            }
        }
    }

    //Fades out looping sound to silence at a given rate. Not used with Layered Sounds. 
    public void FadeOutSound(string name, float rate)
    {
        foreach (Sound s in sounds)
        {
            if (s.soundName == name)
            {
                if (s.loop == false)
                {
                    return;
                }

                if (soundsToFade.Contains(name))
                {
                    // This sound is already being faded. Interrupt and replace it.
                    StopSoundFade(name);
                }

                soundsToFade.Add(name);
                soundFadeRate.Add(rate);
                fadeINOUTRequests.Add(false);
                return;
            }
        }
    }

    // Checks if given sound by name is current being faded in/out.
    public bool IsSoundBeingFaded(string name)
    {
        return soundsToFade.Contains(name);
    }

    // Immediately interrupts the fading in/out of a given sound by name.
    public void StopSoundFade(string name)
    {
        fadeINOUTRequests.RemoveAt(soundsToFade.IndexOf(name));
        soundFadeRate.RemoveAt(soundsToFade.IndexOf(name));
        soundsToFade.Remove(name);
    }

    //Fades IN looping sound, playing it. Not used with Layered Sounds. 

    public void FadeInSound(string name, float rate, float startVol)
    {
        foreach (Sound s in sounds)
        {
            if (s.soundName == name)
            {
                if (s.loop == false)
                {
                    return;
                }

                if (soundsToFade.Contains(name))
                {
                    // This sound is already being faded. Interrupt and replace it.
                    StopSoundFade(name);
                }

                soundsToFade.Add(name);
                soundFadeRate.Add(rate);
                fadeINOUTRequests.Add(true);

                PlaySound(s.soundName);
                s.source.volume = startVol;
                return;
            }
        }
    }

    //Fades out given layer to silence in layered sound with rate.
    public void FadeOutLayer(string name, int layerID, float rate)
    {
        foreach (LayeredSound s in layeredSounds)
        {
            if (name == s.layeredSoundName)
            {
                if (layerID < s.layers.Length)
                {
                    if (soundsToFade.Contains(s.layers[layerID].soundName))
                    {
                        return;
                    }

                    soundsToFade.Add(s.layers[layerID].soundName);
                    soundFadeRate.Add(rate);
                    fadeINOUTRequests.Add(false);
                    return;
                }
                else
                {
                    Debug.LogWarning("Given layer ID not in given layer name.");
                }
            }
        }
    }

    //Fades in layer in layered sound with rate. Will not play the layer if it isn't already playing. 
    public void FadeInLayer(string name, int layerID, float rate)
    {
        foreach (LayeredSound s in layeredSounds)
        {
            if (name == s.layeredSoundName)
            {
                if (layerID < s.layers.Length)
                {
                    if (soundsToFade.Contains(s.layers[layerID].soundName))
                    {
                        return;
                    }

                    soundsToFade.Add(s.layers[layerID].soundName);
                    soundFadeRate.Add(rate);
                    fadeINOUTRequests.Add(true);
                    return;
                }
                else
                {
                    Debug.LogWarning("Given layer ID not in given layer name.");
                }
            }
        }
    }

    //Sets layer of given layered sound volume to given volume as a percentage out of 100. (0% = silent, 100% = full baseVolume).
    public void SetLayerVolume(string name, int layerID, float percent)
    {
        foreach (LayeredSound s in layeredSounds)
        {
            if (name == s.layeredSoundName)
            {
                if (layerID < s.layers.Length)
                {
                    float newVol = s.layers[layerID].baseVolume * (percent / 100);
                    s.layers[layerID].source.volume = newVol;
                    return;
                }
                else
                {
                    Debug.LogWarning("Given layer ID not in given layer name.");
                }
            }
        }
    }



    //Internal Functions//

    //Used internal to get AudioSource object of given sound.
    public AudioSource getSourceByName(string name)
    {
        foreach (Sound s in sounds)
        {
            if (s.soundName == name)
            {
                return s.source;
            }
        }

        for (int i = 0; i < layeredSounds.Length; i++)
        {
            for (int x = 0; x < layeredSounds[i].layers.Length; x++)
            {
                if (layeredSounds[i].layers[x].soundName == name)
                {
                    return layeredSounds[i].layers[x].source;
                }
            }
        }

        return null;
    }

    //Used internally to get base volume of audio source of given sound.
    private float getSoundBaseVolume(string name)
    {
        foreach (Sound s in sounds)
        {
            if (s.soundName == name)
            {
                return s.baseVolume;
            }
        }

        for (int i = 0; i < layeredSounds.Length; i++)
        {
            for (int x = 0; x < layeredSounds[i].layers.Length; x++)
            {
                return layeredSounds[i].layers[x].baseVolume;
            }
        }

        return 1;
    }

    public bool isSoundPlaying(string name)
    {
        return loopingSounds.Contains(name);
    }

    //Go through all looping (current) sounds and fade out any that are marked
    //as being 'background sounds'.
    public void MuteAllBackgroundSounds(float rate)
    {
        backGroundSoundsMutedGlobally = true;
        for (int i = 0; i < sounds.Count; i++)
        {
            if (sounds[i].isBackgroundNoise)
            {
                if (isSoundPlaying(sounds[i].soundName))
                {
                    FadeOutSound(sounds[i].soundName, rate);
                }              
            }
        }
    }

    // Searches through and unmutes all sounds marked as 'background sounds.'
    public void UnMuteAllBackgroundSounds(float rate)
    {
        backGroundSoundsMutedGlobally = false;
        for (int i = 0; i < sounds.Count; i++)
        {
            if (sounds[i].isBackgroundNoise)
            {
                if (IsSoundRoomAmbientSound(sounds[i]))
                {
                    FadeInSound(sounds[i].soundName, rate, 0);
                }
            }
        }
    }

}
