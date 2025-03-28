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

using Newtonsoft.Json;

public class ServerCommunicationManager : MonoBehaviour
{

    public string userId = "user_1";
    [SerializeField] private ResultsCanvasController resultsCanvasController;
    [SerializeField] private TaskSettingsController taskSettingsController;
    private string filePath;
    private string fileName = "GazeData.csv";
    private int filePostfix = 1;



    private string serverURL = "http://127.0.0.1:5000";
    private Coroutine sendRequestCr;
    private ServerResponseMinimized lastServerResponse;
    //private Queue<Coroutine> sendRequestQueue;

    private void Awake()
    {
       
        StartCoroutine(StartSession());

    }

    // Start the session by sending a POST request to /start_session
    IEnumerator StartSession()
    {
        WWWForm form = new WWWForm();
        form.AddField("user_id", userId);

        using (UnityWebRequest www = UnityWebRequest.Post(serverURL + "/start_session", form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Session started: " + www.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Session start failed: " + www.error);
            }
        }
    }

    public void SendCSVBufferToServer(FrameBuffer gazeDataLines)
    {
        filePath = Path.Combine(Application.persistentDataPath, fileName + filePostfix);
        filePostfix++;
        File.WriteAllLines(filePath, gazeDataLines.GetLastFrames());
        // Start the coroutine to send the data
        StartCoroutine(SendCSVToServer(filePath));
    }

    IEnumerator SendCSVToServer(string filePath)
    {
        byte[] fileData = File.ReadAllBytes(filePath);
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, fileName, "text/csv");

        using (UnityWebRequest www = UnityWebRequest.Post(serverURL + "/upload", form))
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

    private void ProcessServerResponse(string jsonResponse)
    {
        lastServerResponse = JsonConvert.DeserializeObject<ServerResponseMinimized>(jsonResponse);
        resultsCanvasController.SetResultsByServer(lastServerResponse);
        taskSettingsController.OnServerResponse(lastServerResponse);
    }

   /* void ProcessServerResponse(string jsonResponse)
    {
        lastServerResponse = JsonUtility.FromJson<ServerResponse>(jsonResponse);

        // Format the eye movement statistics into a readable string
        string eyeMovementStats = FormatEyeMovementStatistics(lastServerResponse);

        // Format the full response text
        string formattedText =
            $"Total Duration: {lastServerResponse.total_duration_minutes} min\n" +
            $"Average FPS: {lastServerResponse.average_fps}\n\n" +
            $"Columns: {string.Join(", ", lastServerResponse.column_names)}\n\n" +
            $"Gazed Objects: {string.Join(", ", lastServerResponse.unique_gazed_objects)}\n\n" +
            $"Head & Gaze Data: {lastServerResponse.head_and_gaze_df}\n\n" +
            $"Eye Movement Statistics:\n{eyeMovementStats}\n\n" +
            $"Combined Data: {lastServerResponse.combined_df}\n\n" +
            $"Pupil Data: {lastServerResponse.pupil_data}";

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
    }*/


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

[System.Serializable]
public class ServerResponseMinimized
{
    public float Fixation_Ratio;
    public float Saccade_Ratio;
    public bool Distraction_Detected;
    public bool Cognitive_Overload;
    public Dictionary<string, float> Gaze_Object_Percentages = new Dictionary<string, float>();
}
