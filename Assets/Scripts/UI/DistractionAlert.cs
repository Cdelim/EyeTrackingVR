using UnityEngine;

public class DistractionAlert : MonoBehaviour
{
    [SerializeField]private TMPro.TextMeshProUGUI text;
    private float scaleTime = .5f;
    private Vector3 targetScale;
    private Vector3 initialScale;
    private float timer;

    private void Awake()
    {
        initialScale = transform.localScale;
        targetScale = transform.localScale * 1.25f;
    }
    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, timer / scaleTime);
        timer += Time.deltaTime;

        if (timer >= scaleTime)
        {
            timer = 0f;
            targetScale = (initialScale == targetScale) ? initialScale * 1.25f: initialScale ;
        }
    }
}
