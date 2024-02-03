using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class CustomBullet : NetworkBehaviour
{
    public Rigidbody rb;
    public GameObject explosion;
    public LayerMask whatIsEnemies;

    [Range(0f, 1f)]
    public float bounciness;
    public bool useGravity;

    public int explosionDamage;
    public float explosionRange;

    public int maxCollisions;
    public float maxLifetime;
    public bool explodeOnTouch = true;

    int collisions;
    PhysicMaterial physics_mat;

    private int teamId;
    private ulong shooterClient;
    private string shooterName;
    private AudioManager am;
    public AudioClip hitSound;
    public RawImage confirmHit;
    public float hitSoundVolume;

    public GameObject shootermanshootermanyeahthatsthem;
    public bool shouldTeleport = false;

    private Vector3 xc, yc, movement;

    private void Start() {
        Setup();
        confirmHit = GameObject.Find("Confirm Hit").GetComponent<RawImage>();
        am = GameObject.Find("AudioManager").GetComponent<AudioManager>();
        confirmHit.canvasRenderer.SetAlpha(0);
        
    }

    private void Update() {
        UpdatePosServerRpc();
        //UpdatePosClientRpc();
        if (collisions > maxCollisions) Explode();

        maxLifetime -= Time.deltaTime;
        if (maxLifetime <= 0) Explode();
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdatePosServerRpc() {
        Vector3 velocity = (xc + yc);
        rb.velocity = velocity;
    }

    [ClientRpc]
    private void UpdatePosClientRpc() {
        movement = (xc + yc) * Time.deltaTime;
        Debug.Log("MOVc: " + movement);
        GetComponent<Rigidbody>().MovePosition(transform.position + movement);
    }

    private void OnCollisionEnter(Collision collision) {
        if (collision.collider.CompareTag("Bullet")) return;

        collisions++;

        if (explodeOnTouch) Explode();
    }

    public void SetShooterStats(int teamId, ulong shooterClient, string shooterName) {
        this.teamId = teamId;
        this.shooterClient = shooterClient;
        this.shooterName = shooterName;
    }

    public void SetBulletStats(Vector3 dir, Vector3 xcomp, Vector3 ycomp) {
        Vector3 directionWithoutSpread;
        transform.forward = dir;
        Debug.Log(xcomp);
        xc = xcomp;
        yc = ycomp;

        // Send a network message containing the movement components
        SetBulletStatsClientRpc(xcomp, ycomp);
    }

    [ClientRpc]
    private void SetBulletStatsClientRpc(Vector3 xcomp, Vector3 ycomp) {
        // Update the movement components on the client side
        xc = xcomp;
        yc = ycomp;
    }


    private void Explode() {
        if (explosion != null) Instantiate(explosion, transform.position, Quaternion.identity);

        Collider[] enemies = Physics.OverlapSphere(transform.position, explosionRange, whatIsEnemies);
        if (enemies.Length > 0) Debug.Log(enemies[0].gameObject.name);

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
                    hitComponent.TakeDamageServerRpc(explosionDamage, teamId, shooterClient, shooterName);
                }
            }
        }

        //Teleport shooterman
        if(shouldTeleport) GameObject.Find("Controller").GetComponent<PlayerController>().TeleportClientRpc(transform.position, shooterClient);

        Invoke("Delay", 0.05f);
    }

    private void Delay() {
        try {
            if (IsServer) gameObject.GetComponent<NetworkObject>().Despawn(true);
        } catch (SpawnStateException e) {
            Debug.Log("SS: " + e);
        }
    }


    private void Setup() {
        physics_mat = new PhysicMaterial();
        physics_mat.bounciness = bounciness;
        physics_mat.frictionCombine = PhysicMaterialCombine.Minimum;
        physics_mat.bounceCombine = PhysicMaterialCombine.Maximum;

        GetComponent<SphereCollider>().material = physics_mat;
        rb.useGravity = useGravity;
    }
}
