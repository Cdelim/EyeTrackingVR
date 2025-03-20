using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Varjo.XR;
using UnityEngine.XR;
using System.Linq;
using GLTFast.Schema;
using Camera = UnityEngine.Camera;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using UnityEngine.UI;

public class GazeRayController : MonoBehaviour
{
    [SerializeField] private List<GameObject> gazeObjects;
    [SerializeField] private GameObject targetObject;
    [SerializeField]private FixationUIController fixationUIController;
    [SerializeField] private Eyes eyes;
    [SerializeField] private Transform head;
    [Header("XR camera")]
    [SerializeField] private Camera xrCamera;
    [Header("Visualization Transforms")]
    [SerializeField] private Transform fixationPointTransform;
    [Header("Gaze Settings")]
    [SerializeField] private float gazeRadius = 0.01f;
    [Header("Gaze target offset towards viewer")]
    [SerializeField] private float targetOffset = 0.2f;


    [SerializeField] private LayerMask gazeHitLayer;
    [SerializeField] private ServerCommunicationManager serverCommunicationManager;
    [SerializeField] private ResultsCanvasController resultsCanvasController;




    private List<Dictionary<string, string>> eyeControllerData = new List<Dictionary<string, string>>();
    private CSVFileReader CSVFileReader;
    private bool isRealTime = false;
    private int frameCounter = 0;
    private VarjoEyeTracking.GazeData gazeData;

    private List<VarjoEyeTracking.GazeData> dataSinceLastUpdate;
    private List<VarjoEyeTracking.EyeMeasurements> eyeMeasurementsSinceLastUpdate;

    private Vector3 rayOrigin;
    private Vector3 direction;
    private RaycastHit hit;
    private float distance;


    private GameObject fixatedObj;
    private bool isFixatedStarted = false;
    private float fixationDurationSec;
    private List<InputDevice> devices = new List<InputDevice>();
    private InputDevice device;
    private int maxDistance = 50;
    private float timer;
    private FrameBuffer messageBuffer;

    private void Awake()
    {
        CSVFileReader = new CSVFileReader();
        eyeControllerData = CSVFileReader.GetCSVFileListofDic("ID_002_Scene__Condition_0_2024-11-05-13-01");
        messageBuffer = new FrameBuffer();
        resultsCanvasController.Initialize(gazeObjects);


    }

    private void Start()
    {
        SetGazeObjLayers();
    }

    private void SetGazeObjLayers()
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

    private void OnEnable()
    {
        if (!device.isValid)
        {
            GetDevice();
        }
    }

    private void GetDevice()
    {
        InputDevices.GetDevicesAtXRNode(XRNode.CenterEye, devices);
        device = devices.FirstOrDefault();
        if(device != null)
        {

            isRealTime = !device.isValid;
        }
    }

