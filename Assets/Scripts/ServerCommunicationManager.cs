using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System;
using UnityEditor.PackageManager;
using System.IO;

public class ServerCommunicationManager : MonoBehaviour
{

    public static ServerCommunicationManager Instance;
    [SerializeField] private ResultsCanvasController resultsCanvasController;
    private string filePath;



    private string serverURL = "http://127.0.0.1:5000/upload";
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
        filePath = Path.Combine(Application.persistentDataPath, "GazeData.csv");

    }

    /// <summary>
    /// Send as line of dict
    /// </summary>
    /// <param name="frameBuffer"></param>
    /*public void SendFrameBufferToServer(FrameBuffer frameBuffer)
    {
        string jsonData = frameBuffer.SerializeToJson();  // Get the serialized JSON string

        // Start the coroutine to send the data
        StartCoroutine(SendDataToServer(jsonData));
    }*/

    // Coroutine to send data to the server
    private IEnumerator SendDataToServer(string jsonData)
    {
        // Create a UnityWebRequest with a POST method
        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(serverURL, jsonData))
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

    public void SendCSVBufferToServer(FrameBuffer gazeDataLines)
    {
        File.WriteAllLines(filePath, gazeDataLines.GetLastFrames());
        // Start the coroutine to send the data
        StartCoroutine(SendCSVToServer(filePath));
    }

    IEnumerator SendCSVToServer(string filePath)
    {
        byte[] fileData = File.ReadAllBytes(filePath);
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, "GazeData.csv", "text/csv");

        using (UnityWebRequest www = UnityWebRequest.Post(serverURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error: " + www.error);
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                Debug.Log("Response from server: " + jsonResponse);
                ProcessServerResponse(jsonResponse);
            }
        }
    }

    void ProcessServerResponse(string jsonResponse)
    {
        ServerResponse response = JsonUtility.FromJson<ServerResponse>(jsonResponse);

        // Format the eye movement statistics into a readable string
        string eyeMovementStats = FormatEyeMovementStatistics(response);

        // Format the full response text
        string formattedText =
            $"Total Duration: {response.total_duration_minutes} min\n" +
            $"Average FPS: {response.average_fps}\n\n" +
            $"Columns: {string.Join(", ", response.column_names)}\n\n" +
            $"Gazed Objects: {string.Join(", ", response.unique_gazed_objects)}\n\n" +
            $"Head & Gaze Data: {response.head_and_gaze_df}\n\n" +
            $"Eye Movement Statistics:\n{eyeMovementStats}\n\n" +
            $"Combined Data: {response.combined_df}\n\n" +
            $"Pupil Data: {response.pupil_data}";

        // Set the results text on the results canvas
        resultsCanvasController.SetResultsText(formattedText);
    }

    string FormatEyeMovementStatistics(ServerResponse response)
    {
        // Format the statistics for each eye movement category
        return $"Fixation: {FormatStatistics(response.fixation)}\n" +
               $"Saccade: {FormatStatistics(response.saccade)}\n" +
               $"Saccade Candidate: {FormatStatistics(response.saccade_candidate)}\n" +
               $"Other Saccades: {FormatStatistics(response.other_saccades)}\n" +
               $"Outlier: {FormatStatistics(response.outlier)}";
    }

    string FormatStatistics(EyeMovementStatistics stats)
    {
        // Format the individual statistics for each category
        return $"Min: {stats.min?.ToString("F3") ?? "N/A"}, " +
               $"Max: {stats.max?.ToString("F3") ?? "N/A"}, " +
               $"Mean: {stats.mean?.ToString("F3") ?? "N/A"}, " +
               $"Count: {stats.count}, " +
               $"Percentage: {stats.percentage:F2}%, " +
               $"Time: {stats.time:F3}s, " +
               $"Time Percentage: {stats.time_percentage:F2}%";
    }


}


[System.Serializable]
public class ServerResponse
{
    public float total_duration_minutes;
    public string[] column_names;
    public string first_rows;

    public string[] gazed_object_column;
    public string[] unique_gazed_objects;
    public Dictionary<string, string> gazed_object_ratios;
    public Dictionary<string, string> gazed_object_durations;
    public Dictionary<string, string> normalized_gazed_object_durations;

    public float average_fps;
    public string head_and_gaze_df;

    public EyeMovementStatistics fixation;
    public EyeMovementStatistics saccade;
    public EyeMovementStatistics saccade_candidate;
    public EyeMovementStatistics other_saccades;
    public EyeMovementStatistics outlier;

    public string eye_movement_df;
    public string eye_movement_dict;

    public string combined_df;
    public string pupil_data;
}

// Class to hold the statistics for different types of eye movements (fixation, saccade, etc.)
[System.Serializable]
public class EyeMovementStatistics
{
    public float? min;
    public float? max;
    public float? mean;
    public int count;
    public float percentage;
    public float time;
    public float time_percentage;
}


