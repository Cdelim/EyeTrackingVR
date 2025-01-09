using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Varjo.XR;
using UnityEngine.XR;
using System.Linq;

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
    private Vector3 rayOrigin;
    private Vector3 direction;
    private RaycastHit hit;
    private float distance;


    private GameObject fixatedObj;
    private bool isFixatedStarted = false;
    private float fixationDurationSec;
    private List<InputDevice> devices = new List<InputDevice>();
    private InputDevice device;
    private int maxDistance = 5;
    private float timer;

    private void Awake()
    {
        CSVFileReader = new CSVFileReader();
        eyeControllerData = CSVFileReader.GetCSVFileListofDic("EyeTrackData2");
        
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
        if (device.isValid)
        {
            GetDevice();
            isRealTime = true;
        }
    }

    private void GetDevice()
    {
        InputDevices.GetDevicesAtXRNode(XRNode.CenterEye, devices);
        device = devices.FirstOrDefault();
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
            gazeData = VarjoEyeTracking.GetGaze();

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
            gazeObjects.Find(gazeObj => gazeObj.Equals(fixatedObj)).fixatitedTimeSec = 0f;
            fixatedObj = null;
        }


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




