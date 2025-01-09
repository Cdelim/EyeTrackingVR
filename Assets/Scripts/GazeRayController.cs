using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Varjo.XR;


public class GazeRayController : MonoBehaviour
{
    [SerializeField]private FixationUIController fixationUIController;
    [SerializeField] private Eyes eyes;
    [SerializeField] private Transform head;
    [Header("XR camera")]
    [SerializeField] private Camera xrCamera;
    [Header("Visualization Transforms")]
    [SerializeField] private Transform fixationPointTransform;

    private List<Dictionary<string, string>> eyeControllerData = new List<Dictionary<string, string>>();
    private CSVFileReader CSVFileReader;
    private bool isRealTime = false;
    private int frameCounter = 0;
    private VarjoEyeTracking.GazeData gazeData;
    private Vector3 rayOrigin;
    private Vector3 direction;

 
    private GameObject fixatedObj;
    private bool isFixatedStarted = false;
    private float fixationDurationSec;

    private void Awake()
    {
        CSVFileReader = new CSVFileReader();
        eyeControllerData = CSVFileReader.GetCSVFileListofDic("EyeTrackData2");
       

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
                return;
            }

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




        frameCounter++;
    }


    private Vector3 FindDirVector(Vector3 pos, Vector3 dir)
    {
        return dir - pos;
    }

}




