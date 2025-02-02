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

public class GazeRayController : MonoBehaviour
{
    [SerializeField] private List<GazeObject> gazeObjects;
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
        eyeControllerData = CSVFileReader.GetCSVFileListofDic("EyeTrackData2");
        messageBuffer = new FrameBuffer();


    }

    private void Start()
    {
        SetGazeObjLayers();
    }

    private void SetGazeObjLayers()
    {
        foreach (var gazeObj in gazeObjects)
        {
            gazeObj.obj.layer = gazeHitLayer;
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

            isRealTime = true;
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
            gazeObjects.Find(gazeObj => gazeObj.obj.Equals(fixatedObj)).fixatitedTimeSec += Time.deltaTime;
            Debug.Log("GazeObject" + fixatedObj.name);


            
        }
        else
        {
            // If gaze ray didn't hit anything, the gaze target is shown at fixed distance
            if (fixatedObj == null)
            {
                return;
            }
            var lastGazeObj = gazeObjects.Find(gazeObj => gazeObj.obj.Equals(fixatedObj));
            if (!IsEnoughFocused(lastGazeObj.fixationThresholdSec, lastGazeObj.fixatitedTimeSec))
            {
                fixationUIController.EnableText();
                string alertText = $"User lost attention. {String.Format("{0:0.00}", lastGazeObj.fixatitedTimeSec)} seconds had been focused";
                fixationUIController.SetText(alertText);
            }
            var obj = gazeObjects.Find(gazeObj => gazeObj.Equals(fixatedObj));
            if(obj != null)
                obj.fixatitedTimeSec = 0f;
            fixatedObj = null;
        }

        if (frameCounter % 60 == 0)
        {
            ServerCommunicationManager.Instance.SendCSVBufferToServer(messageBuffer);
            messageBuffer = new FrameBuffer();
        }
        messageBuffer.AddFrame(new GazeData(gazeData,fixatedObj));


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
   
    public float HeadPositionX;
    public float HeadPositionY;
    public float HeadPositionZ;
    public float HeadDirectionX;
    public float HeadDirectionY;
    public float HeadDirectionZ;
    public float CombinedGazeForwardX;
    public float CombinedGazeForwardY;
    public float CombinedGazeForwardZ;
    public float LeftEyeStatus;
    public float LeftEyePositionX;
    public float LeftEyePositionY;
    public float LeftEyePositionZ;
    public float LeftGazeDirectionX;
    public float LeftGazeDirectionY;
    public float LeftGazeDirectionZ;
    public float RightEyeStatus;
    public float RightEyePositionX;
    public float RightEyePositionY;
    public float RightEyePositionZ;
    public float RightGazeDirectionX;
    public float RightGazeDirectionY;
    public float RightGazeDirectionZ;
    public float FocusDistance;
    public float FocusStability;
    public string Condition;
    public string Scene;
    public string Task;
    public string GazedObject;
    public string ClickedObject;
    public string QuizAnswer;
    public string ChatBot;



    // Constructor to convert from VarjoEyeTracking.GazeData and VarjoEyeTracking.EyeMeasurements to custom EyeTrackingData class
    public GazeData(VarjoEyeTracking.GazeData gazeData, GameObject fixatedObj)
    {
        // Mapping from GazeData struct to the required properties
        HeadPositionX = gazeData.gaze.origin.x;
        HeadPositionY = gazeData.gaze.origin.y;
        HeadPositionZ = gazeData.gaze.origin.z;

        HeadDirectionX = gazeData.gaze.origin.x;
        HeadDirectionY = gazeData.gaze.origin.y;
        HeadDirectionZ = gazeData.gaze.origin.z;

        CombinedGazeForwardX = gazeData.gaze.origin.x;
        CombinedGazeForwardY = gazeData.gaze.origin.y;
        CombinedGazeForwardZ = gazeData.gaze.origin.z;

        LeftEyeStatus = (float)gazeData.leftStatus;  // Cast enum to float
        LeftEyePositionX = gazeData.left.origin.x;
        LeftEyePositionY = gazeData.left.origin.y;
        LeftEyePositionZ = gazeData.left.origin.z;

        LeftGazeDirectionX = gazeData.left.origin.x;
        LeftGazeDirectionY = gazeData.left.origin.y;
        LeftGazeDirectionZ = gazeData.left.origin.z;

        RightEyeStatus = (float)gazeData.rightStatus;  // Cast enum to float
        RightEyePositionX = gazeData.right.origin.x;
        RightEyePositionY = gazeData.right.origin.y;
        RightEyePositionZ = gazeData.right.origin.z;

        RightGazeDirectionX = gazeData.right.origin.x;
        RightGazeDirectionY = gazeData.right.origin.y;
        RightGazeDirectionZ = gazeData.right.origin.z;

        // Mapping from EyeMeasurements struct to the required properties
        FocusDistance = gazeData.focusDistance;
        FocusStability = gazeData.focusStability;

        // You can populate these from additional data sources, as they are not available in the provided structs
        Condition = "Unknown"; // Example default value
        Scene = "Unknown"; // Example default value
        Task = "Unknown"; // Example default value
        GazedObject = fixatedObj == null ? "Invalid" : fixatedObj.name; // Example default value
        ClickedObject = "Unknown"; // Example default value
        QuizAnswer = "Unknown"; // Example default value
        ChatBot = "Unknown"; // Example default value
    }
    public override string ToString()
    {
        string line = $"{HeadPositionX},{HeadPositionY},{HeadPositionZ}," +
              $"{HeadDirectionX},{HeadDirectionY},{HeadDirectionZ}," +
              $"{CombinedGazeForwardX},{CombinedGazeForwardY},{CombinedGazeForwardZ}," +
              $"{LeftEyeStatus},{LeftEyePositionX},{LeftEyePositionY},{LeftEyePositionZ}," +
              $"{LeftGazeDirectionX},{LeftGazeDirectionY},{LeftGazeDirectionZ}," +
              $"{RightEyeStatus},{RightEyePositionX},{RightEyePositionY},{RightEyePositionZ}," +
              $"{RightGazeDirectionX},{RightGazeDirectionY},{RightGazeDirectionZ}," +
              $"{FocusDistance},{FocusStability}," +
              $"{Condition},{Scene},{Task},{GazedObject},{ClickedObject},{QuizAnswer},{ChatBot}";
        return line;
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
        gazeDataLines.Add("HeadPositionX,HeadPositionY,HeadPositionZ," +
                          "HeadDirectionX,HeadDirectionY,HeadDirectionZ," +
                          "CombinedGazeForwardX,CombinedGazeForwardY,CombinedGazeForwardZ," +
                          "LeftEyeStatus,LeftEyePositionX,LeftEyePositionY,LeftEyePositionZ," +
                          "LeftGazeDirectionX,LeftGazeDirectionY,LeftGazeDirectionZ," +
                          "RightEyeStatus,RightEyePositionX,RightEyePositionY,RightEyePositionZ," +
                          "RightGazeDirectionX,RightGazeDirectionY,RightGazeDirectionZ," +
                          "FocusDistance,FocusStability,Condition,Scene,Task,GazedObject,ClickedObject,QuizAnswer,ChatBot");
        gazeDataLines = new List<String>();
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





