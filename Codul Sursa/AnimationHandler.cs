using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public struct AnimationEntry
{
    public Animator animator;
    public string stateName;
    public UnityEvent onExit;
    public bool waitForInput;
}

public class AnimationHandler : MonoBehaviour
{
    // Variabile Globale

    // Informatii generale referitoare la animatii
    public bool inputReceived = false;
    public bool stopLast = false;
    public bool playOnStart = false;
    public string nextSceneName;

    // Referinte Globale
    public List<AnimationEntry> animations;
    public Button startButton;

    // Verificare daca incepe la inceputul Scenei
    void Start()
    {
        if (playOnStart)
        {
            OnStartPressed();
        }
    }

    // La apasarea butonului/inceputul Scenei
    public void OnStartPressed(bool onlyLast = false)
    {
        foreach (var entry in animations)
            if (entry.animator != null)
                entry.animator.enabled = false;

        if (startButton != null)
            startButton.interactable = false;

        if (onlyLast)
            StartCoroutine(PlayLastAnim());
        else
            StartCoroutine(PlayAllAndLoad());
    }

    // Continua secventa de animatii
    public void ContinueSequence()
    {
        inputReceived = true;
    }

    // Incepe animatiile propriu-zise
    IEnumerator PlayAllAndLoad()
    {
        foreach (var entry in animations)
        {
            if (entry.animator != null && !string.IsNullOrEmpty(entry.stateName))
            {
                if (entry.stateName == animations[animations.Count - 1].stateName && stopLast)
                    continue;

                entry.animator.enabled = true;
                entry.animator.Play(entry.stateName);

                yield return WaitForStateEnd(entry.animator, entry.stateName);
            }
            if (entry.waitForInput)
            {
                inputReceived = false;

                while (!inputReceived)
                    yield return null;
            }

            entry.onExit?.Invoke();
        }

        if (!stopLast && !string.IsNullOrWhiteSpace(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    // Porneste doar ultima animatie din secventa
    public IEnumerator PlayLastAnim()
    {
        StopCoroutine(PlayAllAndLoad());

        var entry = animations[animations.Count - 1];
        if (entry.animator == null || string.IsNullOrEmpty(entry.stateName))
            yield return null;

        entry.animator.enabled = true;
        entry.animator.Play(entry.stateName);

        yield return WaitForStateEnd(entry.animator, entry.stateName);

        entry.onExit?.Invoke();

        if (!string.IsNullOrWhiteSpace(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    // Asteapta sa se termine animatia respectiva
    public static IEnumerator WaitForStateEnd(Animator anim, string stateName)
    {
        while (!anim.GetCurrentAnimatorStateInfo(0).IsName(stateName))
            yield return null;

        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        while (info.IsName(stateName) && info.normalizedTime < 1f)
        {
            yield return null;
            info = anim.GetCurrentAnimatorStateInfo(0);
        }
    }
}
