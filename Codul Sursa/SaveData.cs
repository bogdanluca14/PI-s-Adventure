using System.Collections.Generic;

[System.Serializable]
public class GraphDataList
{
    public List<FunctionPlotter.GraphData> graphDatas;
}

[System.Serializable]
public class SaveData
{
    // Informatiile care vor fi salvate

    public List<GraphDataList> graphDatas;
    public List<int> efficiency;
    public int lastLevel;
    public bool firstLaunch;
}