    private void Update()
    {
        if (!isRealTime)
        {
            if (frameCounter == eyeControllerData.Count - 1)
            {
                Debug.LogError("Started Again");
                frameCounter = 0;
            };
            Debug.LogError("Not Real Time Data");
            Dictionary<string, string> frameData = eyeControllerData[frameCounter];
            if (frameData[EyeTrackDataColums.LeftEyeStatus] == "INVALID"
                || frameData[EyeTrackDataColums.RightEyeStatus] == "INVALID")
            {
                frameCounter++;
                return;
            }
            frameCounter++;

            float xLeftEyePos = float.Parse(frameData[EyeTrackDataColums.LeftEyePositionX]);
            float yLeftEyePos = float.Parse(frameData[EyeTrackDataColums.LeftEyePositionY]);
            float zLeftEyePos = float.Parse(frameData[EyeTrackDataColums.LeftEyePositionZ]);

            Vector3 leftEyePos = new Vector3(xLeftEyePos, yLeftEyePos, zLeftEyePos);

            float xLeftEye = float.Parse(frameData[EyeTrackDataColums.LeftGazeDirectionX]);
            float yLeftEye = float.Parse(frameData[EyeTrackDataColums.LeftGazeDirectionY]);
            float zLeftEye = float.Parse(frameData[EyeTrackDataColums.LeftGazeDirectionZ]);

            Vector3 leftEyeDirection = new Vector3(xLeftEye, yLeftEye, zLeftEye);



            float xRightEyePos = float.Parse(frameData[EyeTrackDataColums.RightEyePositionX]);
            float yRightEyePos = float.Parse(frameData[EyeTrackDataColums.RightEyePositionY]);
            float zRightEyePos = float.Parse(frameData[EyeTrackDataColums.RightEyePositionZ]);

            Vector3 rightEyePos = new Vector3(xRightEyePos, yRightEyePos, zRightEyePos);

            float xRightEye = float.Parse(frameData[EyeTrackDataColums.RightGazeDirectionX]);
            float yRightEye = float.Parse(frameData[EyeTrackDataColums.RightGazeDirectionY]);
            float zRightEye = float.Parse(frameData[EyeTrackDataColums.RightGazeDirectionZ]);

            Vector3 rightEyeDirection = new Vector3(xRightEye, yRightEye, zRightEye);

            //eyes.leftEye.position = xrCamera.transform.TransformPoint(leftEyePos);
            eyes.leftEye.localRotation = Quaternion.LookRotation(FindDirVector(leftEyePos, leftEyeDirection).normalized);
            //eyes.leftEye.rotation = Quaternion.LookRotation(xrCamera.transform.TransformDirection(gazeData.left.forward));

            //eyes.rightEye.position = xrCamera.transform.TransformPoint(leftEyePos);
            eyes.rightEye.localRotation = Quaternion.LookRotation(FindDirVector(rightEyePos, rightEyeDirection).normalized);
            //eyes.rightEye.rotation = Quaternion.LookRotation(xrCamera.transform.TransformDirection(gazeData.right.forward));


            // Set gaze origin as raycast origin
            rayOrigin = (eyes.rightEye.position + eyes.leftEye.position)*.5f;

            // Set gaze direction as raycast direction
            direction = FindDirVector(leftEyePos, leftEyeDirection).normalized;

            // Fixation point can be calculated using ray origin, direction and focus distance
            fixationPointTransform.position = rayOrigin + direction * maxDistance;

            Debug.DrawLine(rayOrigin, rayOrigin + direction * maxDistance);

        }
        else
        {
            head.rotation = xrCamera.transform.rotation;
            frameCounter++;
            gazeData = VarjoEyeTracking.GetGaze();

            
            int dataCount = VarjoEyeTracking.GetGazeList(out dataSinceLastUpdate, out eyeMeasurementsSinceLastUpdate);
           
            if (gazeData.status != VarjoEyeTracking.GazeStatus.Invalid)
            {
                // GazeRay vectors are relative to the HMD pose so they need to be transformed to world space
                if (gazeData.leftStatus != VarjoEyeTracking.GazeEyeStatus.Invalid)
                {
                    eyes.leftEye.position = xrCamera.transform.TransformPoint(gazeData.left.origin);
                    eyes.leftEye.rotation = Quaternion.LookRotation(xrCamera.transform.TransformDirection(gazeData.left.forward));
                }

                if (gazeData.rightStatus != VarjoEyeTracking.GazeEyeStatus.Invalid)
                {
                    eyes.rightEye.position = xrCamera.transform.TransformPoint(gazeData.right.origin);
                    eyes.rightEye.rotation = Quaternion.LookRotation(xrCamera.transform.TransformDirection(gazeData.right.forward));
                }

                // Set gaze origin as raycast origin
                rayOrigin = xrCamera.transform.TransformPoint(gazeData.gaze.origin);

                // Set gaze direction as raycast direction
                direction = xrCamera.transform.TransformDirection(gazeData.gaze.forward);

                // Fixation point can be calculated using ray origin, direction and focus distance
                fixationPointTransform.position = rayOrigin + direction * gazeData.focusDistance;
            }
        }

        RaycastHit hit;
        if (Physics.SphereCast(rayOrigin, gazeRadius, direction, out hit, maxDistance, gazeHitLayer))
        {


            /*// Put target on gaze raycast position with offset towards user
            gazeTarget.transform.position = hit.point - direction * targetOffset;

            // Make gaze target point towards user
            gazeTarget.transform.LookAt(rayOrigin, Vector3.up);

            // Scale gazetarget with distance so it apperas to be always same size
            distance = hit.distance;
            gazeTarget.transform.localScale = Vector3.one * distance;*/


            // Prefer layers or tags to identify looked objects in your application
            // This is done here using GetComponent for the sake of clarity as an example
            //RotateWithGaze rotateWithGaze = hit.collider.gameObject.GetComponent<RotateWithGaze>();
            //if (rotateWithGaze != null)
            //{
            //    rotateWithGaze.RayHit();
            //}
            fixatedObj = hit.collider.gameObject;
            /*gazeObjects.Find(gazeObj => gazeObj..Equals(fixatedObj)).fixatitedTimeSec += Time.deltaTime;
            Debug.Log("GazeObject" + fixatedObj.name);*/


            
        }
        else
        {
            // If gaze ray didn't hit anything, the gaze target is shown at fixed distance
            if (fixatedObj == null)
            {
                return;
            }
            var lastGazeObj = gazeObjects.Find(gazeObj => gazeObj.Equals(fixatedObj));
            /*if (!IsEnoughFocused(lastGazeObj.fixationThresholdSec, lastGazeObj.fixatitedTimeSec))
            {
                fixationUIController.EnableText();
                string alertText = $"User lost attention. {String.Format("{0:0.00}", lastGazeObj.fixatitedTimeSec)} seconds had been focused";
                fixationUIController.SetText(alertText);
            }
            var obj = gazeObjects.Find(gazeObj => gazeObj.Equals(fixatedObj));
            if(obj != null)
                obj.fixatitedTimeSec = 0f;
            fixatedObj = null;*/
        }

        if (frameCounter % 240 == 0)
        {
            serverCommunicationManager.SendCSVBufferToServer(messageBuffer);
            messageBuffer = new FrameBuffer();
        }
        messageBuffer.AddFrame(new GazeData(gazeData,fixatedObj, targetObject,VarjoEyeTracking.GetEyeMeasurements(),xrCamera));
        Debug.Log(gazeData.status);


    }



