using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    // Variabile Globale

    // Referinte Globale si informatii utile

    public List<Button> buttons;
    public AnimationHandler animationHandler;
    public Sprite lockedImage;
    public Animator panelAnimator;
    public Animator mainAnimator;
    public Camera cameraT;

    public TextMeshPro lvlText;
    public TextMeshPro fctText;
    public TextMeshPro infoText;

    public Vector3 selectorPos;
    public float selectorSize;
    public int crt;

    [SerializeField]
    public List<ExpressionBuilder.BuilderState> builder;
    public List<LevelsManager> levels;

    private int clickCount = 0;

    // Pornirea sistemelor de baza
    private void Start()
    {
        if (Options.loadStory)
            SaveSystem.LoadLevel();

        if (SaveSystem.saveData.firstLaunch)
        {
            animationHandler.nextSceneName = "Tutorial";
            animationHandler.startButton.onClick.AddListener(() =>
                animationHandler.OnStartPressed(true)
            );

            PlayerLauncher.ind = -1;
            Options.loadStory = false;

            return;
        }

        animationHandler.startButton.onClick.AddListener(() =>
            animationHandler.OnStartPressed(false)
        );

        crt = SaveSystem.saveData.lastLevel + 1;

        for (int i = 0; i < SaveSystem.saveData.efficiency.Count; i++)
        {
            int eff = SaveSystem.saveData.efficiency[i];
            if (eff > 0)
            {
                TextMeshPro tmp = buttons[i].GetComponentInChildren<TextMeshPro>();
                tmp.text = eff.ToString();
                tmp.color = Statistics.GetGradientColor(eff);
            }
        }

        for (int i = crt; i < buttons.Count; i++)
        {
            buttons[i].GetComponent<Image>().sprite = lockedImage;
            buttons[i].transform.GetChild(0).gameObject.SetActive(false);
            buttons[i].interactable = false;
        }

        if (Options.loadStory)
            Options.loadStory = false;
        else
            GoToSelector();
    }

    // Mutarea camerei catre meniul principal
    void GoToSelector()
    {
        animationHandler.animations.RemoveAt(0);
        animationHandler.animations.RemoveAt(0);

        animationHandler.OnStartPressed();

        cameraT.transform.position = selectorPos;
        cameraT.orthographicSize = selectorSize;

        panelAnimator.enabled = true;
        panelAnimator.Play("LevelPanel", 0, 0f);
    }

    // Selectarea unui anumit nivel din meniul principal
    public void SelectLevel(int nr)
    {
        mainAnimator.enabled = true;
        PlayerLauncher.ind = nr;

        lvlText.text = (nr + 1).ToString();
        fctText.text = levels[nr].nrFctAllowed.ToString();
        infoText.text = levels[nr].details;
    }

    // Apasarea butonului secret ce deblocheaza toate nivelurile
    public void OnSecretClicked()
    {
        ++clickCount;
        if (clickCount >= 7)
            LoadFullGame();
    }

    // Incarcarea salvarii cu progresul maxim
    private void LoadFullGame()
    {
        SaveSystem.LoadLevel(Resources.Load<TextAsset>("saveFull"));
        SceneManager.LoadScene("MainMenu");
        SaveSystem.SaveLevel();
    }

    // Intrarea in meniul de testare al nivelului personalizat
    public void TestCustom()
    {
        animationHandler.nextSceneName = "CustomLevelLoad";
        animationHandler.OnStartPressed(true);
    }

    // Intrarea in meniul de creare al nivelului personalizat
    public void CreateCustom()
    {
        animationHandler.nextSceneName = "CustomLevelSave";
        animationHandler.OnStartPressed(true);
    }
}
