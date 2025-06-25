using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Options : MonoBehaviour
{
    // Variabile Globale

    // Referinte Globale si starea nivelului
    public Button optionsBtn;
    public CameraController cameraController;
    public CalcHandler calcHandler;
    public Slider music;
    public Slider sound;
    public PlayerLauncher player;
    public Animator panelAnimator;
    public static bool loadStory = true;

    // Referinte Locale si informatii despre meniu
    private Animator animator;
    private const string OPEN_TRIGGER = "OptionsOpen";
    private const string CLOSE_TRIGGER = "OptionsClose";
    private bool isOpen = false;

    // Assignul referintelor
    private void Start()
    {
        animator = GetComponent<Animator>();
        animator.enabled = false;
        SoundMixerManager soundMixer = AudioManager.instance.GetComponent<SoundMixerManager>();

        if (music != null)
            music.value = soundMixer.GetMusicVolume();
        if (sound != null)
            sound.value = soundMixer.GetSoundVolume();
    }

    // La finalizarea aventurii
    public void GameEnd()
    {
        panelAnimator.enabled = true;
        panelAnimator.Play("StoryPanel", 0, 0f);
        StartCoroutine(ChangeScene("Ending"));
    }

    // La apasarea butonului "Niveluri"
    public void LevelsBtn()
    {
        player.plotter.EndEdit();

        if (PlayerLauncher.ind < 0)
            SaveSystem.FirstLaunched();
        else if (!player.inCustom)
            SaveSystem.SaveLevel(player.lineManager.GetGraphDatas(), 0, PlayerLauncher.ind);

        panelAnimator.enabled = true;
        panelAnimator.Play("StoryPanel", 0, 0f);

        StartCoroutine(ChangeScene("MainMenu"));
    }

    // La apasarea butonului "Inapoi"
    public void BackBtn()
    {
        if (!isOpen)
            return;

        animator.enabled = true;
        animator.Play(CLOSE_TRIGGER, 0, 0f);

        if (cameraController != null)
            cameraController.StopBlur();

        StartCoroutine(EnableButtons(CLOSE_TRIGGER));
        isOpen = false;
    }

    // La apasarea butonului "Iesire"
    public void ExitBtn()
    {
        Application.Quit();
    }

    // La iesirea aplicatiei
    private void OnApplicationQuit()
    {
        if (player == null)
            return;
        player.plotter.EndEdit();

        List<FunctionPlotter.GraphData> graphs = player.lineManager.GetGraphDatas();
        if (PlayerLauncher.ind >= 0 && !player.inCustom)
            SaveSystem.SaveLevel(graphs, 0, PlayerLauncher.ind);
    }

    // La deschiderea meniului de optiuni
    public void OpenOptions()
    {
        animator.enabled = true;

        animator.Play(OPEN_TRIGGER, 0, 0f);
        if (
            cameraController != null
            && cameraController.GetComponent<PostProcessVolume>().weight != 1
        )
            cameraController.StartBlur();

        if (calcHandler != null)
            calcHandler.OnForceClose();
        if (player != null)
            player.DisableImportantBtns(true);
        else
            optionsBtn.interactable = false;
        isOpen = true;
    }

    // Activarea butoanelor la finalul animatiei
    IEnumerator EnableButtons(string state)
    {
        yield return AnimationHandler.WaitForStateEnd(animator, state);

        if (player != null)
            player.EnableImportantBtns();
        else
            optionsBtn.interactable = true;
    }

    // Schimbarea Scenei la finalul animatiei
    IEnumerator ChangeScene(string sceneName)
    {
        yield return AnimationHandler.WaitForStateEnd(panelAnimator, "StoryPanel");

        SceneManager.LoadScene(sceneName);
    }
}
