using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Eyes
{
    public Transform leftEye;
    public Transform rightEye;


    public void RotateEyes(float degree, Vector3 axis)
    {
        leftEye.rotation = Quaternion.AngleAxis(degree, axis);
        rightEye.rotation = Quaternion.AngleAxis(degree, axis);
    }
}

public class EyeController : MonoBehaviour
{
    [SerializeField]private Eyes eyes;
    [SerializeField]private Transform head;


    private bool isRotationStart = false;
    private float timerSec = 0f;
    private float rotationDuration = .5f;
    private float rotationRotDegree = 30f;
    private float previousRotDegree = 0f;

    private List<Dictionary<string, string>> eyeControllerData = new List<Dictionary<string, string>>();
    private CSVFileReader CSVFileReader;
    private int frameCounter = 0;
    private int framer = 0;

    private Quaternion headInitialRotation;
    private Quaternion leftEyeInitialRot;
    private Quaternion rightEyeInitialRot;
    private void Awake()
    {
        CSVFileReader = new CSVFileReader();
        eyeControllerData = CSVFileReader.GetCSVFileListofDic("EyeTrackData");
        isRotationStart = true;
        headInitialRotation = head.localRotation;
        leftEyeInitialRot = eyes.leftEye.localRotation;
        rightEyeInitialRot = eyes.rightEye.localRotation;

    }

    private void Update()
    {

        if (frameCounter == eyeControllerData.Count - 1)
        {
            frameCounter = 0;
        }
        //if (framer % 60 == 0)
        {
            frameCounter++;
            framer = 0;
        }

        Dictionary<string, string> frameData = eyeControllerData[frameCounter];
        if (frameData[EyeTrackDataColums.LeftEyeStatus] == "INVALID"
            || frameData[EyeTrackDataColums.RightEyeStatus] == "INVALID")
        {
            return;
        }
        float x = float.Parse(frameData[EyeTrackDataColums.HeadDirectionX]);
        float y = float.Parse(frameData[EyeTrackDataColums.HeadDirectionY]);
        float z = float.Parse(frameData[EyeTrackDataColums.HeadDirectionZ]);
        Vector3 headDirection = new Vector3(x, y, z);


        float xPos = float.Parse(frameData[EyeTrackDataColums.HeadPositionX]);
        float yPos = float.Parse(frameData[EyeTrackDataColums.HeadPositionY]);
        float zPos = float.Parse(frameData[EyeTrackDataColums.HeadPositionZ]);

        Vector3 headPos = new Vector3(xPos, yPos, zPos);

        Vector3 centeredHeadDir = FindDirVector(headPos,headDirection).normalized;

        Vector3 headTarget = headPos + centeredHeadDir;
        Debug.Log("Head Direction: " + headDirection);
        float angle = Vector3.Angle(Vector3.forward, headDirection);
        //head.up = head.position + centeredHeadDir;

        //head.position = headPos;
        head.localRotation = Quaternion.LookRotation(centeredHeadDir);



        float xLeftEyePos = float.Parse(frameData[EyeTrackDataColums.LeftEyePositionX]);
        float yLeftEyePos = float.Parse(frameData[EyeTrackDataColums.LeftEyePositionY]);
        float zLeftEyePos = float.Parse(frameData[EyeTrackDataColums.LeftEyePositionZ]);

        Vector3 leftEyePos = new Vector3(xLeftEyePos, yLeftEyePos, zLeftEyePos);

        float xLeftEye = float.Parse(frameData[EyeTrackDataColums.LeftGazeDirectionX]);
        float yLeftEye = float.Parse(frameData[EyeTrackDataColums.LeftGazeDirectionY]);
        float zLeftEye = float.Parse(frameData[EyeTrackDataColums.LeftGazeDirectionZ]);

        Vector3 leftEyeDirection = new Vector3(xLeftEye, yLeftEye, zLeftEye);


        Vector3 leftEyeTarget = eyes.leftEye.position + FindDirVector(leftEyePos,leftEyeDirection).normalized;
        //eyes.leftEye.up = (leftEyeTarget);

        eyes.leftEye.localRotation = Quaternion.LookRotation(FindDirVector(leftEyePos, leftEyeDirection).normalized);


        float xRightEyePos = float.Parse(frameData[EyeTrackDataColums.RightEyePositionX]);
        float yRightEyePos = float.Parse(frameData[EyeTrackDataColums.RightEyePositionY]);
        float zRightEyePos = float.Parse(frameData[EyeTrackDataColums.RightEyePositionZ]);

        Vector3 rightEyePos = new Vector3(xRightEyePos, yRightEyePos, zRightEyePos);

        float xRightEye = float.Parse(frameData[EyeTrackDataColums.RightGazeDirectionX]);
        float yRightEye = float.Parse(frameData[EyeTrackDataColums.RightGazeDirectionY]);
        float zRightEye = float.Parse(frameData[EyeTrackDataColums.RightGazeDirectionZ]);

        Vector3 rightEyeDirection = new Vector3(xRightEye, yRightEye, zRightEye);


        Vector3 rightEyeTarget = eyes.rightEye.position + FindDirVector(rightEyePos, rightEyeDirection).normalized;
        //eyes.rightEye.up = (rightEyeTarget);
        eyes.rightEye.localRotation = Quaternion.LookRotation(FindDirVector(rightEyePos, rightEyeDirection).normalized);





        framer++;
        /*if (isRotationStart)
        {
            timerSec += Time.deltaTime;
            RotateEyes(rotationRotDegree, Vector3.up);
            if (timerSec>= rotationDuration)
            {
                isRotationStart = false;
            }
        }
        else
        {
            previousRotDegree = rotationRotDegree;
            rotationRotDegree = UnityEngine.Random.Range(-30f,30f);
            timerSec = 0f;
            isRotationStart = true;
        }*/
    }

