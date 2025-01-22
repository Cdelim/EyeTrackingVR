using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class ServerCommunicationManager : MonoBehaviour
{

    public static ServerCommunicationManager Instance;
    [System.Serializable]
    public class CalculationData
    {
        public float value1;
        public float value2;
    }

    [System.Serializable]
    public class CalculationResult
    {
        public float result;
    }


    private Coroutine sendRequestCr;
    //private Queue<Coroutine> sendRequestQueue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
        SendDataToServer(10, 20);
    }

    private IEnumerator SendDataToServerCr(float value1, float value2)
    {
        // Prepare data
        CalculationData data = new CalculationData { value1 = value1, value2 = value2 };
        string jsonData = JsonUtility.ToJson(data);

        // Send POST request
        UnityWebRequest request = new UnityWebRequest("http://127.0.0.1:5000/calculate", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Parse the response
            string responseText = request.downloadHandler.text;
            CalculationResult result = JsonUtility.FromJson<CalculationResult>(responseText);
            Debug.Log("Result from server: " + result.result);
        }
        else
        {
            Debug.LogError("Server error: " + request.error);
        }
    }



    public void SendDataToServer(float value1, float value2)
    {
        if (sendRequestCr != null)
        {
            StopCoroutine(sendRequestCr);
        }
        sendRequestCr = StartCoroutine(SendDataToServerCr(value1, value2));
    }

}
