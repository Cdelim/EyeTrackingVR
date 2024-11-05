using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Varjo.XR;

using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.XR;
public enum GazeDataSource
{
    InputSubsystem,
    GazeAPI
}

public class EyeTrackingScript : MonoBehaviour
{
    //C, N, W, H
    [Tooltip("CW:1 CH:2 NW:3 NH:4")]
    public int condition;
    //0(menu scene, log 0), 1, 2, 3, 4
    public string scene = "";
    //0(no task), 1, 2,...
    public int task = 1;

    public string gazedObject = "";
    //"" or 1
    public string clickedObject = "";

    //a, b, c, d, e, or "" 
    public string quizAnswer = "";

    //1 or 0
    public int chatBot = 0;
    //Performance string
    public string performance;
    [Header("Gaze data")]
    public GazeDataSource gazeDataSource = GazeDataSource.InputSubsystem;

    [Header("Gaze calibration settings")]
    public VarjoEyeTracking.GazeCalibrationMode gazeCalibrationMode = VarjoEyeTracking.GazeCalibrationMode.Fast;
    public KeyCode calibrationRequestKey = KeyCode.Space;

    [Header("Gaze output filter settings")]
    public VarjoEyeTracking.GazeOutputFilterType gazeOutputFilterType = VarjoEyeTracking.GazeOutputFilterType.Standard;
    public KeyCode setOutputFilterTypeKey = KeyCode.RightShift;

    [Header("Gaze data output frequency")]
    public VarjoEyeTracking.GazeOutputFrequency frequency;

    [Header("Toggle gaze target visibility")]
    public KeyCode toggleGazeTarget = KeyCode.Return;

    [Header("Debug Gaze")]
    public KeyCode checkGazeAllowed = KeyCode.PageUp;
    public KeyCode checkGazeCalibrated = KeyCode.PageDown;

    [Header("Toggle fixation point indicator visibility")]
    public bool showFixationPoint = true;

    [Header("Visualization Transforms")]
    public Transform fixationPointTransform;
    public Transform leftEyeTransform;
    public Transform rightEyeTransform;

    [Header("XR camera")]
    public Camera xrCamera;

    [Header("Gaze point indicator")]
    public GameObject gazeTarget;

    [Header("Gaze ray radius")]
    public float gazeRadius = 0.01f;

    [Header("Gaze point distance if not hit anything")]
    public float floatingGazeTargetDistance = 5f;

    [Header("Gaze target offset towards viewer")]
    public float targetOffset = 0.2f;

    [Header("Amout of force give to freerotating objects at point where user is looking")]
    public float hitForce = 5f;

    [Header("Gaze data logging")]
    public KeyCode loggingToggleKey = KeyCode.RightControl;

    [Header("Default path is Logs under application data path.")]
    public bool useCustomLogPath = false;
    public string customLogPath = "";

    [Header("Print gaze data framerate while logging.")]
    public bool printFramerate = false;

    private List<InputDevice> devices = new List<InputDevice>();
    private InputDevice device;
    private Eyes eyes;
    private VarjoEyeTracking.GazeData gazeData;
    private List<VarjoEyeTracking.GazeData> dataSinceLastUpdate;
    private List<VarjoEyeTracking.EyeMeasurements> eyeMeasurementsSinceLastUpdate;
    private Vector3 leftEyePosition;
    private Vector3 rightEyePosition;
    private Quaternion leftEyeRotation;
    private Quaternion rightEyeRotation;
    private Vector3 fixationPoint;
    private Vector3 direction;
    private Vector3 rayOrigin;
    private RaycastHit hit;
    private float distance;
    private StreamWriter writer = null;
    private bool logging = false;

