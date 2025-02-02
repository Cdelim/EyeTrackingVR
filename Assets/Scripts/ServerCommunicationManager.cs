using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System;
using UnityEditor.PackageManager;

public class ServerCommunicationManager : MonoBehaviour
{

    public static ServerCommunicationManager Instance;
    [SerializeField] private ResultsCanvasController resultsCanvasController;



    private string serverUrl = "http://127.0.0.1:5000/CalculateValue";
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


    public void SendFrameBufferToServer(FrameBuffer frameBuffer)
    {
        string jsonData = frameBuffer.SerializeToJson();  // Get the serialized JSON string

        // Start the coroutine to send the data
        StartCoroutine(SendDataToServer(jsonData));
    }

    // Coroutine to send data to the server
    private IEnumerator SendDataToServer(string jsonData)
    {
        // Create a UnityWebRequest with a POST method
        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(serverUrl, jsonData))
        {
            // Set the content type to "application/json"
            www.SetRequestHeader("Content-Type", "application/json");

            // Convert the JSON string to a byte array and send it
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();

            // Wait for the response from the server
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Data sent successfully to Python server");
            }
            else
            {
                Debug.Log("Error sending data to server: " + www.error);
            }
        }
    }

    /*private IEnumerator SendDataToServerCr(FrameBuffer buffer)
    {
        // Prepare data
        string jsonData = JsonUtility.ToJson(buffer);

        // Send POST request
        UnityWebRequest request = new UnityWebRequest("http://127.0.0.1:5000/CalculateValue", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Parse the response
            string responseText = request.downloadHandler.text;
            //int result = JsonUtility.FromJson<CalculationResult>(responseText);
            //Debug.Log("Result from server: " + result.result);
        }
        else
        {
            Debug.LogError("Server error: " + request.error);
        }
    }



    public void SendDataToServer(FrameBuffer buffer)
    {
        if (sendRequestCr != null)
        {
            StopCoroutine(sendRequestCr);
        }
        sendRequestCr = StartCoroutine(SendDataToServerCr(buffer));
    }*/


  
}
