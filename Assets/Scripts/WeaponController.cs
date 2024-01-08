using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public PlayerController pc;
    public GameObject weapon;

    public Transform inactiveSeat;
    public Transform activeSeat;
    public CameraController cc;
    public float bulletTimeGravity = 1.2f;

    // Update is called once per frame
    void Update()
    {
        if(Input.GetButton("Fire2") && pc.aimEnabled) {
            if(!pc.aiming) {
                pc.moveDirection.y = 0f;
                cc.ActivateCamera(1);
            }
            pc.aiming = true;
            pc.sprintEnabled = false;
            pc.jumpEnabled = false;
            pc.gravityScale = bulletTimeGravity;
            SetSeat(activeSeat);
        } else {
            if(pc.aiming) cc.ActivateCamera(0);
            pc.aiming = false;
            pc.sprintEnabled = true;
            pc.jumpEnabled = true;
            pc.gravityScale = pc.storedGravityScale;
            SetSeat(inactiveSeat);
        }
    }

    private void SetSeat(Transform seat) {
        weapon.transform.parent = seat;
        weapon.transform.localPosition = new Vector3(0f, 0f, 0f);
        weapon.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
    }
}
