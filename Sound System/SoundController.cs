using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Audio;

/// <summary>
/// A Singleton class for playing and manipulating sounds. Any sound effect or song you want to play must first be created
/// as a scriptable object (Sound.cs) and then referenced by the name given there for all functions.
/// </summary>
public class SoundController : Singleton<SoundController>
{
    // Internal map for keeping track of Sounds and their associated Audio Sources. 
    private Dictionary<Sound, AudioSource> SoundLookup;
    // All the sounds that are looping in given context (bg music or sfx etc...)
    private List<Sound> loopingSounds;
    // Should always be 1:1 in terms of sounds that should be fading and their fade rate (same size, same index)
    private List<Sound> soundsToFade;
    private List<float> soundFadeRate;
    private List<bool> fadeINOUTRequests;
    // Parent object for one shot type sounds in the editor. Automatically generated in Awake. 
    private GameObject oneShotParent;
    // Stores pooled audio sources to use when the next new sound is requested
    private Queue<AudioSource> freeSources;
    private const int audioSourceAllocationAmmount = 50;


    [SerializeField] private float maxSoundDistance = 15f;
    private GameObject playerObject;

    protected override void _Initialize(System.Action onInitialized)
    {
        SoundLookup = new Dictionary<Sound, AudioSource>();

        playerObject = GameObject.FindWithTag("Player");

        // Set up one shot parent
        oneShotParent = new GameObject();
        oneShotParent.transform.SetParent(transform);

        loopingSounds = new List<Sound>();
        soundsToFade = new List<Sound>();
        soundFadeRate = new List<float>();
        fadeINOUTRequests = new List<bool>();

        // Starting audio sources
        freeSources = new Queue<AudioSource>();
        AllocateNewAudioSources(audioSourceAllocationAmmount);
        onInitialized?.Invoke();
    }
    

    // Allocated a set number of new audio sources such that we don't need to
    // perform a new allocation for every sound call
    private void AllocateNewAudioSources(int ammount)
    {
        for(int i = 0; i < ammount; i++)
        {
            freeSources.Enqueue(gameObject.AddComponent<AudioSource>());
        }
    }

    // Gets the next free audio source from the free source queue or allocated a new block
    private AudioSource GetFreeAudioSource()
    {
        if (freeSources.Count == 0)
        {
            AllocateNewAudioSources(audioSourceAllocationAmmount);
        }
        return freeSources.Dequeue();
    }

    // Overloaded version of the function taking in a Sound class instead.
    private bool GenerateAudioSource(Sound s)
    {
        AudioSource NewSource = GetFreeAudioSource();
        NewSource.clip = s.Clip;
        NewSource.volume = s.BaseVolume;
        NewSource.pitch = s.BasePitch;
        NewSource.loop = s.ShouldLoop;
        NewSource.outputAudioMixerGroup = s.AudioMixerGroup;

        SoundLookup.Add(s, NewSource);
        return true;
    }

    // Checks if the given sound by name can be played. If the sound doesn't currently
    // have an audio source, one will be created. Will only return false if creating
    // the audio source failed- ie. the sound cannot be loaded from the given resource path.
    private bool CanPlaySound(Sound s)
    {
        if (!SoundLookup.ContainsKey(s))
        {
            return GenerateAudioSource(s);
        }
        else
        {
            return true;
        }
    }

