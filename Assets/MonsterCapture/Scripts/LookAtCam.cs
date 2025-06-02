using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCam : MonoBehaviour
{
    Camera cameraToLookAt;

    private void Start()
    {
        cameraToLookAt = FindFirstObjectByType<Camera>();
    }
    private void Update()
    {
        Vector3 pos = cameraToLookAt.transform.position - transform.position;
        pos.x = pos.z = 0.0f;
        transform.LookAt(cameraToLookAt.transform.position - pos);
        transform.Rotate(0, 180, 0);
    }
}
