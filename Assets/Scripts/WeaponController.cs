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
        CancelInvoke();
        readyToShoot = true;
        shooting = false;
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
                Shootable hitComponent = enemies[i].GetComponent<Shootable>();

                if(hitComponent != null) {
                    bool shouldTakeDamage = enemies[i].CompareTag("Enemy") || enemies[i].CompareTag("Objective")
                        || (hitComponent.OwnerClientId != OwnerClientId && enemies[i].CompareTag("Player"));
                    if (shouldTakeDamage) {
                        am.PlaySoundEffect(hitSound, hitSoundVolume);
                        confirmHit.CrossFadeAlpha(0.35f, 0f, false);
                        confirmHit.CrossFadeAlpha(0f, 0.3f, false);
                        hitComponent.TakeDamageServerRpc(damage/2, teamId, OwnerClientId, transform.parent.gameObject.name, transform.position);
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
                    Shootable hitComponent = rayHit.collider.GetComponent<Shootable>();
                    bool shouldTakeDamage = rayHit.collider.CompareTag("Enemy") || rayHit.collider.CompareTag("Objective")
                        || (hitComponent.OwnerClientId != OwnerClientId && rayHit.collider.CompareTag("Player"));
                    if (shouldTakeDamage) {
                        am.PlaySoundEffect(hitSound, hitSoundVolume);
                        confirmHit.CrossFadeAlpha(0.35f, 0f, false);
                        confirmHit.CrossFadeAlpha(0f, 0.3f, false);
                        hitComponent.TakeDamageServerRpc(damage, teamId, OwnerClientId, transform.parent.gameObject.name, transform.position);
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

            SpawnBulletOnServerRpc(ray, true);

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
                    Shootable hitComponent = rayHit.collider.GetComponent<Shootable>();
                    bool shouldTakeDamage = rayHit.collider.CompareTag("Enemy") || rayHit.collider.CompareTag("Objective")
                        || (hitComponent.OwnerClientId != OwnerClientId && rayHit.collider.CompareTag("Player"));
                    if (shouldTakeDamage) {
                        am.PlaySoundEffect(hitSound, hitSoundVolume);
                        confirmHit.CrossFadeAlpha(0.35f, 0f, false);
                        confirmHit.CrossFadeAlpha(0f, 0.3f, false);
                        hitComponent.TakeDamageServerRpc(damage, teamId, OwnerClientId, transform.parent.gameObject.name, transform.position);
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

    [ServerRpc(RequireOwnership = false)]
    private void SpawnBulletOnServerRpc(Ray ray, bool isSub) {
        RaycastHit hit;
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit)) targetPoint = ray.GetPoint(75); //targetPoint = hit.point;
        else targetPoint = ray.GetPoint(75);

        Vector3 directionWithoutSpread = targetPoint - attackPoint.position;

        GameObject currentBullet;
        if (isSub) currentBullet = Instantiate(subBullet, attackPoint.position, Quaternion.identity);
        else currentBullet = Instantiate(bullet, attackPoint.position, Quaternion.identity);

        NetworkObject netObj = currentBullet.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.SpawnWithOwnership(OwnerClientId, true);
            Debug.Log("Bullet spawned successfully.");
        }

        Debug.Log(directionWithoutSpread);

        currentBullet.GetComponent<CustomBullet>().SetShooterStats(teamId, OwnerClientId, transform.parent.gameObject.name);
        currentBullet.GetComponent<CustomBullet>().SetBulletStats(directionWithoutSpread.normalized, directionWithoutSpread.normalized * shootForce, cam.transform.up * upwardForce);
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
        if (!IsOwner || !hasControl) return;

        ShotInput();
        AimInput();

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