    public void Update()
    {
        if (!IsInitialized) return;

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
                        SoundLookup[loopingSounds[i]].volume += Time.deltaTime * soundFadeRate[x];
                        //If this makes it silent, pause the sound.
                        if (SoundLookup[loopingSounds[i]].volume >= loopingSounds[i].BaseVolume)
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
                        SoundLookup[loopingSounds[i]].volume -= Time.deltaTime * soundFadeRate[x];
                        //If this makes it silent, pause the sound.
                        if (SoundLookup[loopingSounds[i]].volume <= 0)
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
    public void CrossFade(Sound outSong, Sound inSong, float crossFadeTime)
    {
        FadeOutSound(outSong, crossFadeTime);
        FadeInSound(inSong, crossFadeTime, 0);
    }

    /// <summary>
    /// Given the name of a sound, plays it with its base pitch randomly modified by the given variance. 
    /// </summary>
    public void PlaySoundRandomPitch(Sound s, float pitchVariance)
    {
        if (CanPlaySound(s))
        {
            AudioSource Source = SoundLookup[s];

            Source.Stop();
            Source.Play();
            Source.pitch = s.BasePitch + UnityEngine.Random.Range(-pitchVariance, pitchVariance);

            Source.volume = s.BaseVolume;

            if (s.ShouldLoop)
            {
                if (!loopingSounds.Contains(s))
                {
                    loopingSounds.Add(s);
                }
            }
        }
    }

    /// <summary>
    /// Given the name of a sound, plays it as a one shot with its base pitch randomly modified by the given variance.
    /// Useful if you want to play the same sound many times in a row with overlap between them as one shots are each a
    /// temporary unique audio source. 
    /// </summary>
    public void PlaySoundOneShotRandomPitch(Sound s, float pitchVariance)
    {
        if (CanPlaySound(s))
        {
            PlaySoundOneShotRandomPitch(s, pitchVariance, s.BaseVolume);
        }
    }

    /// <summary>
    /// Overload of the same function allowing specification of the base volume. 
    /// </summary>
    public void PlaySoundOneShotRandomPitch(Sound s, float pitchVariance, float basevolume)
    {
        if (CanPlaySound(s))
        {

            GameObject newOneShot = new GameObject();
            newOneShot.transform.SetParent(oneShotParent.transform);
            AudioSource source = newOneShot.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = s.AudioMixerGroup;
            source.clip = s.Clip;
            source.volume = basevolume;
            source.pitch = s.BasePitch + UnityEngine.Random.Range(-pitchVariance, pitchVariance);
            source.Play();

            Destroy(newOneShot, s.Clip.length);
        }
    }

    /// <summary>
    /// Play given looping or non-looping sound by name. Volume will be set to base volume. 
    /// </summary>
    public void PlaySound(Sound s)
    {
        if (CanPlaySound(s))
        {
            AudioSource Source = SoundLookup[s];

            Source.Stop();
            Source.Play();
            Source.pitch = s.BasePitch;
            Source.volume = s.BaseVolume;
            if (s.ShouldLoop)
            {
                if (!loopingSounds.Contains(s))
                {
                    loopingSounds.Add(s);
                }
            }
        }
    }

    /// <summary>
    /// Plays a sound from a specific position with volume based on distance to player,
    /// with random pitch variation.
    /// </summary>
    /// name is the sound to play
    /// originPosition is n where the sound originates from
    /// pitchVariance is the maximum pitch variation (+/- this value)
    public void PlaySoundAtPositionRandomPitch(Sound s, Vector3 originPosition, float pitchVariance)
    {
        if (playerObject == null || !CanPlaySound(s))
        {
            return;
        }

        float distance = Vector3.Distance(originPosition, playerObject.transform.position);
        float volumePercentage = Mathf.Clamp01(1 - (distance / maxSoundDistance));
        
        if (volumePercentage <= 0)
        {
            return;
        }

        GameObject tempSoundObject = new GameObject("TempSound");
        tempSoundObject.transform.position = originPosition;
        tempSoundObject.transform.SetParent(oneShotParent.transform);
        
        AudioSource source = tempSoundObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = s.AudioMixerGroup;
        source.clip = s.Clip;
        source.volume = s.BaseVolume * volumePercentage;
        source.pitch = s.BasePitch + UnityEngine.Random.Range(-pitchVariance, pitchVariance);
        source.spatialBlend = 0;
        source.Play();

        Destroy(tempSoundObject, s.Clip.length);
    }

    /// <summary>
    /// Plays a sound from a specific position with volume based on distance to player.
    /// Volume decreases linearly from 100% at 0 distance to 0% at maxSoundDistance.
    /// </summary>
    /// name is the sound to play
    /// originPosition is where the sound originates from<
    public void PlaySoundAtPosition(Sound s, Vector3 originPosition)
    {
        if (playerObject == null || !CanPlaySound(s))
        {
            return;
        }

        // Calculate distance to player
        float distance = Vector3.Distance(originPosition, playerObject.transform.position);
        
        // Calculate volume percentage (1 at 0 distance, 0 at maxSoundDistance)
        float volumePercentage = Mathf.Clamp01(1 - (distance / maxSoundDistance));
        
        // If completely out of range, don't play the sound
        if (volumePercentage <= 0)
        {
            return;
        }

        // Create a temporary audio source at the position
        GameObject tempSoundObject = new GameObject("TempSound");
        tempSoundObject.transform.position = originPosition;
        tempSoundObject.transform.SetParent(oneShotParent.transform);
        
        AudioSource source = tempSoundObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = s.AudioMixerGroup;
        source.clip = s.Clip;
        source.volume = s.BaseVolume * volumePercentage;
        source.pitch = s.BasePitch;
        source.spatialBlend = 0; // Ensure it's 2D sound since we're handling distance manually
        source.Play();

        Destroy(tempSoundObject, s.Clip.length);
    }

    /// <summary>
    /// Pause given sound by name.
    /// </summary>
    public void PauseSound(Sound s)
    {
        if (SoundLookup.ContainsKey(s))
        {
            AudioSource Source = SoundLookup[s];

            Source.Pause();
            if (s.ShouldLoop)
            {
                loopingSounds.Remove(s);
            }
        }
    }

    /// <summary>
    /// Fades out looping sound to silence over the given time in seconds.
    /// </summary>
    public void FadeOutSound(Sound s, float fadeTime)
    {
        if (CanPlaySound(s))
        {
            float FadeRate = 1 / fadeTime;

            if (s.ShouldLoop == false)
            {
                return;
            }

            if (soundsToFade.Contains(s))
            {
                // This sound is already being faded. Interrupt and replace it.
                StopSoundFade(s);
            }

            soundsToFade.Add(s);
            soundFadeRate.Add(FadeRate);
            fadeINOUTRequests.Add(false);
        }
    }

    /// <summary>
    /// Checks if given sound by name is current being faded in/out.
    /// </summary>
    public bool IsSoundBeingFaded(Sound s)
    {
        return soundsToFade.Contains(s);
    }

    /// <summary>
    /// Immediately interrupts the fading in/out of a given sound by name.
    /// </summary>
    public void StopSoundFade(Sound s)
    {
        fadeINOUTRequests.RemoveAt(soundsToFade.IndexOf(s));
        soundFadeRate.RemoveAt(soundsToFade.IndexOf(s));
        soundsToFade.Remove(s);
    }

    /// <summary>
    /// Fades in a looping sound from the start volume to its base volume over the given time in seconds.
    /// </summary>
    public void FadeInSound(Sound s, float fadeTime, float startVol)
    {
        if (CanPlaySound(s))
        {
            float FadeRate = 1 / fadeTime;
            AudioSource Source = SoundLookup[s];

            if (s.ShouldLoop == false)
            {
                return;
            }

            if (soundsToFade.Contains(s))
            {
                // This sound is already being faded. Interrupt and replace it.
                StopSoundFade(s);
            }

            soundsToFade.Add(s);
            soundFadeRate.Add(FadeRate);
            fadeINOUTRequests.Add(true);

            PlaySound(s);
            Source.volume = startVol;
        }
    }

    /// <summary>
    /// Returns true if a given looping sound is currently playing.
    /// </summary>
    public bool IsSoundPlaying(Sound s)
    {
        return loopingSounds.Contains(s);
    }
}