    private static readonly string[] ColumnNames = { "Frame", "TimeStamp", "LogTime",
        "HeadPositionX", "HeadPositionY", "HeadPositionZ", "HeadDirectionX", "HeadDirectionY", "HeadDirectionZ",
        "GazeStatus", "CombinedGazeForwardX", "CombinedGazeForwardY", "CombinedGazeForwardZ", "CombinedGazePositionX", "CombinedGazePositionY", "CombinedGazePositionZ", "InterPupillaryDistanceInMM",
        "LeftEyeStatus", "LeftGazeDirectionX","LeftGazeDirectionY","LeftGazeDirectionZ", "LeftEyePositionX", "LeftEyePositionY", "LeftEyePositionZ",
        "LeftPupilIrisDiameterRatio", "LeftPupilDiameterInMM", "LeftIrisDiameterInMM", "LeftEyeOpenness",
        "RightEyeStatus", "RightGazeDirectionX","RightGazeDirectionY", "RightGazeDirectionZ", "RightEyePositionX", "RightEyePositionY", "RightEyePositionZ",
        "RightPupilIrisDiameterRatio", "RightPupilDiameterInMM", "RightIrisDiameterInMM", "RightEyeOpenness",
        "FocusDistance", "FocusStability",
        "Condition", "Scene", "Task", "GazedObject", "ClickedObject", "QuizAnswer", "ChatBot"};

    ////C, N, W, H
    //public string condition = "";
    ////1,2,3,4,5
    //public string scene = "";
    ////1,2,3,4
    //public int task = 0;

    //public string gazedObject = "";
    ////"" or 1
    //public string clickedObject = "";

    //public string quizAnswer = "";
    ////1 or 0
    //public int chatBot = 0;

    private const string ValidString = "VALID";
    private const string InvalidString = "INVALID";

