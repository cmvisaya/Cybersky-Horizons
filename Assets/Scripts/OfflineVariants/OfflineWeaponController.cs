using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

/*
 * Script that is attached to Players in singleplayer mode
 * Contains the same functionality as the online variant without the server-client interactions
 */

public class OfflineWeaponController : MonoBehaviour
{
    public OfflinePlayerController pc;
    public GameObject weapon;

    public Transform inactiveSeat, activeSeat;
    public CameraController cc;
    public float bulletTimeGravity = 1.2f;

    //Gun Stats
    public int damage;
    public float timeBetweenShooting, spread, range, reloadTime, timeBetweenShots;
    public int magazineSize, bulletsPerTap, bulletsPerReload;
    public bool allowButtonHold, allowSightSprinting, hasControl, displayByShots;
    [SerializeField] int bulletsLeft, bulletsShot;
    [SerializeField] bool shooting, readyToShoot, reloading;
    public Camera cam;
    public Transform attackPoint;
    public GameObject muzzleFlash;
    public RaycastHit rayHit;
    public LayerMask whatIsEnemy;
    public LayerMask whatIsWall;
    public AudioClip shotSound;
    public float shotSoundVolume, hitSoundVolume;
    public float shakeIntensity = 2.25f;
    public float shakeTime = 0.1f;
    public AudioClip reloadSound, dryFireSound, hitSound;
    private bool dryFireSoundPlayed;
    public RawImage crosshairs, confirmHit;

    private AudioManager am;

    public TextMeshProUGUI boulettes, hudNotif;

    public int teamId = -1;

    //Bullet stats
    public GameObject bullet;
    public GameObject subBullet;
    public float shootForce, upwardForce;
    public bool subShooting, subEnabled, allowSubButtonHold = false;

    public int kills = 0;
    public bool isMelee = false;

    public void LoadBullets() {
        LoadBullets(bulletsPerReload);
    }

    public void ResetBullets() {
        readyToShoot = true;
        bulletsLeft = 0;
    }

    public int GetBullets() {
        return bulletsLeft;
    }

    public void LoadBullets(int bullets) {
        am.PlaySoundEffect(reloadSound, 0.5f);
        bulletsLeft += bullets;
        if (bulletsLeft > magazineSize) bulletsLeft = magazineSize;
    }

    private void ShotInput() {
        if (allowButtonHold) shooting = Input.GetButton("Fire1");
        else shooting = Input.GetButtonDown("Fire1");

        if (allowSubButtonHold) subShooting = Input.GetButton("Sub1") && !shooting && subEnabled;
        else subShooting = Input.GetButtonDown("Sub1") && !shooting && subEnabled;

        if (Input.GetButtonUp("Fire1")) dryFireSoundPlayed = false;

        //if ((Input.GetButtonDown("Reload") && bulletsLeft <= magazineSize && !reloading) || (bulletsLeft == 0 && !reloading)) Reload();

        //Shoot
        if (readyToShoot && shooting && !reloading) {
            if(bulletsLeft > 0) {
                bulletsShot = bulletsPerTap;
                Shoot();
            } else {
                if (!dryFireSoundPlayed) am.PlaySoundEffect(dryFireSound, 0.5f);
                dryFireSoundPlayed = true;
            }
        }

        //SubShooting
        if (readyToShoot && subShooting && !reloading) {
            if(bulletsLeft > 0) {
                bulletsShot = bulletsPerTap;
                ShootSub();
            } else {
                if (!dryFireSoundPlayed) am.PlaySoundEffect(dryFireSound, 0.5f);
                dryFireSoundPlayed = true;
            }
        }

        pc.shooting = shooting || subShooting;
    }

    private bool CompareLayer(RaycastHit hit, string layerName) {
        return hit.transform.gameObject.layer == LayerMask.NameToLayer(layerName);
    }

    private void Shoot() {
        readyToShoot = false;

        //Spread
        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);

        RaycastHit hit;
        
