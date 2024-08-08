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


    private bool isRotationStart = false;
    private float timerSec = 0f;
    private float rotationDuration = .5f;
    private float rotationRotDegree = 30f;
    private float previousRotDegree = 0f;
    private void Awake()
    {
        isRotationStart = true;

    }

    private void Update()
    {
        if (isRotationStart)
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
        }
    }

    private void RotateEyes(float degree,Vector3 axis)
    {
        float rot = Mathf.LerpAngle(0, degree, timerSec / rotationDuration);
        eyes.RotateEyes(rot, axis);
    }
}


