using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class WeaponController : NetworkBehaviour
{
    public PlayerController pc;
    public GameObject weapon;

    public Transform inactiveSeat;
    public Transform activeSeat;
    public CameraController cc;
    public float bulletTimeGravity = 1.2f;

    //Gun Stats
    public int damage;
    public float timeBetweenShooting, spread, range, reloadTime, timeBetweenShots;
    public int magazineSize, bulletsPerTap;
    public bool allowButtonHold;
    [SerializeField] int bulletsLeft, bulletsShot;
    [SerializeField] bool shooting, readyToShoot, reloading;
    public Camera cam;
    public Transform attackPoint;
    public GameObject muzzleFlash;
    public RaycastHit rayHit;
    public LayerMask whatIsEnemy;
    public AudioClip shotSound;
    public AudioClip reloadSound;
    public AudioClip dryFireSound;
    public RawImage crosshairs;

    private AudioManager am;

    public TextMeshProUGUI boulettes;

    public void LoadBullets(int bullets) {
        am.PlaySoundEffect(reloadSound, 0.5f);
        bulletsLeft += bullets;
        if (bulletsLeft > magazineSize) bulletsLeft = magazineSize;
    }

    private void ShotInput() {
        if (allowButtonHold) shooting = Input.GetButton("Fire1");
        else shooting = Input.GetButtonDown("Fire1");

        //if ((Input.GetButtonDown("Reload") && bulletsLeft <= magazineSize && !reloading) || (bulletsLeft == 0 && !reloading)) Reload();

        //Shoot
        if (readyToShoot && shooting && !reloading) {
            if(bulletsLeft > 0) {
                bulletsShot = bulletsPerTap;
                Shoot();
            } else {
                am.PlaySoundEffect(dryFireSound, 0.5f);
            }
        }
    }

    private void Shoot() {
        readyToShoot = false;

        //MAY HAVE TO MAKE ANIMATION POINT GUN FORWARDS

        //Spread
        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);
        Vector3 direction = cam.transform.forward + new Vector3(x, y, 0);

        //Raycast for shot - need to edit ray for third person - since its invisible i think we can just do it from the camera to the center... which actually this might be doing
        RaycastHit[] hits;
        hits = Physics.RaycastAll(cam.transform.position, direction, range, whatIsEnemy);
        if (hits.Length > 0) {
            foreach (RaycastHit rayHit in hits) {
                //Code for hit enemies to take damage
                Shootable hitComponent = rayHit.collider.GetComponent<Shootable>();
                if (rayHit.collider.CompareTag("Enemy") || (hitComponent.OwnerClientId != OwnerClientId && rayHit.collider.CompareTag("Player")))
                    hitComponent.TakeDamageServerRpc(damage);
            }
        }

        //Screenshake
        CameraController.Instance.ShakeCameras(2.25f, 0.1f);

        Instantiate(muzzleFlash, attackPoint.position, Quaternion.identity);

        am.PlayGlobalSoundEffectServerRpc(0, transform.position, 1f);
        bulletsLeft--;
        bulletsShot--;

        Invoke("ResetShot", timeBetweenShooting);

        if (bulletsShot > 0 && bulletsLeft > 0) Invoke("Shoot", timeBetweenShots);

    }

    private void ResetShot() {
        readyToShoot = true;
    }

    private void Reload() {
        am.PlaySoundEffect(reloadSound, 0.5f);
        Invoke("ReloadFinished", reloadTime);
        reloading = true;
    }

    private void ReloadFinished() {
        bulletsLeft = magazineSize;
        reloading = false;
    }

    private void Awake() {
        bulletsLeft = 0;
        readyToShoot = true;
    }

    private void Start() {
        //GameObject camera = GameObject.Find("Main Camera");
        //cc = camera.GetComponent<CameraController>();
        //cam = camera.GetComponent<Camera>();
        crosshairs = GameObject.Find("Crosshairs").GetComponent<RawImage>();
        boulettes = GameObject.Find("Bullets").GetComponent<TextMeshProUGUI>();
        am = GameObject.Find("AudioManager").GetComponent<AudioManager>();
        crosshairs.canvasRenderer.SetAlpha(0);
    }

    // Update is called once per frame
    private void Update()
    {
        if (!IsOwner) return;

        ShotInput();
        AimInput();

        if(boulettes != null) {
            boulettes.text = "" + bulletsLeft + "/" + magazineSize;
        }

    }

    private void AimInput() {
        //Edit the aiming code to tighten spread
        if(Input.GetButton("Fire2") && pc.aimEnabled) {
            if(!pc.aiming) {
                pc.moveDirection.y *= 0.2f;
                cc.ActivateCamera(1);
                crosshairs.CrossFadeAlpha(1.0f, 0.1f, false);
            }
            pc.aiming = true;
            pc.sprintEnabled = false;
            pc.jumpEnabled = false;
            pc.gravityScale = bulletTimeGravity;
            SetSeat(activeSeat);
        } else {
            if(pc.aiming) {
                cc.ActivateCamera(0);
                crosshairs.CrossFadeAlpha(0f, 0f, false);
            }
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