        //With bullets now
        if(isMelee) {
            Collider[] enemies = Physics.OverlapSphere(attackPoint.position, range, whatIsEnemy);
            for (int i = 0; i < enemies.Length; i++) {
                //Code for hit enemies to take damage
                OfflineShootable hitComponent = enemies[i].GetComponent<OfflineShootable>();

                if(hitComponent != null) {
                    bool shouldTakeDamage = enemies[i].CompareTag("Enemy") || enemies[i].CompareTag("Objective")
                        || enemies[i].CompareTag("Player");
                    if (shouldTakeDamage) {
                        am.PlaySoundEffect(hitSound, hitSoundVolume);
                        confirmHit.CrossFadeAlpha(0.35f, 0f, false);
                        confirmHit.CrossFadeAlpha(0f, 0.3f, false);
                        hitComponent.TakeDamage(damage/2);
                    }
                }
            }
        } else { //Raycast bullets
            Vector3 direction = cam.transform.forward + new Vector3(x, y, 0);

            //Raycast for shot - need to edit ray for third person - since its invisible i think we can just do it from the camera to the center... which actually this might be doing
            RaycastHit[] hits;
            hits = Physics.RaycastAll(cam.transform.position, direction, range, whatIsEnemy);
            bool hitWall = false;

            //If hit returns an object tagged with wall or default, do not process hits. (This would mean the first object we hit was a wall);
            if(Physics.Raycast(cam.transform.position, direction, out hit, range, whatIsWall)) {
                hitWall = CompareLayer(hit, "Walls") || CompareLayer(hit, "Default");
                Debug.Log("Hitwall calc: " + hit.collider.gameObject.name);
            } //Must include player

            if (!hitWall && hits.Length > 0) {
                foreach (RaycastHit rayHit in hits) {
                    //Code for hit enemies to take damage
                    OfflineShootable hitComponent = rayHit.collider.GetComponent<OfflineShootable>();
                    bool shouldTakeDamage = rayHit.collider.CompareTag("Enemy") || rayHit.collider.CompareTag("Objective")
                        || rayHit.collider.CompareTag("Player");
                    if (shouldTakeDamage) {
                        am.PlaySoundEffect(hitSound, hitSoundVolume);
                        confirmHit.CrossFadeAlpha(0.35f, 0f, false);
                        confirmHit.CrossFadeAlpha(0f, 0.3f, false);
                        hitComponent.TakeDamage(damage);
                    }
                }
            }
        }

        //Debug.DrawRay(cam.transform.position, direction, Color.green, 5f);

        //Screenshake
        cc.ShakeCameras(shakeIntensity, shakeTime);

        SetSeat(activeSeat);
        Instantiate(muzzleFlash, attackPoint.position, Quaternion.identity);

        am.PlaySoundEffect(shotSound, shotSoundVolume);
        bulletsLeft--;
        bulletsShot--;

        if (bulletsShot <= 0) Invoke("ResetShot", timeBetweenShooting);

