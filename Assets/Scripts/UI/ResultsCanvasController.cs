using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResultsCanvasController : MonoBehaviour
{
    [SerializeField] private GameObject percentageBarPrefab;
    [SerializeField]private TMPro.TextMeshProUGUI resultsText;

    private ServerResponseMinimized serverResponseMinimized;
    private Dictionary<string, PercentageBar> objPercentageBar;


    public void Initialize(List<GameObject> possibleGazeObj)
    {
        objPercentageBar = new();
        foreach (var gazeObj in possibleGazeObj)
        {
            var newPercentageBar = GameObject.Instantiate(percentageBarPrefab, transform).GetComponent<PercentageBar>();
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
    }
    private string FormatText()
    {
        string formattedText =
            $"Fixation Ratio: {serverResponseMinimized.FixationRatio}" +
            $"Saccade Ratio: {serverResponseMinimized.SaccadeRatio}\n" +
            $"Distraction Detected: {string.Join(", ", serverResponseMinimized.DistractionDetected)}\n" +
            $"Is CognitiveOverload: {string.Join(", ", serverResponseMinimized.CognitiveOverload)}\n";
        return formattedText;
    }

    private void UpdatePercentages()
    {

    }
    private void DistractionAlert()
    {

    }
}

public class PercentageBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private TMPro.TextMeshProUGUI percentageText;
    [SerializeField] private TMPro.TextMeshProUGUI gameObjectName;

    private float percentage;

    public void SetPercentage(float val, string gameObjName)
    {
        percentage = val;
        fillImage.fillAmount = percentage * .01f;
        percentageText.text = percentage.ToString();
        gameObjectName.text = gameObjName;

    }

}
