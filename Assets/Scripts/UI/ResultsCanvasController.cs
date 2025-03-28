using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResultsCanvasController : MonoBehaviour
{
    [SerializeField] private GameObject percentageBarPrefab;
    [SerializeField]private TMPro.TextMeshProUGUI resultsText;
    [SerializeField]private Transform percentageBarParent;
    [SerializeField] private DistractionAlert distractionAlert;
    [SerializeField] private bool showObjectPercentages = true;

    private ServerResponseMinimized serverResponseMinimized;
    private Dictionary<string, PercentageBar> objPercentageBar;


    public void Initialize(List<GameObject> possibleGazeObj)
    {
        objPercentageBar = new();
        foreach (var gazeObj in possibleGazeObj)
        {
            var newPercentageBar = GameObject.Instantiate(percentageBarPrefab, percentageBarParent).GetComponent<PercentageBar>();
            var gazeObjName = gazeObj.name;
            objPercentageBar.Add(gazeObjName, newPercentageBar);
            newPercentageBar.SetPercentage(0, gazeObjName);

        }
        SetActivePercentageBars();
    }

    public void SetResultsByServer(ServerResponseMinimized response)
    {
        serverResponseMinimized = response;
        OnResponseRecived();

    }


    private void SetActivePercentageBars()
    {
        if (showObjectPercentages)
        {
            percentageBarParent.gameObject.SetActive(true);
            return;
        }
        percentageBarParent.gameObject.SetActive(false);
    }

    private void OnResponseRecived()
    {
        resultsText.text = FormatText();
        DistractionAlert(serverResponseMinimized.Distraction_Detected);
        UpdatePercentages();


    }
    private string FormatText()
    {
        string formattedText =
            $"Fixation Ratio: {serverResponseMinimized.Fixation_Ratio.ToString("F3")}\n" +
            $"Saccade Ratio: {serverResponseMinimized.Saccade_Ratio.ToString("F3")}\n" +
            //$"Distraction Detected: {string.Join(", ", serverResponseMinimized.DistractionDetected)}\n" +
            $"Is CognitiveOverload: {string.Join(", ", serverResponseMinimized.Cognitive_Overload)}\n";
        return formattedText;
    }

    private void UpdatePercentages()
    {
        foreach(var key in objPercentageBar.Keys)
        {
            float percentage;
            if(serverResponseMinimized.Gaze_Object_Percentages.TryGetValue(key, out percentage))
            {
                objPercentageBar[key].SetPercentage(percentage, key);
            }
        }
        SetActivePercentageBars();
    }
    private void DistractionAlert(bool isActive)
    {
        if (isActive)
        {
            distractionAlert.gameObject.SetActive(true);
        }
        else
        {
            distractionAlert.gameObject.SetActive(false);
        }
    }
}
