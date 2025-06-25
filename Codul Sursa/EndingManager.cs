using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class EndingManager : MonoBehaviour
{
    // Variabile Globale

    // Referinte Globale
    public TextMeshProUGUI nameTMP;
    public TextMeshProUGUI effTMP;
    public TMP_InputField inputField;
    public GameObject confettiL;
    public GameObject confettiR;

    // Finalizarea aventurii
    public void ContinueEnding()
    {
        float sum = 0f;
        inputField.interactable = false;
        nameTMP.text = inputField.text;

        if (SaveSystem.saveData == null)
            SaveSystem.LoadLevel();
        foreach (int eff in SaveSystem.saveData.efficiency)
            sum += eff;

        sum = (float)sum / SaveSystem.saveData.efficiency.Count;
        sum = Mathf.Round(sum) / 10f;

        effTMP.text = string.Format("{0:#.00}", sum);
    }

    // Inceperea confettiului
    public void StartConfetti()
    {
        confettiL.SetActive(false);
        confettiR.SetActive(false);
        confettiL.SetActive(true);
        confettiR.SetActive(true);
        AudioManager.instance.PlaySound("finishLevel");
    }
}
