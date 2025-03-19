using UnityEngine;
using UnityEngine.UI;

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