    private bool IsEnoughFocused(float treshHoldSec, float focusedTimeSec)
    {
        return focusedTimeSec >= treshHoldSec;
    }

    private Vector3 FindDirVector(Vector3 pos, Vector3 dir)
    {
        return dir - pos;
    }


}

[System.Serializable]
public class GazeObject
{
    public GameObject obj;
    public int fixationThresholdSec = 0;
    public float fixatitedTimeSec = 0;
}


[System.Serializable]
public class GazeData
{

    private const string ValidString = "VALID";
    private const string InvalidString = "INVALID";


    public float Frame = 0;
    public long TimeStamp = 0;
    public float LogTime = 0;

    public float HeadPositionX;
    public float HeadPositionY;
    public float HeadPositionZ;
    public float HeadDirectionX;
    public float HeadDirectionY;
    public float HeadDirectionZ;

    public string GazeStatus;
    public float CombinedGazeForwardX;
    public float CombinedGazeForwardY;
    public float CombinedGazeForwardZ;
    public float CombinedGazePositionX;
    public float CombinedGazePositionY;
    public float CombinedGazePositionZ;
    public float InterPupillaryDistanceInMM;

    public string LeftEyeStatus;
    public float LeftGazeDirectionX;
    public float LeftGazeDirectionY;
    public float LeftGazeDirectionZ;
    public float LeftEyePositionX;
    public float LeftEyePositionY;
    public float LeftEyePositionZ;
    public float LeftPupilIrisDiameterRatio;
    public float LeftPupilDiameterInMM;
    public float LeftIrisDiameterInMM;
    public float LeftEyeOpenness;

    public string RightEyeStatus;
    public float RightGazeDirectionX;
    public float RightGazeDirectionY;
    public float RightGazeDirectionZ;
    public float RightEyePositionX;
    public float RightEyePositionY;
    public float RightEyePositionZ;
    public float RightPupilIrisDiameterRatio;
    public float RightPupilDiameterInMM;
    public float RightIrisDiameterInMM;
    public float RightEyeOpenness;

    public float FocusDistance;
    public float FocusStability;

    public string Condition;
    public string Scene;
    public string Task;
    public string GazedObject;
    public string ClickedObject;
    public string QuizAnswer;
    public string ChatBot;

