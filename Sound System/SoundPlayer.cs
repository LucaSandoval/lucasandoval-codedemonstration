using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Audio;

/// <summary>
/// A Singleton class for playing and manipulating sounds. Any sound effect or song you want to play must first be created
/// as a scriptable object (Sound.cs) and then referenced by the name given there for all functions.
/// </summary>
public class SoundPlayer : Singleton<SoundPlayer>
{
    // The path in the Resources Folder with which to look for Sound scriptable objects.
    public string SoundLookupPath;
    // If you would like an AudioSource to be generated for any sounds on Awake instead of at 
    // runtime, list them in the inspector here.
    public List<Sound> Sounds; 
    // Internal map for keeping track of Sounds and their associated Audio Sources. 
    private Dictionary<string, Tuple<Sound, AudioSource>> SoundLookup;
    // All the sounds that are looping in given context (bg music or sfx etc...)
    private List<string> loopingSounds; 
    // Should always be 1:1 in terms of sounds that should be fading and their fade rate (same size, same index)
    private List<string> soundsToFade;
    private List<float> soundFadeRate;
    private List<bool> fadeINOUTRequests;
    // Parent object for one shot type sounds in the editor. Automatically generated in Awake. 
    private GameObject oneShotParent;

    protected override void Awake()
    {
        base.Awake();
        SoundLookup = new Dictionary<string, Tuple<Sound, AudioSource>>();
        
        // Set up one shot parent
        oneShotParent = new GameObject();
        oneShotParent.transform.SetParent(transform);

        loopingSounds = new List<string>();
        soundsToFade = new List<string>();
        soundFadeRate = new List<float>();
        fadeINOUTRequests = new List<bool>();

        //Create an audio source for each sound present before runtime in the inspector.
        foreach (Sound s in Sounds)
        {
            GenerateAudioSource(s);
        }
    }

    //Given the name of a Sound, create an AudioSource and add it to the controller object with the specified settings.
    //Returns true if the audio source was created sucessfully. 
    private bool GenerateAudioSource(string soundName)
    {
        string FullPath = SoundLookupPath + "/" + soundName;
        Sound Sound = Resources.Load<Sound>(FullPath);
        if (Sound != null)
        {
            return GenerateAudioSource(Sound);
        } else
        {
            Debug.LogWarning("No sound found at " + FullPath);
            return false;
        }
    }

    // Overloaded version of the function taking in a Sound class instead.
    private bool GenerateAudioSource(Sound s)
    {
        AudioSource NewSource = gameObject.AddComponent<AudioSource>();
        NewSource.clip = s.Clip;
        NewSource.volume = s.BaseVolume;
        NewSource.pitch = s.BasePitch;
        NewSource.loop = s.ShouldLoop;
        NewSource.outputAudioMixerGroup = s.AudioMixerGroup;

        SoundLookup.Add(s.SoundName, new Tuple<Sound, AudioSource>(s, NewSource));
        return true;
    }

    // Checks if the given sound by name can be played. If the sound doesn't currently
    // have an audio source, one will be created. Will only return false if creating
    // the audio source failed- ie. the sound cannot be loaded from the given resource path.
    private bool CanPlaySound(string soundName)
    {
        if (GetSourceByName(soundName) == null)
        {
            return GenerateAudioSource(soundName);
        } else
        {
            return true;
        }
    }

    public void Update()
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
                        float baseVolume = GetSoundByName(loopingSounds[i]).BaseVolume;
                        GetSourceByName(loopingSounds[i]).volume += baseVolume * Time.deltaTime * soundFadeRate[x];
                        //If this makes it silent, pause the sound.
                        if (GetSourceByName(loopingSounds[i]).volume >= GetSoundBaseVolume(loopingSounds[i]))
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
                        float baseVolume = GetSoundByName(loopingSounds[i]).BaseVolume;
                        GetSourceByName(loopingSounds[i]).volume -= baseVolume * Time.deltaTime * soundFadeRate[x];
                        //If this makes it silent, pause the sound.
                        if (GetSourceByName(loopingSounds[i]).volume <= 0)
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

    /// <summary>
    /// Given the name of two sounds, fades out the first and fades in the second over the given
    /// time in seconds. 
    /// </summary>
    public void CrossFade(string outSong, string inSong, float crossFadeTime)
    {
        FadeOutSound(outSong, crossFadeTime);
        FadeInSound(inSong, crossFadeTime, 0);
    }

    /// <summary>
    /// Given the name of a sound, plays it with its base pitch randomly modified by the given variance. 
    /// </summary>
    public void PlaySoundRandomPitch(string soundName, float pitchVariance)
    {
        if (CanPlaySound(soundName))
        {
            AudioSource Source = GetSourceByName(soundName);
            Sound SoundObject = GetSoundByName(soundName);

            Source.Stop();
            Source.Play();
            Source.pitch = SoundObject.BasePitch + UnityEngine.Random.Range(-pitchVariance, pitchVariance);

            Source.volume = SoundObject.BaseVolume;

            if (SoundObject.ShouldLoop)
            {
                if (!loopingSounds.Contains(SoundObject.SoundName))
                {
                    loopingSounds.Add(SoundObject.SoundName);
                }
            }
        }
    }

