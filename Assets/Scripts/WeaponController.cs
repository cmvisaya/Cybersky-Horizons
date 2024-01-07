using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public PlayerController pc;
    public GameObject weapon;

    public Transform inactiveSeat;
    public Transform activeSeat;
    public GameObject aimCamera;
    public GameObject walkCamera;

    // Update is called once per frame
    void Update()
    {
        if(Input.GetButton("Fire2")) {
            if(pc.aiming == false) {
                pc.SnapAimModel();
            }
            pc.aiming = true;
            SetSeat(activeSeat);
            walkCamera.SetActive(false);
            aimCamera.SetActive(true);
        } else {
            pc.aiming = false;
            SetSeat(inactiveSeat);
            aimCamera.SetActive(false);
            walkCamera.SetActive(true);
        }
    }

    private void SetSeat(Transform seat) {
        weapon.transform.parent = seat;
        weapon.transform.localPosition = new Vector3(0f, 0f, 0f);
        weapon.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
    }
}