    public override string ToString()
    {
        return $"{Frame};{TimeStamp};{LogTime};" +
               $"{HeadPositionX};{HeadPositionY};{HeadPositionZ};" +
               $"{HeadDirectionX};{HeadDirectionY};{HeadDirectionZ};" +
               $"{GazeStatus};{CombinedGazeForwardX};{CombinedGazeForwardY};{CombinedGazeForwardZ};" +
               $"{CombinedGazePositionX};{CombinedGazePositionY};{CombinedGazePositionZ};" +
               $"{InterPupillaryDistanceInMM};" +
               $"{LeftEyeStatus};{LeftGazeDirectionX};{LeftGazeDirectionY};{LeftGazeDirectionZ};" +
               $"{LeftEyePositionX};{LeftEyePositionY};{LeftEyePositionZ};" +
               $"{LeftPupilIrisDiameterRatio};{LeftPupilDiameterInMM};{LeftIrisDiameterInMM};{LeftEyeOpenness};" +
               $"{RightEyeStatus};{RightGazeDirectionX};{RightGazeDirectionY};{RightGazeDirectionZ};" +
               $"{RightEyePositionX};{RightEyePositionY};{RightEyePositionZ};" +
               $"{RightPupilIrisDiameterRatio};{RightPupilDiameterInMM};{RightIrisDiameterInMM};{RightEyeOpenness};" +
               $"{FocusDistance};{FocusStability};" +
               $"{Condition};{Scene};{Task};{GazedObject};{ClickedObject};{QuizAnswer};{ChatBot}";
    }

    // Constructor to convert from VarjoEyeTracking.GazeData and VarjoEyeTracking.EyeMeasurements to custom EyeTrackingData class
    public GazeData(VarjoEyeTracking.GazeData data, GameObject fixatedObj, GameObject targetObject,VarjoEyeTracking.EyeMeasurements eyeMeasurements
        , Camera xrCamera)
    {


        Frame = data.frameNumber;

        
        TimeStamp = data.captureTime;


      
        LogTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

        
        HeadPositionX = xrCamera.transform.localPosition.x;
        HeadPositionY = xrCamera.transform.localPosition.y;
        HeadPositionZ = xrCamera.transform.localPosition.z;
        HeadDirectionX = xrCamera.transform.localRotation.x;
        HeadDirectionY = xrCamera.transform.localRotation.y;
        HeadDirectionZ = xrCamera.transform.localRotation.z;

        // Combined gaze
        bool invalid = data.status == VarjoEyeTracking.GazeStatus.Invalid;
        GazeStatus = invalid ? "INVALID" : "VALID";
        CombinedGazeForwardX = invalid ? 0 : data.gaze.forward.x;
        CombinedGazeForwardY = invalid ? 0 : data.gaze.forward.y;
        CombinedGazeForwardZ = invalid ? 0 : data.gaze.forward.z;
        CombinedGazePositionX = invalid ? 0 : data.gaze.origin.x;
        CombinedGazePositionY = invalid ? 0 : data.gaze.origin.y;
        CombinedGazePositionZ = invalid ? 0 : data.gaze.origin.z;

        // IPD
        InterPupillaryDistanceInMM = invalid ? 0 : eyeMeasurements.interPupillaryDistanceInMM;

        // Left eye
        bool leftInvalid = data.leftStatus == VarjoEyeTracking.GazeEyeStatus.Invalid;
        LeftEyeStatus = invalid ? "INVALID" : "VALID";
        LeftGazeDirectionX = leftInvalid ? 0 : data.left.forward.x;
        LeftGazeDirectionY = leftInvalid ? 0 : data.left.forward.y;
        LeftGazeDirectionZ = leftInvalid ? 0 : data.left.forward.z;
        LeftEyePositionX = leftInvalid ? 0 : data.left.origin.x;
        LeftEyePositionY = leftInvalid ? 0 : data.left.origin.y;
        LeftEyePositionZ = leftInvalid ? 0 : data.left.origin.z;
        LeftPupilIrisDiameterRatio = leftInvalid ? 0 : eyeMeasurements.leftPupilIrisDiameterRatio;
        LeftPupilDiameterInMM = leftInvalid ? 0 : eyeMeasurements.leftPupilDiameterInMM;
        LeftIrisDiameterInMM = leftInvalid ? 0 : eyeMeasurements.leftIrisDiameterInMM;
        LeftEyeOpenness = leftInvalid ? 0 : eyeMeasurements.leftEyeOpenness;

        // Right eye
        bool rightInvalid = data.rightStatus == VarjoEyeTracking.GazeEyeStatus.Invalid;
        RightEyeStatus = invalid ? "INVALID" : "VALID";
        RightGazeDirectionX = rightInvalid ? 0 : data.right.forward.x;
        RightGazeDirectionY = rightInvalid ? 0 : data.right.forward.y;
        RightGazeDirectionZ = rightInvalid ? 0 : data.right.forward.z;
        RightEyePositionX = rightInvalid ? 0 : data.right.origin.x;
        RightEyePositionY = rightInvalid ? 0 : data.right.origin.y;
        RightEyePositionZ = rightInvalid ? 0 : data.right.origin.z;
        RightPupilIrisDiameterRatio = rightInvalid ? 0 : eyeMeasurements.rightPupilIrisDiameterRatio;
        RightPupilDiameterInMM = rightInvalid ? 0 : eyeMeasurements.rightPupilDiameterInMM;
        RightIrisDiameterInMM = rightInvalid ? 0 : eyeMeasurements.rightIrisDiameterInMM;
        RightEyeOpenness = rightInvalid ? 0 : eyeMeasurements.rightEyeOpenness;

        // Focus
        FocusDistance = invalid ? 0 : data.focusDistance;
        FocusStability = invalid ? 0 : data.focusStability;

        

        // You can populate these from additional data sources, as they are not available in the provided structs
        Condition = "Unknown"; // Example default value
        Scene = "Unknown"; // Example default value
        Task = targetObject == null ? "empty_task" : targetObject.name; // Example default value
        GazedObject = fixatedObj == null ? "Invalid" : fixatedObj.name; // Example default value
        ClickedObject = "Unknown"; // Example default value
        QuizAnswer = "Unknown"; // Example default value
        ChatBot = "Unknown"; // Example default value
    }
   


   
}


