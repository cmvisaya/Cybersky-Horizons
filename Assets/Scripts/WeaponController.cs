using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

/*
 * Manages weapon functionality including shooting, reloading, and aiming.
 * Controls bullet mechanics, handles HUD updates, and interacts with other game systems.
 */
public class WeaponController : NetworkBehaviour
{
    // References to player controller and weapon game object
    public PlayerController pc;
    public GameObject weapon;

    // Seats for weapon positioning
    public Transform inactiveSeat, activeSeat;
    public CameraController cc;
    public float bulletTimeGravity = 1.2f;

    // Gun statistics and configurations
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

    // Bullet statistics
    public GameObject bullet;
    public GameObject subBullet;
    public float shootForce, upwardForce;
    public bool subShooting, subEnabled, allowSubButtonHold = false;

    public int kills = 0;
    public bool isMelee = false;

    // Reload bullets to full magazine
    public void LoadBullets() {
        LoadBullets(bulletsPerReload);
    }

    // Reset bullet-related variables
    public void ResetBullets() {
        CancelInvoke();
        readyToShoot = true;
        shooting = false;
        bulletsLeft = 0;
    }

    // Get the number of bullets left
    public int GetBullets() {
        return bulletsLeft;
    }

    // Load a specified number of bullets into the weapon
    public void LoadBullets(int bullets) {
        am.PlaySoundEffect(reloadSound, 0.5f);
        bulletsLeft += bullets;
        if (bulletsLeft > magazineSize) bulletsLeft = magazineSize;
    }

