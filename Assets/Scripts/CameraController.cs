using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

/*
 * Script that is attached to the main camera, which is a child of each player object.
 * Handles 3rd person camera control, rotating around the character with mouse movement as well as zoom effects for aiming down sights and increased fov for wall-running.
 */

public class CameraController : MonoBehaviour
{
    // The target that the camera will follow
    public Transform target;
    
    // Offset of the camera relative to the target
    public Vector3 offset;
    
    // Flag to determine whether to use the predefined offset values or calculate them based on the target's position
    public bool useOffsetValues;
    
    // Speed at which the camera rotates
    public float rotateSpeed;
    
    // Reference to the pivot point around which the camera will rotate
    public Transform pivot;

    // Maximum and minimum view angles for the camera's vertical rotation
    public float maxViewAngle;
    public float minViewAngle;

    // Lower and upper bounds for zoom levels (unused in this script)
    [SerializeField] private float zoomLowBound = 2;
    [SerializeField] private float zoomHighBound = 10;

    // Flag to determine whether the Y-axis input should be inverted
    public bool invertY;

    // Internal variable for tracking the vertical rotation delta
    private float dv;

    // Array of camera GameObjects that can be switched between
    public GameObject[] cams;
    
    // Array of Cinemachine virtual cameras corresponding to the camera GameObjects
    [SerializeField] private CinemachineVirtualCamera[] vcams;
    
    // Timer for controlling the duration of camera shake
    private float shakeTimer;

    // Unity's Awake method, called when the script instance is being loaded
    private void Awake() {
        // Initialize the vcams array and populate it with CinemachineVirtualCamera components from the camera GameObjects
        vcams = new CinemachineVirtualCamera[cams.Length];
        for (int i = 0; i < cams.Length; i++) {
            vcams[i] = cams[i].GetComponent<CinemachineVirtualCamera>();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Set up the AudioListener to work with the AudioManager. This is in the instance of multiple players to allow for global sounds.
        AudioListener al = GetComponent<AudioListener>();
        if (AudioManager.Instance.al == null) {
            AudioManager.Instance.al = al;
            al.enabled = true;
        } else {
            al.enabled = false;
        }

        // Initialize the camera's position and pivot based on the target
        if(target != null) {
            if (!useOffsetValues)
            {
                // Calculate the offset from the target's position
                offset = target.position - transform.position;
            }

            // Set the pivot's position to the target's position and unparent it to allow independent rotation
            // The pivot is the gameobject that is contained within the neck of the player and acts as a socket that the camera is attached to for rotating around.
            pivot.transform.position = target.transform.position;
            pivot.transform.parent = null;
        }
    }

    // Update is called once per frame
    private void Update() {
        // Handle camera shake timer and reset the shake intensity when the timer expires
        if (shakeTimer >= 0) {
            shakeTimer -= Time.deltaTime;
            if (shakeTimer <= 0f) {
                foreach(CinemachineVirtualCamera vcam in vcams) {
                    CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin =
                        vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                    cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = 0f;
                }
            }
        }
    }

    // LateUpdate is called after all Update functions have been called
    void LateUpdate()
    {
        if(target != null) {
            // Update the pivot's position to follow the target
            pivot.transform.position = target.transform.position;

            // Get horizontal input for rotation and rotate the pivot accordingly
            float horizontal = Input.GetAxis("Mouse X") * rotateSpeed;
            pivot.Rotate(0, horizontal, 0, Space.World);

            // Get vertical input for rotation and update the vertical delta
            float vertical = Input.GetAxis("Mouse Y") * rotateSpeed;
            dv -= vertical;

            // Clamp the vertical delta to the specified view angles
            if (dv > maxViewAngle)
            {
                dv = maxViewAngle;
            }
            if (dv < minViewAngle)
            {
                dv = minViewAngle;
            }

            // Apply vertical rotation based on the invertY setting
            if (invertY)
            {
                pivot.Rotate(vertical, 0, 0, Space.World);
            }
            else
            {
                pivot.Rotate(-vertical, 0, 0);
            }

            // Clamp the pivot's rotation to the specified view angles
            if (pivot.rotation.eulerAngles.x > maxViewAngle && pivot.rotation.eulerAngles.x < 180f)
            {
                pivot.rotation = Quaternion.Euler(maxViewAngle, pivot.rotation.eulerAngles.y, pivot.rotation.eulerAngles.z);
            }
            if (pivot.rotation.eulerAngles.x > 180f && pivot.rotation.eulerAngles.x < 360f + minViewAngle)
            {
                pivot.rotation = Quaternion.Euler(360f + minViewAngle, pivot.rotation.eulerAngles.y, pivot.rotation.eulerAngles.z);
            }

            // Adjust the camera's position based on the collected offset and rotation
            if (Mathf.Abs(dv) <= 1.0f) {
                float asinValue = Mathf.Asin(dv * Mathf.Deg2Rad);
                float desiredYAngle = pivot.eulerAngles.y;
                float desiredXAngle = pivot.eulerAngles.x;
                Quaternion rotation = Quaternion.Euler(desiredXAngle, desiredYAngle, 0);

                if(offset.magnitude != 0f) {
                    transform.position = (target.position + new Vector3(0, asinValue * offset.magnitude, 0)) - (rotation * offset);
                }
            }
        }
    }

    // Activates the specified camera by its ID and deactivates all others - this is for switching cameras based on specific zoom effects
    public void ActivateCamera(int camID) {
        shakeTimer = 0f;
        for(int i = 0; i < cams.Length; i++) {
            if(camID == i) cams[i].SetActive(true);
            else cams[i].SetActive(false);
        }
    }

    // Initiates camera shake with the specified intensity and duration
    // Shake timer is decremented and reset in the Update() method
    public void ShakeCameras(float intensity, float time) {
        shakeTimer = time;
        foreach(CinemachineVirtualCamera vcam in vcams) {
            CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin =
                vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = intensity;
        }
    }
}
