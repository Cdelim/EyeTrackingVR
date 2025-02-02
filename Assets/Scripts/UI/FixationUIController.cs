using UnityEngine;
using UnityEngine.UI;

public class FixationUIController : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI text;


    public void SetText(string txt)
    {
        text.text = txt;
    }

    public void EnableText()
    {

        text.gameObject.SetActive(true);
    }

    public void DisableText()
    {
        text.gameObject.SetActive( false);
    }
}




