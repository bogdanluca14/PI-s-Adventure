using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Varibile Globale

    // Referinte Globale
    public static AudioManager instance;
    public AudioSource audioObject;
    public AudioClip obsHit;
    public AudioClip othHit;
    public AudioClip lvlFinish;
    public AudioClip buttonClick;
    public AudioClip coinCollect;

    // Variabile Locale

    private GameObject lastAudio;

    // Assignul referinte
    private void Awake()
    {
        if (instance == null)
            instance = this;

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    // Porneste clip audio
    public void PlayAudioClip(AudioClip clip)
    {
        if (lastAudio != null)
            Destroy(lastAudio);

        AudioSource audioSource = Instantiate(audioObject, Vector3.zero, Quaternion.identity);
        lastAudio = audioSource.gameObject;

        audioSource.clip = clip;
        audioSource.volume = 0.7f;
        audioSource.Play();
        float clipLength = audioSource.clip.length;

        try
        {
            Destroy(audioSource.gameObject, clipLength);
        }
        catch { }
    }

    // Porneste sunet dupa nume
    public void PlaySound(string input, float volume = 0.7f)
    {
        AudioClip crt = null;
        switch (input)
        {
            case "hitGraph":
                crt = othHit;
                break;
            case "hitObstacle":
                crt = obsHit;
                break;
            case "finishLevel":
                crt = lvlFinish;
                break;
            case "buttonClick":
                crt = buttonClick;
                break;
            case "coinCollect":
                crt = coinCollect;
                break;
        }

        AudioSource audioSource = Instantiate(audioObject, Vector3.zero, Quaternion.identity);
        audioSource.clip = crt;
        audioSource.volume = volume;
        audioSource.Play();

        float clipLength = audioSource.clip.length;
        Destroy(audioSource.gameObject, clipLength);
    }
}
