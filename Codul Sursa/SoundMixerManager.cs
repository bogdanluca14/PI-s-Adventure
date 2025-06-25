using UnityEngine;
using UnityEngine.Audio;

public class SoundMixerManager : MonoBehaviour
{
    // Instanta pentru mixerul audio
    public AudioMixer audioMixer;

    public void SetSoundVolume(float level)
    {
        audioMixer.SetFloat("SoundVolume", Mathf.Log10(level) * 20);
    }

    public void SetMusicVolume(float level)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(level) * 20);
    }

    public float GetSoundVolume()
    {
        audioMixer.GetFloat("SoundVolume", out float volume);
        return Mathf.Pow(10f, volume / 20f);
    }

    public float GetMusicVolume()
    {
        audioMixer.GetFloat("MusicVolume", out float volume);
        return Mathf.Pow(10f, volume / 20f);
    }
}
