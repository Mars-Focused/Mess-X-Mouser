
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

/// Thanks for downloading my projectile gun script! :D
/// Feel free to use it in any project you like!
/// 
/// The code is fully commented but if you still have any questions
/// don't hesitate to write a yt comment
/// or use the #coding-problems channel of my discord server
/// 
/// Dave
public class ProjectileGun : MonoBehaviour
{
    //bullet 
    public GameObject bullet;
    [SerializeField] GameObject ammoCounter;
    public AudioManagerBrackeys audioManager;
    public Collider playerCollider;
    Collider bulletCollider;

    //bullet force
    public float shootForce;
    public float upwardForce;

    //Gun stats
    public float timeBetweenBursts;
    public float reloadTime;
    public float timeBetweenShots;
    public int magazineSize;
    public int bulletsPerBurst;
    public float lowAmmoAmt;
    public bool magDump;
    public bool fullAuto;
    public bool multiBurst;

    public int bulletsLeft;
    int bulletsShotThisBurst;
    float passiveReloadTimer;

    //Recoil
    public Rigidbody playerRb;
    public float knockbackRecoil;

    //bools
    bool shooting;
    bool readyToShoot;
    bool firing;
    LayerMask raycastLayerMask;

    // bool reloading;

    //Reference
    public Camera fpsCam;
    public Transform attackPoint;
    public ProtagControls protagControls;
    public InputAction fire;

    public float floatMagSize;

    //Graphics
    public GameObject muzzleFlash; //TODO: Add Muzzle Flash Maybe

    //bug fixing :D
    public bool allowResetShot = true;

    private void Awake()
    {
        protagControls = new ProtagControls(); // <-Nessesary for NewInput Controller

        //make sure magazine is full
        bulletsLeft = magazineSize;
        readyToShoot = true;

        // Make LayerMask that Ignores Projectiles
        raycastLayerMask = LayerMask.GetMask("Default", "TransparentFX", "IgnoreRaycast", "Water", "UI", "Ground");
    }

    private void OnEnable()
    {
        fire = protagControls.Player.Fire;
        fire.Enable();
        fire.started += PullTrigger;
        fire.canceled += PullTrigger;
    }

    private void OnDisable()
    {
        fire.Disable();
    }

    private void Update()
    {
        FiringHandler();
        ammoCounter.GetComponent<TMPro.TMP_Text>().text = "" + bulletsLeft;
    }

    private int UsedBulletsPerBurst()
    {
        if (magDump) { return magazineSize; }
        else if (multiBurst) { return bulletsPerBurst; }
        else { return 1; }
    }

    private void PullTrigger(InputAction.CallbackContext context) // TODO: Change Input system to Handle Auto Vs Semi-Auto
    {
        if (context.started) 
        {
            firing = true;
        }

        if (context.canceled) 
        {
            firing = false;
        }
    }

    private void FiringHandler()
    {
        //Shooting
        if (readyToShoot && bulletsLeft > 0 && firing)
        {
            //Handles Burst
            bulletsShotThisBurst = 0;
            Shoot();

            if (firing && !fullAuto)
            {
                firing = false;
            }
        }
    }

    private void Shoot()
    {
        readyToShoot = false;

        //TODO: Look up if there is a "ResetInvokeTimer" Function.
        CancelInvoke("RefillAmmo");
        Invoke("RefillAmmo", reloadTime);

        //Find the exact hit position using a raycast
        Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); //Just a ray through the middle of your current view
        RaycastHit hit;

        //check if ray hits something
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, raycastLayerMask))
        {
            targetPoint = hit.point;
            //Debug.Log("We Hit " + hit.transform.name);
        }
        else
        {
            targetPoint = ray.GetPoint(75); //Just a point far away from the player
        }

        //Calculate direction from attackPoint to targetPoint
        Vector3 shotDirection = targetPoint - attackPoint.position;

        floatMagSize = magazineSize;

        if (bulletsLeft <= lowAmmoAmt)
        {
            audioManager.SetPitch("Player Shoot", 1.04f);
        }
        else 
        {
            audioManager.SetPitch("Player Shoot", 1f);
        }

        audioManager.Play("Player Shoot");

        //Instantiate bullet/projectile
        GameObject currentBullet = Instantiate(bullet, attackPoint.position, Quaternion.identity); //store instantiated bullet in currentBullet

        // Getting the collider of the bullet
        Collider bulletCollider = currentBullet.GetComponent<Collider>();

        // Ignoring collisions between the projectile and the player.
        Physics.IgnoreCollision(playerCollider, bulletCollider);

        //Rotate bullet to shoot direction
        currentBullet.transform.forward = shotDirection.normalized;

        //Add forces to bullet
        currentBullet.GetComponent<Rigidbody>().AddForce(shotDirection.normalized * shootForce, ForceMode.Impulse);
        currentBullet.GetComponent<Rigidbody>().AddForce(fpsCam.transform.up * upwardForce, ForceMode.Impulse);

        //Instantiate muzzle flash, if you have one
        if (muzzleFlash != null)
            Instantiate(muzzleFlash, attackPoint.position, Quaternion.identity);

        //Add recoil to player (should only be called once)
        // TODO: Add Toggle to Add Recoil on Shot or Burst
        playerRb.AddForce(-shotDirection.normalized * knockbackRecoil, ForceMode.Impulse);

        bulletsLeft--;
        bulletsShotThisBurst++;

        //Invoke resetShot function (if not already invoked), with your timeBetweenShooting
        if (allowResetShot && UsedBulletsPerBurst() == bulletsShotThisBurst || bulletsLeft <= 0)
        {
            allowResetShot = false;
            Invoke("ResetShot", timeBetweenBursts);
        }

        //if more than one bulletsPerTap make sure to repeat shoot function
        if (bulletsShotThisBurst < UsedBulletsPerBurst() && bulletsLeft > 0)
            Invoke("Shoot", timeBetweenShots);
    }
    private void ResetShot()
    {
        //Allow shooting another burst
        readyToShoot = true;
        allowResetShot = true;
    }

    private void RefillAmmo() 
    {
        audioManager.Play("Player Reload");
        bulletsLeft = magazineSize;
    }

}
