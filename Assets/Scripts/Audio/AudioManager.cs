using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private Audio[] audios;
    
    public static AudioManager Instance { get; private set; }

    // Initialize each audio in the game
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        foreach (Audio a in audios)
        {
            a.source = gameObject.AddComponent<AudioSource>();   // set audio source for each audio
            a.source.clip = a.clip;
            a.source.volume = a.volume;
            a.source.pitch = a.pitch;
            a.source.loop = a.loop;
        }
    }


    public void Play(AudioClipsNames audioName, bool playOnce = true)
    {
        Audio a = Array.Find(audios, audio => audio.name == audioName);    // Search audio by name

        if (a == null)
        {
            Debug.Log("Audio " + audioName + " not found");
            return;
        }

        if (!playOnce)
            a.source.Play();
        else if(!a.source.isPlaying)   // TO AVOID AUDIO BUG 
            a.source.Play();    // a.source.PlayOneShot(a.clip); 
    }


    public void Play(string audioName)
    {
        Audio a = Array.Find(audios, audio => audio.name.ToString() == audioName);    // Search audio by name

        if (a == null)
        {
            Debug.Log("Audio " + audioName + " not found");
            return;
        }

        a.source.Play();
        // if (!playOnce)
        //     a.source.Play();
        // else if(!a.source.isPlaying)   // TO AVOID AUDIO BUG 
        //     a.source.Play();    // a.source.PlayOneShot(a.clip); 
    }

    // MINE 
    public void Stop(AudioClipsNames audioName)
    {
        Audio a = Array.Find(audios, audio => audio.name == audioName);    // Search by name
        if (a == null)
            return;
        a.source.Stop();
    }
}
