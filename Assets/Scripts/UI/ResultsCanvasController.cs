using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResultsCanvasController : MonoBehaviour
{
    [SerializeField] private GameObject percentageBarPrefab;
    [SerializeField]private TMPro.TextMeshProUGUI resultsText;
    [SerializeField]private Transform percentageBarParent;
    [SerializeField] private DistractionAlert distractionAlert;

    private ServerResponseMinimized serverResponseMinimized;
    private Dictionary<string, PercentageBar> objPercentageBar;


    public void Initialize(List<GameObject> possibleGazeObj)
    {
        objPercentageBar = new();
        foreach (var gazeObj in possibleGazeObj)
        {
            var newPercentageBar = GameObject.Instantiate(percentageBarPrefab, percentageBarParent).GetComponent<PercentageBar>();
            var gazeObjName = gazeObj.ToString();
            objPercentageBar.Add(gazeObjName, newPercentageBar);
            newPercentageBar.SetPercentage(0, gazeObjName);

        }
    }

    public void SetResultsByServer(ServerResponseMinimized response)
    {
        serverResponseMinimized = response;
        OnResponseRecived();

    }




    private void OnResponseRecived()
    {
        resultsText.text = FormatText();
        DistractionAlert(serverResponseMinimized.DistractionDetected);
        UpdatePercentages();


    }
    private string FormatText()
    {
        string formattedText =
            $"Fixation Ratio: {serverResponseMinimized.FixationRatio}" +
            $"Saccade Ratio: {serverResponseMinimized.SaccadeRatio}\n" +
            //$"Distraction Detected: {string.Join(", ", serverResponseMinimized.DistractionDetected)}\n" +
            $"Is CognitiveOverload: {string.Join(", ", serverResponseMinimized.CognitiveOverload)}\n";
        return formattedText;
    }

    private void UpdatePercentages()
    {
        foreach(var key in objPercentageBar.Keys)
        {
            objPercentageBar[key].SetPercentage(serverResponseMinimized.Gaze_Object_Percentages[key], key);
        }
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
