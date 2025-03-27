using System.Collections.Generic;
using UnityEngine;
using Material = UnityEngine.Material;

public class TaskSettingsController : MonoBehaviour
{
    public float serverFrameBufferSize = 2400;
    public List<GameObject> gazeObjects;
    public GameObject targetObject;
    public Eyes eyes;
    public Transform head;
    public LayerMask gazeHitLayer;
    private ServerResponseMinimized serverResponseMinimized;

    private Renderer targetObjRenderer;
    private Color targetInitialColor;
    [SerializeField]private Color targetHighlightColor;
    private float timer = 0f;
    private float highlightDeltaTime = .5f;


    private bool isDistracted = false;

    private void Awake()
    {
        targetObjRenderer = targetObject.GetComponent<Renderer>();
        targetObjRenderer.material.EnableKeyword("_EMISSION");
        targetInitialColor = targetObjRenderer.material.GetColor("_EmissionColor");
    }
    public void SetGazeObjLayers()
    {

        foreach (var gazeObj in gazeObjects)
        {
            if (gazeHitLayer.value == 0)
            {
                Debug.LogWarning("Mask is empty. Assigning default layer.");
            }
            else
            {
                gazeObj.layer = Mathf.RoundToInt(Mathf.Log(gazeHitLayer.value, 2));
            }
        }
    }


    private void Update()
    {
        if (isDistracted)
        {
            HighLightTargetObj();

        }
        else
        {

            targetObjRenderer.material.SetColor("_EmissionColor", targetInitialColor);
            timer = 0f;
        }
    }

    public void OnServerResponse(ServerResponseMinimized serverResponse)
    {
        serverResponseMinimized = serverResponse;
        isDistracted = serverResponseMinimized.DistractionDetected;

    }


    private void HighLightTargetObj()
    {
        var temp = Color.Lerp(targetInitialColor, targetHighlightColor, timer / highlightDeltaTime);
        timer += Time.deltaTime;
        if (timer >= highlightDeltaTime)
        {
            temp = Color.Lerp(targetHighlightColor, targetInitialColor, timer / (2*highlightDeltaTime));
        }
        if(timer >= (2 * highlightDeltaTime))
        {
            timer = 0f;
        }
        targetObjRenderer.material.SetColor("_EmissionColor", temp);
    }

}





