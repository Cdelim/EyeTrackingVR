using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResultsCanvasController : MonoBehaviour
{
    [SerializeField]private TMPro.TextMeshProUGUI resultsText;

    public void SetResultsText(string txt)
    {
        resultsText.text = txt;
    }
}