[System.Serializable]
public class FrameBuffer
{
    private List<String> gazeDataLines;
    private int maxSize;

    public FrameBuffer(int size = 60)
    {
        maxSize = size;
        gazeDataLines = new List<String>();
        gazeDataLines.Add("Frame;TimeStamp;LogTime;" +
            "HeadPositionX;HeadPositionY;HeadPositionZ;" +
            "HeadDirectionX;HeadDirectionY;HeadDirectionZ;" +
            "GazeStatus;CombinedGazeForwardX;CombinedGazeForwardY;" +
            "CombinedGazeForwardZ;CombinedGazePositionX;CombinedGazePositionY;CombinedGazePositionZ;" +
            "InterPupillaryDistanceInMM;LeftEyeStatus;" +
            "LeftGazeDirectionX;LeftGazeDirectionY;LeftGazeDirectionZ;" +
            "LeftEyePositionX;LeftEyePositionY;LeftEyePositionZ;" +
            "LeftPupilIrisDiameterRatio;LeftPupilDiameterInMM;" +
            "LeftIrisDiameterInMM;LeftEyeOpenness;" +
            "RightEyeStatus;" +
            "RightGazeDirectionX;RightGazeDirectionY;RightGazeDirectionZ;" +
            "RightEyePositionX;RightEyePositionY;RightEyePositionZ;" +
            "RightPupilIrisDiameterRatio;RightPupilDiameterInMM;RightIrisDiameterInMM;" +
            "RightEyeOpenness;FocusDistance;FocusStability;" +
            "Condition;Scene;Task;GazedObject;ClickedObject;QuizAnswer;ChatBot");
    }

    // Add a frame to the buffer
    public void AddFrame(GazeData newFrame)
    {
        gazeDataLines.Add(newFrame.ToString());  // Add the new frame
    }

    // Get the last 'maxSize' frames
    public List<String> GetLastFrames()
    {
        return (gazeDataLines);  // Convert the queue to a list
    }


}





