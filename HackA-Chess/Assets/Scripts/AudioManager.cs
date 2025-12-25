using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public Slider VolumeSlider;
    public TMP_Dropdown listMusic;
    public Sound[] musicSounds;
    public AudioSource musicSource;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        listMusic.ClearOptions();
        List<string> names = new List<string>();
        foreach (var s in musicSounds)
            names.Add(s.name);
        listMusic.AddOptions(names);
        listMusic.onValueChanged.AddListener(OnMusicChanged);
        if (musicSounds.Length > 0)
            PlayMusic(musicSounds[0].name);

        if (VolumeSlider != null)
        {
            VolumeSlider.value = musicSource.volume;
            VolumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (musicSounds.Length > 0)
            PlayMusic(musicSounds[0].name);
    }

    public void PlayMusic(string name)
    {
        Sound s = Array.Find(musicSounds, x => x.name == name);
        if (s == null)
        {
            Debug.Log("Sound not found: " + name);
        }
        else
        {
            musicSource.clip = s.clip;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void OnMusicChanged(int index)
    {
        PlayMusic(musicSounds[index].name);
    }

    public void SetVolume(float value)
    {
        musicSource.volume = value;
    }
    public void StopBGM()
    {
        musicSource.Stop();
    }
}

