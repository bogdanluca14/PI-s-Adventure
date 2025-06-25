using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelHandler : MonoBehaviour
{
    // Variabile Globale

    // Obstacole si Stele
    public List<GameObject> obstacles;
    public List<GameObject> stars;
    public List<int> starsPopped;

    // Informatii referitoare la nivel
    public string restartTag = "Player";
    public LevelsManager crt = null;
    public TextMeshProUGUI crtTMP;
    public TextMeshProUGUI allTMP;

    // Referinte in Scena
    public PlayerLauncher playerLauncher;
    public Statistics stats;

    // Gestionam un nou nivel
    public void NewLevel(LevelsManager lvl)
    {
        LineManager lineManager = playerLauncher.lineManager;

        if (crt == null)
        {
            lineManager.NewLevel();
        }

        crt = lvl;

        foreach (LevelPrefab data in lvl.obstacles)
        {
            GameObject obstacle = Instantiate(data.prefab);

            obstacle.transform.position = data.pos;
            obstacle.transform.eulerAngles = data.rot;
            obstacle.transform.localScale = data.scale;

            if (data.sprite != null)
                obstacle.GetComponent<SpriteRenderer>().sprite = data.sprite;

            obstacles.Add(obstacle);
        }

        foreach (LevelPrefab data in lvl.stars)
        {
            GameObject star = Instantiate(data.prefab);

            star.transform.position = data.pos;
            star.transform.eulerAngles = data.rot;
            star.transform.localScale = data.scale;

            if (data.sprite != null)
                star.GetComponent<SpriteRenderer>().sprite = data.sprite;

            stars.Add(star);
        }

        // Modificam numarul de stele
        UpdateStarCount();

        if (
            PlayerLauncher.ind < 0
            || PlayerLauncher.ind > SaveSystem.saveData.lastLevel
            || (
                PlayerLauncher.ind == SaveSystem.saveData.lastLevel
                && SaveSystem.saveData.efficiency.Count <= PlayerLauncher.ind
            )
        )
            for (int i = 0; i < lvl.nrFctAllowed; i++)
                lineManager.OnSave();
        else
            foreach (var graph in SaveSystem.saveData.graphDatas[PlayerLauncher.ind].graphDatas)
                lineManager.OnSave(graph);
    }

    // Modificam numarul total de stele
    public void UpdateStarCount()
    {
        if (allTMP == null)
            return;
        allTMP.text = stars.Count.ToString();
    }

    // Modificam numarul de stele colectate
    public void UpdateStars()
    {
        if (crtTMP == null)
            return;
        crtTMP.text = starsPopped.Count.ToString();
    }

    // Restam nivelul daca este nevoie
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(restartTag))
            return;

        ResetLevel();
    }

    // Gestionam resetarea nivelului curent
    public void ResetLevel()
    {
        playerLauncher.enabled = false;

        playerLauncher.rb.velocity = Vector2.zero;
        playerLauncher.rb.angularVelocity = 0f;

        playerLauncher.rb.gravityScale = 0f;
        playerLauncher.rb.drag = playerLauncher.airDrag;
        playerLauncher.rb.angularDrag = playerLauncher.groundAngularDrag;

        if (playerLauncher.levelEnding)
            playerLauncher.GetComponent<SpriteRenderer>().enabled = false;
        else
            ResetPlayerPos();

        playerLauncher.resetBtn.interactable = false;
        playerLauncher.launched = false;

        foreach (var ec in FindObjectsOfType<EdgeCollider2D>())
            if (ec.gameObject.name.StartsWith("Functia"))
                ec.enabled = false;

        playerLauncher.plotter.DisableAllGraphPhysicsColliders();
        playerLauncher.enabled = true;

        starsPopped = new List<int>();
        LineManager lineManager = playerLauncher.lineManager;

        foreach (var star in stars)
        {
            star.GetComponent<StarCollect>().animator.enabled = false;
            star.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        }

        if (CustomLevelHandler.instance != null)
            CustomLevelHandler.instance.LevelStopped();

        UpdateStars();
    }

    // Resetam pozitia utilizatorului
    public void ResetPlayerPos()
    {
        playerLauncher.transform.position = playerLauncher.startPos;
        playerLauncher.transform.rotation = Quaternion.identity;
        playerLauncher.GetComponent<SpriteRenderer>().enabled = true;
        playerLauncher.ChangeSprite();

        if (PlayerLauncher.ind >= 0 || !playerLauncher.inTutorial)
            playerLauncher.EnableImportantBtns();
        else if (playerLauncher.inTutorial)
        {
            playerLauncher.animH.ContinueSequence();
            playerLauncher.inTutorial = false;
        }
    }
}