    /// <summary>
    /// Given the name of a sound, plays it as a one shot with its base pitch randomly modified by the given variance.
    /// Useful if you want to play the same sound many times in a row with overlap between them as one shots are each a
    /// temporary unique audio source. 
    /// </summary>
    public void PlaySoundOneShotRandomPitch(string soundName, float pitchVariance)
    {
        if (CanPlaySound(soundName))
        {
            PlaySoundOneShotRandomPitch(soundName, pitchVariance, GetSoundBaseVolume(soundName));
        }
    }

    /// <summary>
    /// Overload of the same function allowing specification of the base volume. 
    /// </summary>
    public void PlaySoundOneShotRandomPitch(string soundName, float pitchVariance, float basevolume)
    {
        if (CanPlaySound(soundName))
        {
            Sound SoundObject = GetSoundByName(soundName);

            GameObject newOneShot = new GameObject();
            newOneShot.transform.SetParent(oneShotParent.transform);
            AudioSource source = newOneShot.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = SoundObject.AudioMixerGroup;
            source.clip = SoundObject.Clip;
            source.volume = basevolume;
            source.pitch = SoundObject.BasePitch + UnityEngine.Random.Range(-pitchVariance, pitchVariance);
            source.Play();

            Destroy(newOneShot, SoundObject.Clip.length);
        }
    }

    // Given the name of a sound, returns its base volume as defined in the sound scriptable object. 
    private float GetSoundBaseVolume(string soundName)
    {
        if (SoundLookup.ContainsKey(soundName))
        {
            return SoundLookup[soundName].Item1.BaseVolume;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Play given looping or non-looping sound by name. Volume will be set to base volume. 
    /// </summary>
    public void PlaySound(string soundName)
    {
        if (CanPlaySound(soundName))
        {
            AudioSource Source = GetSourceByName(soundName);
            Sound SoundObject = GetSoundByName(soundName);
            Source.Stop();
            Source.Play();
            Source.pitch = SoundObject.BasePitch;
            Source.volume = SoundObject.BaseVolume;
            if (SoundObject.ShouldLoop)
            {
                if (!loopingSounds.Contains(soundName))
                {
                    loopingSounds.Add(soundName);
                }
            }
        }
    }

    /// <summary>
    /// Pause given sound by name.
    /// </summary>
    public void PauseSound(string soundName)
    {
        if (GetSourceByName(soundName) != null)
        {
            AudioSource Source = GetSourceByName(soundName);
            Sound SoundObject = GetSoundByName(soundName);
            Source.Pause();
            if (SoundObject.ShouldLoop)
            {
                loopingSounds.Remove(SoundObject.SoundName);
            }
        }
    }

    /// <summary>
    /// Fades out looping sound to silence over the given time in seconds.
    /// </summary>
    public void FadeOutSound(string soundName, float fadeTime)
    {
        if (CanPlaySound(soundName))
        {
            float FadeRate = 1 / fadeTime;
            Sound SoundObject = GetSoundByName(soundName);
            
            if (SoundObject.ShouldLoop == false)
            {
                return;
            }

            if (soundsToFade.Contains(soundName))
            {
                // This sound is already being faded. Interrupt and replace it.
                StopSoundFade(soundName);
            }

            soundsToFade.Add(soundName);
            soundFadeRate.Add(FadeRate);
            fadeINOUTRequests.Add(false);
        }
    }

    /// <summary>
    /// Checks if given sound by name is current being faded in/out.
    /// </summary>
    public bool IsSoundBeingFaded(string soundName)
    {
        return soundsToFade.Contains(soundName);
    }

    /// <summary>
    /// Immediately interrupts the fading in/out of a given sound by name.
    /// </summary>
    public void StopSoundFade(string soundName)
    {
        fadeINOUTRequests.RemoveAt(soundsToFade.IndexOf(soundName));
        soundFadeRate.RemoveAt(soundsToFade.IndexOf(soundName));
        soundsToFade.Remove(soundName);
    }

    /// <summary>
    /// Fades in a looping sound from the start volume to its base volume over the given time in seconds.
    /// </summary>
    public void FadeInSound(string soundName, float fadeTime, float startVol)
    {
        if (CanPlaySound(soundName))
        {
            float FadeRate = 1 / fadeTime;
            AudioSource Source = GetSourceByName(soundName);
            Sound SoundObject = GetSoundByName(soundName);

            if (SoundObject.ShouldLoop == false)
            {
                return;
            }

            if (soundsToFade.Contains(soundName))
            {
                // This sound is already being faded. Interrupt and replace it.
                StopSoundFade(soundName);
            }

            soundsToFade.Add(soundName);
            soundFadeRate.Add(FadeRate);
            fadeINOUTRequests.Add(true);

            PlaySound(SoundObject.SoundName);
            Source.volume = startVol;
        }
    }

    // Given the name of a sound, returns the associated AudioSource. 
    private AudioSource GetSourceByName(string soundName)
    {
        if (SoundLookup.ContainsKey(soundName))
        {
            return SoundLookup[soundName].Item2;
        } else
        {
            return null;
        }
    }

    // Given the name of a sound, returns the corresponding Sound scriptable object.
    private Sound GetSoundByName(string soundName)
    {
        if (SoundLookup.ContainsKey(soundName))
        {
            return SoundLookup[soundName].Item1;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Returns true if a given looping sound is currently playing.
    /// </summary>
    public bool IsSoundPlaying(string soundName)
    {
        return loopingSounds.Contains(soundName);
    }