    private Vector3 FindDirVector(Vector3 pos, Vector3 dir)
    {
        return dir - pos;
    }

    private void RotateEyes(float degree,Vector3 axis)
    {
        float rot = Mathf.LerpAngle(0, degree, timerSec / rotationDuration);
        eyes.RotateEyes(rot, axis);
    }
}



public static class EyeTrackDataColums
{
    //Frame;TimeStamp;LogTime;
    //HeadPositionX;HeadPositionY;HeadPositionZ;
    //HeadDirectionX;HeadDirectionY;HeadDirectionZ;GazeStatus;
    //CombinedGazeForwardX;CombinedGazeForwardY;CombinedGazeForwardZ;
    //CombinedGazePositionX;CombinedGazePositionY;CombinedGazePositionZ;
    //InterPupillaryDistanceInMM;
    //LeftEyeStatus;LeftGazeDirectionX;LeftGazeDirectionY;LeftGazeDirectionZ;
    //LeftEyePositionX;LeftEyePositionY;LeftEyePositionZ;
    //LeftPupilIrisDiameterRatio;LeftPupilDiameterInMM;LeftIrisDiameterInMM;LeftEyeOpenness;
    //RightEyeStatus;RightGazeDirectionX;RightGazeDirectionY;RightGazeDirectionZ;RightEyePositionX;RightEyePositionY;RightEyePositionZ;RightPupilIrisDiameterRatio;RightPupilDiameterInMM;RightIrisDiameterInMM;RightEyeOpenness;FocusDistance;FocusStability;Condition;Scene;Task;GazedObject;ClickedObject;QuizAnswer;ChatBot
    public const string HeadPositionX = "HeadPositionX";
    public const string HeadPositionY = "HeadPositionY";
    public const string HeadPositionZ = "HeadPositionZ";
    public const string HeadDirectionX = "HeadDirectionX";
    public const string HeadDirectionY = "HeadDirectionY";
    public const string HeadDirectionZ = "HeadDirectionZ";
    public const string CombinedGazeForwardX = "CombinedGazeForwardX";
    public const string CombinedGazeForwardY = "CombinedGazeForwardY";
    public const string CombinedGazeForwardZ = "CombinedGazeForwardZ";
    public const string LeftEyeStatus = "LeftEyeStatus";
    public const string LeftEyePositionX = "LeftEyePositionX";
    public const string LeftEyePositionY = "LeftEyePositionY";
    public const string LeftEyePositionZ = "LeftEyePositionZ";
    public const string LeftGazeDirectionX = "LeftGazeDirectionX";
    public const string LeftGazeDirectionY = "LeftGazeDirectionY";
    public const string LeftGazeDirectionZ = "LeftGazeDirectionZ";
    public const string RightEyeStatus = "RightEyeStatus";
    public const string RightEyePositionX = "RightEyePositionX";
    public const string RightEyePositionY = "RightEyePositionY";
    public const string RightEyePositionZ = "RightEyePositionZ";
    public const string RightGazeDirectionX = "RightGazeDirectionX";
    public const string RightGazeDirectionY = "RightGazeDirectionY";
    public const string RightGazeDirectionZ = "RightGazeDirectionZ";
}


