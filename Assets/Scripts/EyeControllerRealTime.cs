using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Varjo.XR;

public class EyeControllerRealTime : MonoBehaviour
{
    [SerializeField] private Transform leftEyeTransform;
    [SerializeField] private Transform rightEyeTransform;
    [SerializeField] private Transform headBone;         // Reference to the head bone of the character

    private List<InputDevice> devices = new List<InputDevice>();
    private InputDevice device;
    private Eyes eyes;
    private VarjoEyeTracking.GazeData gazeData;
    private List<VarjoEyeTracking.EyeMeasurements> eyeMeasurementsSinceLastUpdate;

    public Camera xrCamera;



    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 40;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        // Make the head bone match the VR camera's rotation
        headBone.rotation = xrCamera.transform.rotation;
        SetData();
    }



    private void SetData()
    {
        if (VarjoEyeTracking.IsGazeAllowed() && VarjoEyeTracking.IsGazeCalibrated())
        {

            //Get device if not valid
            if (!device.isValid)
            {
                GetDevice();
            }

            
            {
                gazeData = VarjoEyeTracking.GetGaze();

                if (gazeData.status != VarjoEyeTracking.GazeStatus.Invalid)
                {
                    // GazeRay vectors are relative to the HMD pose so they need to be transformed to world space
                    if (gazeData.leftStatus != VarjoEyeTracking.GazeEyeStatus.Invalid)
                    {
                        //leftEyeTransform.position = xrCamera.transform.TransformPoint(gazeData.left.origin);
                        leftEyeTransform.rotation = Quaternion.LookRotation(xrCamera.transform.TransformDirection(gazeData.left.forward));
                    }

                    if (gazeData.rightStatus != VarjoEyeTracking.GazeEyeStatus.Invalid)
                    {
                       // rightEyeTransform.position = xrCamera.transform.TransformPoint(gazeData.right.origin);
                        rightEyeTransform.rotation = Quaternion.LookRotation(xrCamera.transform.TransformDirection(gazeData.right.forward));
                    }


                }
            }
        }

        
    }

    void GetDevice()
    {
        InputDevices.GetDevicesAtXRNode(XRNode.CenterEye, devices);
        device = devices.FirstOrDefault();
    }
}
