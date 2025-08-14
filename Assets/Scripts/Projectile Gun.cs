
using UnityEngine;
using TMPro;

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

    //bullet force
    public float shootForce;
    public float upwardForce;

    //Gun stats
    public float timeBetweenBursts;
    public float reloadTime;
    public float timeBetweenShots;
    public int magazineSize;
    public int bulletsPerBurst;
    public bool allowButtonHold;

    public int bulletsLeft;
    int bulletsShotThisBurst;
    float passiveReloadTimer;

    //Recoil
    public Rigidbody playerRb;
    public float knockbackRecoil;

    //bools
    bool shooting;
    bool readyToShoot;
    LayerMask raycastLayerMask;

    // bool reloading;

    //Reference
    public Camera fpsCam;
    public Transform attackPoint;

    //Graphics
    public GameObject muzzleFlash; //TODO: Add Muzzle Flash Maybe
    //public TextMeshProUGUI ammunitionDisplay; //TODO: Add Amunition Display

    //bug fixing :D
    public bool allowResetShot = true;

    private void Awake()
    {
        //make sure magazine is full
        bulletsLeft = magazineSize;
        readyToShoot = true;

        // Make LayerMask that Ignores Projectiles
        raycastLayerMask = LayerMask.GetMask("Default", "TransparentFX", "IgnoreRaycast", "Water", "UI", "Ground");
    }

    private void Update()
    {
        OldInputMethod();
        ammoCounter.GetComponent<TMPro.TMP_Text>().text = "" + bulletsLeft;
    }

    private void OldInputMethod() //TODO: Change to the New Input System
    {
        //Check if allowed to hold down button and take corresponding input
        // TODO: Change Input system to Handle Auto Vs Semi-Auto
        if (allowButtonHold) shooting = Input.GetKey(KeyCode.Mouse0);
        else shooting = Input.GetKeyDown(KeyCode.Mouse0);

        //Shooting
        if (readyToShoot && shooting && bulletsLeft > 0)
        {
            //Handles Burst
            bulletsShotThisBurst = 0;
            Shoot();
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
            targetPoint = ray.GetPoint(3000); //Just a point far away from the player
        }

        //Calculate direction from attackPoint to targetPoint
        Vector3 shotDirection = targetPoint - attackPoint.position;

        //Instantiate bullet/projectile
        GameObject currentBullet = Instantiate(bullet, attackPoint.position, Quaternion.identity); //store instantiated bullet in currentBullet
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
        if (allowResetShot)
        {
            allowResetShot = false;
            Invoke("ResetShot", timeBetweenBursts);
        }

        //if more than one bulletsPerTap make sure to repeat shoot function
        if (bulletsShotThisBurst < bulletsPerBurst && bulletsLeft > 0)
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
        //Fill magazine
        bulletsLeft = magazineSize;
    }
}
