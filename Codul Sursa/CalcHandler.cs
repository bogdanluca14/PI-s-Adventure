using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CalcHandler : MonoBehaviour
{
    // Variabile Globale

    // Referinte globale si informatii utile
    public PlayerLauncher player;
    public Animator calculatorAnimator;
    public Button calcBtn;
    public Button optionsBtn;
    public bool isOpen = false;

    // Variabile Locale

    private const string OPEN_TRIGGER = "OpenCalc";
    private const string CLOSE_TRIGGER = "CloseCalc";

    // Deschiderea/Inchiderea calculatorului
    public void OnToggleCalculator(bool tutorial = false)
    {
        if (!isOpen)
        {
            calculatorAnimator.enabled = true;
            calculatorAnimator.Play(OPEN_TRIGGER);

            calcBtn.interactable = false;
            optionsBtn.interactable = false;
            isOpen = true;

            StartCoroutine(EnableButton(OPEN_TRIGGER, tutorial));
        }
        else
        {
            calculatorAnimator.enabled = true;
            calculatorAnimator.Play(CLOSE_TRIGGER);

            calcBtn.interactable = false;
            optionsBtn.interactable = false;
            isOpen = false;

            StartCoroutine(EnableButton(CLOSE_TRIGGER, tutorial));
        }
        
    }

    // Inchiderea fortata a calculatorului
    public void OnForceClose()
    {
        if(isOpen)
        {
            calculatorAnimator.enabled = true;
            calculatorAnimator.Play(CLOSE_TRIGGER);

            isOpen = false;
        }
    }

    // Activarea butonului la finalul unei animatii specifice
    IEnumerator EnableButton(string state, bool tutorial = false)
    {
        yield return AnimationHandler.WaitForStateEnd(calculatorAnimator, state);

        if (!tutorial)
        {
            calcBtn.interactable = true;
            optionsBtn.interactable = true;
        }
        else if(player.inTutorial)
        {
            player.animH.ContinueSequence();
        }
    }
}