        if (bulletsShot > 0 && bulletsLeft > 0) Invoke("Shoot", timeBetweenShots);
    }

    private void ShootSub() {
        readyToShoot = false;

        //MAY HAVE TO MAKE ANIMATION POINT GUN FORWARDS

        //Spread
        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);

        RaycastHit hit;
        
        //With bullets now
        if(subBullet != null) {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        } else { //Raycast bullets
            Vector3 direction = cam.transform.forward + new Vector3(x, y, 0);

            //Raycast for shot - need to edit ray for third person - since its invisible i think we can just do it from the camera to the center... which actually this might be doing
            RaycastHit[] hits;
            hits = Physics.RaycastAll(cam.transform.position, direction, range, whatIsEnemy);
            bool hitWall = false;

            //If hit returns an object tagged with wall or default, do not process hits. (This would mean the first object we hit was a wall);
            if(Physics.Raycast(cam.transform.position, direction, out hit, range, whatIsWall)) {
                hitWall = CompareLayer(hit, "Walls") || CompareLayer(hit, "Default");
                Debug.Log("Hitwall calc: " + hit.collider.gameObject.name);
            } //Must include player

            if (!hitWall && hits.Length > 0) {
                foreach (RaycastHit rayHit in hits) {
                    //Code for hit enemies to take damage
                    OfflineShootable hitComponent = rayHit.collider.GetComponent<OfflineShootable>();
                    bool shouldTakeDamage = rayHit.collider.CompareTag("Enemy") || rayHit.collider.CompareTag("Objective")
                        || rayHit.collider.CompareTag("Player");
                    if (shouldTakeDamage) {
                        am.PlaySoundEffect(hitSound, hitSoundVolume);
                        confirmHit.CrossFadeAlpha(0.35f, 0f, false);
                        confirmHit.CrossFadeAlpha(0f, 0.3f, false);
                        hitComponent.TakeDamage(damage);
                    }
                }
            }
        }

        //Debug.DrawRay(cam.transform.position, direction, Color.green, 5f);

        //Screenshake
        cc.ShakeCameras(shakeIntensity, shakeTime);

        SetSeat(activeSeat);
        Instantiate(muzzleFlash, attackPoint.position, Quaternion.identity);

        am.PlayGlobalSoundEffectServerRpc(0, transform.position, shotSoundVolume);
        bulletsLeft--;
        bulletsShot--;

        if (bulletsShot <= 0) Invoke("ResetShot", timeBetweenShooting);

        if (bulletsShot > 0 && bulletsLeft > 0) Invoke("Shoot", timeBetweenShots);
    }

    private void ResetShot() {
        readyToShoot = true;
    }

    private void Reload() {
        am.PlaySoundEffect(reloadSound, 1f);
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
        
        //BLUE TEAM FOR TESTING PURPOSES
        //MAKE SURE TO GIVE PLAYER SHOOTABLE COMPONENT A PROPER TEAM ID
        //teamId = 1;
        hasControl = true;
    }

    private void Start() {
        //GameObject camera = GameObject.Find("Main Camera");
        //cc = camera.GetComponent<CameraController>();
        //cam = camera.GetComponent<Camera>();
        crosshairs = GameObject.Find("Crosshairs").GetComponent<RawImage>();
        confirmHit = GameObject.Find("Confirm Hit").GetComponent<RawImage>();
        boulettes = GameObject.Find("Bullets").GetComponent<TextMeshProUGUI>();
        hudNotif = GameObject.Find("HudNotif").GetComponent<TextMeshProUGUI>();
        am = GameObject.Find("AudioManager").GetComponent<AudioManager>();
        pc.cam = cam;
        crosshairs.canvasRenderer.SetAlpha(0);
        confirmHit.canvasRenderer.SetAlpha(0);
        hudNotif.text = "";
    }

    // Update is called once per frame
    private void Update()
    {
        if (GameManager.Instance.pauseMenu.activeSelf || GameManager.Instance.inLevelClear) {
            return;
        }

        if(hasControl) {
            ShotInput();
            AimInput();
        }

        if(boulettes != null) {
            if (!displayByShots) boulettes.text = "" + (bulletsLeft / bulletsPerTap) + "/" + (magazineSize / bulletsPerTap);
            else boulettes.text = "" + bulletsLeft + "/" + magazineSize;
        }

    }

    private void AimInput() {
        //Edit the aiming code to tighten spread
        if(Input.GetButton("Fire2") && pc.aimEnabled) {
            if(!pc.aiming) {
                pc.moveDirection.y *= 0.2f;
                cc.ActivateCamera(1);
                crosshairs.CrossFadeAlpha(1.0f, 0.1f, false);
                if (pc.grounded == false) am.PlaySoundEffect(5, 1f);
            }
            pc.aiming = true;
            if (!allowSightSprinting) pc.sprintEnabled = false;
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
            if(!shooting && readyToShoot) SetSeat(inactiveSeat);
        }
    }

    private void SetSeat(Transform seat) {
        weapon.transform.parent = seat;
        weapon.transform.localPosition = new Vector3(0f, 0f, 0f);
        weapon.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
    }

    public void DisplayHUDNotif(string message) {
        StartCoroutine(NotifDisplay(message));
    }

    private IEnumerator NotifDisplay(string message) {
        hudNotif.text = message;
        yield return new WaitForSeconds(4f);
        hudNotif.text = "";
    }
}