    int gazeDataCount = 0;
    float gazeTimer = 0f;
    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 40;
    }
    void GetDevice()
    {
        InputDevices.GetDevicesAtXRNode(XRNode.CenterEye, devices);
        device = devices.FirstOrDefault();
    }

    void OnEnable()
    {
        if (!device.isValid)
        {
            GetDevice();
        }
    }

    private void Start()
    {


        VarjoEyeTracking.SetGazeOutputFrequency(frequency);
        //Hiding the gazetarget if gaze is not available or if the gaze calibration is not done
        if (VarjoEyeTracking.IsGazeAllowed() && VarjoEyeTracking.IsGazeCalibrated())
        {
            gazeTarget.SetActive(true);
        }
        else
        {
            gazeTarget.SetActive(false);
        }

        if (showFixationPoint)
        {
            fixationPointTransform.gameObject.SetActive(true);
        }
        else
        {
            fixationPointTransform.gameObject.SetActive(false);
        }
        Scene scene = SceneManager.GetActiveScene();
        //if (scene.name == "2FruitMarket")
        StartLogging();
        logging = true;
    }

    void Update()
    {
        //https://docs.unity3d.com/ScriptReference/MonoBehaviour.FixedUpdate.html
        if (logging && printFramerate)
        {
            gazeTimer += Time.deltaTime;
            if (gazeTimer >= 1.0f)
            {
                // Debug.Log("Gaze data rows per second: " + gazeDataCount);
                gazeDataCount = 0;
                gazeTimer = 0f;
            }
        }

        // Request gaze calibration
        if (Input.GetKeyDown(calibrationRequestKey))
        {
            VarjoEyeTracking.RequestGazeCalibration(gazeCalibrationMode);
        }

        // Set output filter type
        if (Input.GetKeyDown(setOutputFilterTypeKey))
        {
            VarjoEyeTracking.SetGazeOutputFilterType(gazeOutputFilterType);
            Debug.Log("Gaze output filter type is now: " + VarjoEyeTracking.GetGazeOutputFilterType());
        }

        // Check if gaze is allowed
        if (Input.GetKeyDown(checkGazeAllowed))
        {
            Debug.Log("Gaze allowed: " + VarjoEyeTracking.IsGazeAllowed());
        }

        // Check if gaze is calibrated
        if (Input.GetKeyDown(checkGazeCalibrated))
        {
            Debug.Log("Gaze calibrated: " + VarjoEyeTracking.IsGazeCalibrated());
        }

        // Toggle gaze target visibility
        if (Input.GetKeyDown(toggleGazeTarget))
        {
            gazeTarget.GetComponentInChildren<MeshRenderer>().enabled = !gazeTarget.GetComponentInChildren<MeshRenderer>().enabled;
        }

        // Get gaze data if gaze is allowed and calibrated
        if (VarjoEyeTracking.IsGazeAllowed() && VarjoEyeTracking.IsGazeCalibrated())
        {

            //Get device if not valid
            if (!device.isValid)
            {
                GetDevice();
            }

            // Show gaze target
            gazeTarget.SetActive(true);

            if (gazeDataSource == GazeDataSource.InputSubsystem)
            {
               /*
                // Get data for eye positions, rotations and the fixation point
                if (device.TryGetFeatureValue(eyesFeature, out eyes))
                {
                    if (eyes.TryGetLeftEyePosition(out leftEyePosition))
                    {
                        leftEyeTransform.localPosition = leftEyePosition;
                    }

                    if (eyes.TryGetLeftEyeRotation(out leftEyeRotation))
                    {
                        leftEyeTransform.localRotation = leftEyeRotation;
                    }

                    if (eyes.TryGetRightEyePosition(out rightEyePosition))
                    {
                        rightEyeTransform.localPosition = rightEyePosition;
                    }

                    if (eyes.TryGetRightEyeRotation(out rightEyeRotation))
                    {
                        rightEyeTransform.localRotation = rightEyeRotation;
                    }

                    if (eyes.TryGetFixationPoint(out fixationPoint))
                    {
                        fixationPointTransform.localPosition = fixationPoint;
                    }
                }

                // Set raycast origin point to VR camera position
                rayOrigin = xrCamera.transform.position;

                // Direction from VR camera towards fixation point
                direction = (fixationPointTransform.position - xrCamera.transform.position).normalized;*/

            }
            else
            {
                gazeData = VarjoEyeTracking.GetGaze();

                if (gazeData.status != VarjoEyeTracking.GazeStatus.Invalid)
                {
                    // GazeRay vectors are relative to the HMD pose so they need to be transformed to world space
                    if (gazeData.leftStatus != VarjoEyeTracking.GazeEyeStatus.Invalid)
                    {
                        leftEyeTransform.position = xrCamera.transform.TransformPoint(gazeData.left.origin);
                        leftEyeTransform.rotation = Quaternion.LookRotation(xrCamera.transform.TransformDirection(gazeData.left.forward));
                    }

                    if (gazeData.rightStatus != VarjoEyeTracking.GazeEyeStatus.Invalid)
                    {
                        rightEyeTransform.position = xrCamera.transform.TransformPoint(gazeData.right.origin);
                        rightEyeTransform.rotation = Quaternion.LookRotation(xrCamera.transform.TransformDirection(gazeData.right.forward));
                    }

                    // Set gaze origin as raycast origin
                    rayOrigin = xrCamera.transform.TransformPoint(gazeData.gaze.origin);

                    // Set gaze direction as raycast direction
                    direction = xrCamera.transform.TransformDirection(gazeData.gaze.forward);

                    // Fixation point can be calculated using ray origin, direction and focus distance
                    fixationPointTransform.position = rayOrigin + direction * gazeData.focusDistance;
                }
            }
        }

        if (Physics.SphereCast(rayOrigin, gazeRadius, direction, out hit))
        {


            // Put target on gaze raycast position with offset towards user
            gazeTarget.transform.position = hit.point - direction * targetOffset;

            // Make gaze target point towards user
            gazeTarget.transform.LookAt(rayOrigin, Vector3.up);

            // Scale gazetarget with distance so it apperas to be always same size
            distance = hit.distance;
            gazeTarget.transform.localScale = Vector3.one * distance;


            // Prefer layers or tags to identify looked objects in your application
            // This is done here using GetComponent for the sake of clarity as an example
            //RotateWithGaze rotateWithGaze = hit.collider.gameObject.GetComponent<RotateWithGaze>();
            //if (rotateWithGaze != null)
            //{
            //    rotateWithGaze.RayHit();
            //}
            gazedObject = hit.collider.gameObject.name;
            Debug.Log("GazeObject" + gazedObject);


            // Alternative way to check if you hit object with tag
            /*
            if (hit.transform.CompareTag("TrackObject"))
            {
                gazedObject = hit.collider.gameObject.name;
                Debug.Log("GazeObject"+gazedObject);
                //AddForceAtHitPosition();
            }
            */
        }
        else
        {
            // If gaze ray didn't hit anything, the gaze target is shown at fixed distance
            gazeTarget.transform.position = rayOrigin + direction * floatingGazeTargetDistance;
            gazeTarget.transform.LookAt(rayOrigin, Vector3.up);
            gazeTarget.transform.localScale = Vector3.one * floatingGazeTargetDistance;
            gazedObject = "Nothing tracked";
        }

        if (Input.GetKeyDown(loggingToggleKey))
        {
            if (!logging)
            {
                StartLogging();
            }
            else
            {
                StopLogging();
            }
            return;
        }

        if (logging)
        {
            int dataCount = VarjoEyeTracking.GetGazeList(out dataSinceLastUpdate, out eyeMeasurementsSinceLastUpdate);
            if (printFramerate) gazeDataCount += dataCount;
            for (int i = 0; i < dataCount; i++)
            {
                LogGazeData(dataSinceLastUpdate[i], eyeMeasurementsSinceLastUpdate[i]);
            }
            gazedObject = "";

        }
    }

    void AddForceAtHitPosition()
    {
        //Get Rigidbody form hit object and add force on hit position
        Rigidbody rb = hit.rigidbody;
        if (rb != null)
        {
            rb.AddForceAtPosition(direction * hitForce, hit.point, ForceMode.Force);
        }
    }

    void LogGazeData(VarjoEyeTracking.GazeData data, VarjoEyeTracking.EyeMeasurements eyeMeasurements)
    {
        string[] logData = new string[49];

        // Gaze data frame number
        logData[0] = data.frameNumber.ToString();

        // Gaze data capture time (nanoseconds)

        logData[1] = (data.captureTime / 1000000).ToString();


        // Log time (milliseconds)
        logData[2] = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString();

        // HMD
        logData[3] = xrCamera.transform.localPosition.x.ToString("F3");
        logData[4] = xrCamera.transform.localPosition.y.ToString("F3");
        logData[5] = xrCamera.transform.localPosition.z.ToString("F3");
        logData[6] = xrCamera.transform.localRotation.x.ToString("F3");
        logData[7] = xrCamera.transform.localRotation.y.ToString("F3");
        logData[8] = xrCamera.transform.localRotation.z.ToString("F3");

        // Combined gaze
        bool invalid = data.status == VarjoEyeTracking.GazeStatus.Invalid;
        logData[9] = invalid ? InvalidString : ValidString;
        logData[10] = invalid ? "" : data.gaze.forward.x.ToString("F3");
        logData[11] = invalid ? "" : data.gaze.forward.y.ToString("F3");
        logData[12] = invalid ? "" : data.gaze.forward.z.ToString("F3");
        logData[13] = invalid ? "" : data.gaze.origin.x.ToString("F3");
        logData[14] = invalid ? "" : data.gaze.origin.y.ToString("F3");
        logData[15] = invalid ? "" : data.gaze.origin.z.ToString("F3");

        // IPD
        logData[16] = invalid ? "" : eyeMeasurements.interPupillaryDistanceInMM.ToString("F3");

        // Left eye
        bool leftInvalid = data.leftStatus == VarjoEyeTracking.GazeEyeStatus.Invalid;
        logData[17] = leftInvalid ? InvalidString : ValidString;
        logData[18] = leftInvalid ? "" : data.left.forward.x.ToString("F3");
        logData[19] = leftInvalid ? "" : data.left.forward.y.ToString("F3");
        logData[20] = leftInvalid ? "" : data.left.forward.z.ToString("F3");
        logData[21] = leftInvalid ? "" : data.left.origin.x.ToString("F3");
        logData[22] = leftInvalid ? "" : data.left.origin.y.ToString("F3");
        logData[23] = leftInvalid ? "" : data.left.origin.z.ToString("F3");
        logData[24] = leftInvalid ? "" : eyeMeasurements.leftPupilIrisDiameterRatio.ToString("F3");
        logData[25] = leftInvalid ? "" : eyeMeasurements.leftPupilDiameterInMM.ToString("F3");
        logData[26] = leftInvalid ? "" : eyeMeasurements.leftIrisDiameterInMM.ToString("F3");
        logData[27] = leftInvalid ? "" : eyeMeasurements.leftEyeOpenness.ToString("F3");

        // Right eye
        bool rightInvalid = data.rightStatus == VarjoEyeTracking.GazeEyeStatus.Invalid;
        logData[28] = rightInvalid ? InvalidString : ValidString;
        logData[29] = rightInvalid ? "" : data.right.forward.x.ToString("F3");
        logData[30] = rightInvalid ? "" : data.right.forward.y.ToString("F3");
        logData[31] = rightInvalid ? "" : data.right.forward.z.ToString("F3");
        logData[32] = rightInvalid ? "" : data.right.origin.x.ToString("F3");
        logData[33] = rightInvalid ? "" : data.right.origin.y.ToString("F3");
        logData[34] = rightInvalid ? "" : data.right.origin.z.ToString("F3");
        logData[35] = rightInvalid ? "" : eyeMeasurements.rightPupilIrisDiameterRatio.ToString("F3");
        logData[36] = rightInvalid ? "" : eyeMeasurements.rightPupilDiameterInMM.ToString("F3");
        logData[37] = rightInvalid ? "" : eyeMeasurements.rightIrisDiameterInMM.ToString("F3");
        logData[38] = rightInvalid ? "" : eyeMeasurements.rightEyeOpenness.ToString("F3");

        // Focus
        logData[39] = invalid ? "" : data.focusDistance.ToString();
        logData[40] = invalid ? "" : data.focusStability.ToString();

        ////C, N, W, H
        //public string condition = "";
        ////1,2,3,4,5
        //public string scene = "";
        ////1,2,3,4
        //public int task = 0;

        //public string gazedObject = "";
        ////"" or 1
        //public string clickedObject = "";

        //public string quizAnswer = "";
        ////1 or 0
        //public int chatBot = 0;

        //condition
        logData[41] = invalid ? "" : condition.ToString();
        //scene
        logData[42] = invalid ? "" : scene;
        //task
        logData[43] = invalid ? "" : task.ToString();
        //gazedObject
        logData[44] = invalid ? "" : gazedObject;

        //clickedObject
        logData[45] = invalid ? "" : clickedObject;
        clickedObject = "";

        //quizAnswer
        logData[46] = invalid ? "" : quizAnswer;
        quizAnswer = "";
        //chatBot
        logData[47] = invalid ? "" : chatBot.ToString();
        //Performance
        logData[48] = performance;
        performance = "";

        Log(logData);
    }

    // Write given values in the log file
    void Log(string[] values)
    {
        if (!logging || writer == null)
            return;

        string line = "";
        for (int i = 0; i < values.Length; ++i)
        {
            values[i] = values[i].Replace("\r", "").Replace("\n", ""); // Remove new lines so they don't break csv
            line += values[i] + (i == (values.Length - 1) ? "" : ";"); // Do not add semicolon to last data string
        }
        writer.WriteLine(line);
    }

    public void StartLogging()
    {
        Debug.Log("Gaze Log file started 0 ");

        if (logging)
        {
            Debug.LogWarning("Logging was on when StartLogging was called. No new log was started.");
            return;
        }
        Debug.Log("Gaze Log file started 1 ");

        logging = true;

        string logPath = useCustomLogPath ? customLogPath : Application.dataPath + "/Logs/";
        Debug.Log("Gaze" + logPath);

        Directory.CreateDirectory(logPath);
        Debug.Log("Gaze" + logPath);

        DateTime now = DateTime.Now;
        string fileName = "ID_" + "002" + "_Scene_" + scene + "_Condition_" + condition + "_" + string.Format("{0}-{1:00}-{2:00}-{3:00}-{4:00}", now.Year, now.Month, now.Day, now.Hour, now.Minute);
        Debug.Log("Gaze" + logPath);

        string path = logPath + fileName + ".csv";
        writer = new StreamWriter(path);

        Log(ColumnNames);
        Debug.Log("Gaze Log file started at: " + path);
    }

    void StopLogging()
    {
        if (!logging)
            return;

        if (writer != null)
        {
            writer.Flush();
            writer.Close();
            writer = null;
        }
        logging = false;
        Debug.Log("Logging ended");
    }

    void OnApplicationQuit()
    {
        StopLogging();
    }

    public void SetClickedObject(string name)
    {
        clickedObject = name;
    }

    public void IncreaseTaskNumber()
    {
        task++;
    }
}