    // Handle shooting and sub-shooting inputs
    private void ShotInput() {
        if (allowButtonHold) shooting = Input.GetButton("Fire1");
        else shooting = Input.GetButtonDown("Fire1");

        if (allowSubButtonHold) subShooting = Input.GetButton("Sub1") && !shooting && subEnabled;
        else subShooting = Input.GetButtonDown("Sub1") && !shooting && subEnabled;

        if (Input.GetButtonUp("Fire1")) dryFireSoundPlayed = false;

        // Perform shooting or sub-shooting if conditions are met
        if (readyToShoot && shooting && !reloading) {
            if(bulletsLeft > 0) {
                bulletsShot = bulletsPerTap;
                Shoot();
            } else {
                if (!dryFireSoundPlayed) am.PlaySoundEffect(dryFireSound, 0.5f);
                dryFireSoundPlayed = true;
            }
        }

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

    // Check if a hit object belongs to a specified layer
    private bool CompareLayer(RaycastHit hit, string layerName) {
        return hit.transform.gameObject.layer == LayerMask.NameToLayer(layerName);
    }

    // Handle normal shooting mechanics
    private void Shoot() {
        readyToShoot = false;

        // Calculate spread for shot
        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);

        RaycastHit hit;
        
        if(isMelee) {
            // Perform melee attack
            Collider[] enemies = Physics.OverlapSphere(attackPoint.position, range, whatIsEnemy);
            for (int i = 0; i < enemies.Length; i++) {
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
        } else {
            // Perform raycast shooting
            Vector3 direction = cam.transform.forward + new Vector3(x, y, 0);

            RaycastHit[] hits;
            hits = Physics.RaycastAll(cam.transform.position, direction, range, whatIsEnemy);
            bool hitWall = false;

            // Check for wall collisions
            if(Physics.Raycast(cam.transform.position, direction, out hit, range, whatIsWall)) {
                hitWall = CompareLayer(hit, "Walls") || CompareLayer(hit, "Default");
                Debug.Log("Hitwall calc: " + hit.collider.gameObject.name);
            }

            if (!hitWall && hits.Length > 0) {
                foreach (RaycastHit rayHit in hits) {
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

        // Apply screen shake effect
        cc.ShakeCameras(shakeIntensity, shakeTime);

        // Update weapon position and instantiate muzzle flash
        SetSeat(activeSeat);
        Instantiate(muzzleFlash, attackPoint.position, Quaternion.identity);

        // Play shot sound effect
        am.PlayGlobalSoundEffectServerRpc(0, transform.position, shotSoundVolume);
        bulletsLeft--;
        bulletsShot--;

        // Schedule next shot or reset shooting state
        if (bulletsShot <= 0) Invoke("ResetShot", timeBetweenShooting);
        if (bulletsShot > 0 && bulletsLeft > 0) Invoke("Shoot", timeBetweenShots);
    }

    // Handle sub-bullet shooting mechanics
    private void ShootSub() {
        readyToShoot = false;

        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);

        RaycastHit hit;
        
        if(subBullet != null) {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            SpawnBulletOnServerRpc(ray, true);
        } else {
            Vector3 direction = cam.transform.forward + new Vector3(x, y, 0);

            RaycastHit[] hits;
            hits = Physics.RaycastAll(cam.transform.position, direction, range, whatIsEnemy);
            bool hitWall = false;

            if(Physics.Raycast(cam.transform.position, direction, out hit, range, whatIsWall)) {
                hitWall = CompareLayer(hit, "Walls") || CompareLayer(hit, "Default");
                Debug.Log("Hitwall calc: " + hit.collider.gameObject.name);
            }

            if (!hitWall && hits.Length > 0) {
                foreach (RaycastHit rayHit in hits) {
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

        cc.ShakeCameras(shakeIntensity, shakeTime);
        SetSeat(activeSeat);
        Instantiate(muzzleFlash, attackPoint.position, Quaternion.identity);
        am.PlayGlobalSoundEffectServerRpc(0, transform.position, shotSoundVolume);
        bulletsLeft--;
        bulletsShot--;

        if (bulletsShot <= 0) Invoke("ResetShot", timeBetweenShooting);
        if (bulletsShot > 0 && bulletsLeft > 0) Invoke("Shoot", timeBetweenShooting);
    }

    // Server-side method to spawn a bullet
    [ServerRpc(RequireOwnership = false)]
    private void SpawnBulletOnServerRpc(Ray ray, bool isSub) {
        RaycastHit hit;
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit)) targetPoint = ray.GetPoint(75);
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

        currentBullet.GetComponent<CustomBullet>().SetShooterStats(teamId, OwnerClientId, transform.parent.gameObject.name);
        currentBullet.GetComponent<CustomBullet>().SetBulletStats(directionWithoutSpread.normalized, directionWithoutSpread.normalized * shootForce, cam.transform.up * upwardForce);
    }

    // Reset shooting state
    private void ResetShot() {
        readyToShoot = true;
    }

    // Start reloading process
    private void Reload() {
        am.PlaySoundEffect(reloadSound, 1f);
        Invoke("ReloadFinished", reloadTime);
        reloading = true;
    }

    // Finish reloading process
    private void ReloadFinished() {
        bulletsLeft = magazineSize;
        reloading = false;
    }

    // Initialization
    private void Awake() {
        bulletsLeft = 0;
        readyToShoot = true;
        hasControl = true;
    }

    // Setup HUD and references
    private void Start() {
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

        // Update bullet count display
        if(boulettes != null) {
            if (!displayByShots) boulettes.text = "" + (bulletsLeft / bulletsPerTap) + "/" + (magazineSize / bulletsPerTap);
            else boulettes.text = "" + bulletsLeft + "/" + magazineSize;
        }
    }

    // Handle aiming input and adjustments
    private void AimInput() {
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

    // Adjust weapon's position and rotation to match the provided seat
    private void SetSeat(Transform seat) {
        weapon.transform.parent = seat;
        weapon.transform.localPosition = new Vector3(0f, 0f, 0f);
        weapon.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
    }

    // Display a notification message on the HUD
    public void DisplayHUDNotif(string message) {
        StartCoroutine(NotifDisplay(message));
    }

    // Coroutine for displaying and then hiding notification messages
    private IEnumerator NotifDisplay(string message) {
        hudNotif.text = message;
        yield return new WaitForSeconds(4f);
        hudNotif.text = "";
    }
}
