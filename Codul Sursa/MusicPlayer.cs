using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    // Instanta scriptului de muzica
    public static MusicPlayer Instance;

    // Ne asiguram sa avem exact o instanta in Scena
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
