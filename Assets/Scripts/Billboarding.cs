using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Script that is attached to 3D Text/UI objects to always face the camera.
 * Dynamic billboarding causes text objects to always face towards the camera.
 * Static billboarding causes UI objects to always face in the direction the camera is looking.
 */
public class Billboarding : MonoBehaviour
{
    // Reference to the main camera in the scene
    private Camera cam;

    // Boolean flag to determine whether to use static or dynamic billboarding
    public bool useStaticBillboard;

    // Unity's Start method, called before the first frame update
    private void Start() {
        // Assign the main camera to the cam variable
        cam = Camera.main;
    }

    // Unity's LateUpdate method, called after all Update functions have been called
    private void LateUpdate() {
        // Check if dynamic billboarding is enabled
        if (!useStaticBillboard) {
            // Make the object face the camera by rotating its forward direction towards the camera's transform
            transform.LookAt(cam.transform);
        } else {
            // If static billboarding is enabled, match the object's rotation to the camera's rotation
            transform.rotation = cam.transform.rotation;
        }

        // Ensure the object's rotation only affects the y-axis, keeping it upright
        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
    }
}
