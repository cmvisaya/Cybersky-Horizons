using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }
    public Transform target;
    public Vector3 offset;
    public bool useOffsetValues;
    public float rotateSpeed;
    public Transform pivot;


    public float maxViewAngle;
    public float minViewAngle;


    [SerializeField] private float zoomLowBound = 2;
    [SerializeField] private float zoomHighBound = 10;

    public bool invertY;

    private float dv;

    public GameObject[] cams;
    [SerializeField] private CinemachineVirtualCamera[] vcams;
    private float shakeTimer;

    private void Awake() {
        Instance = this;
        vcams = new CinemachineVirtualCamera[cams.Length];
        for (int i = 0; i < cams.Length; i++) {
            vcams[i] = cams[i].GetComponent<CinemachineVirtualCamera>();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        AudioListener al = GetComponent<AudioListener>();
        if (AudioManager.Instance.al == null) {
            AudioManager.Instance.al = al;
            al.enabled = true;
        } else {
            al.enabled = false;
        }

        if(target != null) {
            if (!useOffsetValues)
            {
                offset = target.position - transform.position;
            }

            pivot.transform.position = target.transform.position;
            pivot.transform.parent = null;
        }
    }

    private void Update() {
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

    // Update is called once per frame
    void LateUpdate()
    {
        if(target != null) {
            pivot.transform.position = target.transform.position;

            float horizontal = Input.GetAxis("Mouse X") * rotateSpeed;

            pivot.Rotate(0, horizontal, 0, Space.World);
            //Debug.Log("X = " + pivot.rotation.eulerAngles.x + " | Y = " + pivot.rotation.eulerAngles.y);


            float vertical = Input.GetAxis("Mouse Y") * rotateSpeed;
            dv -= vertical;
            if (dv > maxViewAngle)
            {
                dv = maxViewAngle;
            }
            if (dv < minViewAngle)
            {
                dv = minViewAngle;
            }
            //Debug.Log("dvAngle = " + dv + " | dv = " + Mathf.Asin(dv * Mathf.Deg2Rad));


            if (invertY)
            {
                pivot.Rotate(vertical, 0, 0, Space.World);
            }
            else
            {
                pivot.Rotate(-vertical, 0, 0);
            }


            //Debug.Log("y = " + pivot.rotation.y + " | z = " + pivot.rotation.z);
            if (pivot.rotation.eulerAngles.x > maxViewAngle && pivot.rotation.eulerAngles.x < 180f)
            {
                pivot.rotation = Quaternion.Euler(maxViewAngle, pivot.rotation.eulerAngles.y, pivot.rotation.eulerAngles.z);
                //Debug.Log("Snapback up: y = " + pivot.rotation.y + " | z = " + pivot.rotation.z);
            }
            if (pivot.rotation.eulerAngles.x > 180f && pivot.rotation.eulerAngles.x < 360f + minViewAngle)
            {
                pivot.rotation = Quaternion.Euler(360f + minViewAngle, pivot.rotation.eulerAngles.y, pivot.rotation.eulerAngles.z);
                //Debug.Log("Snapback down: y = " + pivot.rotation.y + " | z = " + pivot.rotation.z);
            }

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

    public void ActivateCamera(int camID) {
        shakeTimer = 0f;
        for(int i = 0; i < cams.Length; i++) {
            if(camID == i) cams[i].SetActive(true);
            else cams[i].SetActive(false);
        }
    }

    public void ShakeCameras(float intensity, float time) {
        shakeTimer = time;
        foreach(CinemachineVirtualCamera vcam in vcams) {
            CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin =
                vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = intensity;
        }
    }


}


