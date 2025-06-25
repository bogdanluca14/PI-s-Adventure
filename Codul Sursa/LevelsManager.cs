using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct LevelPrefab
{
    public GameObject prefab;
    public Sprite sprite;
    public Vector3 pos;
    public Vector3 rot;
    public Vector3 scale;
}

[System.Serializable]
public struct Solution
{
    public float minX;
    public float maxX;
    public string function;
}

[CreateAssetMenu(fileName = "Level", menuName = "ScriptableObjects/Level", order = 1)]
public class LevelsManager : ScriptableObject
{
    // Informatii referitoare la niveluri
    public List<LevelPrefab> obstacles;
    public List<LevelPrefab> stars;
    public List<string> disabledBtns;
    public List<Solution> solutions;
    public int nrFctAllowed;
    public string details;
}
