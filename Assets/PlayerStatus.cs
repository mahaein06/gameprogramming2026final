using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerStatus : MonoBehaviour
{
    private const int BulletDamage = 10;
    private const string BulletTag = "Bullet";
    private const string LoseSceneName = "LoseScene";

    [Header("HP")]
    public int maxHP = 100;
    public int currentHP = 100;
    public Slider hpSlider;

    [Header("Ammo")]
    public int maxAmmo = 10;
    public int currentAmmo = 10;
    public TMP_Text ammoText;

    private void Awake()
    {
        AutoBindUI();
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        currentAmmo = Mathf.Clamp(currentAmmo, 0, maxAmmo);
        UpdateHPUI();
        UpdateAmmoUI();
    }

    private void Start()
    {
        AutoBindUI();
        UpdateHPUI();
        UpdateAmmoUI();
    }

    private void AutoBindUI()
    {
        if (hpSlider == null)
        {
            GameObject hpObject = GameObject.Find("HPBar");
            if (hpObject != null)
            {
                hpSlider = hpObject.GetComponent<Slider>();
            }
        }

        if (ammoText == null)
        {
            GameObject ammoObject = GameObject.Find("AmmoText");
            if (ammoObject != null)
            {
                ammoText = ammoObject.GetComponent<TMP_Text>();
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;

        currentHP = Mathf.Max(0, currentHP - damage);
        UpdateHPUI();

        if (currentHP <= 0)
        {
            SceneManager.LoadScene(LoseSceneName);
        }
    }

    public void HealFull()
    {
        currentHP = maxHP;
        UpdateHPUI();
    }

    public void ReloadFull()
    {
        currentAmmo = maxAmmo;
        UpdateAmmoUI();
    }

    public bool UseAmmo()
    {
        if (currentAmmo <= 0)
        {
            UpdateAmmoUI();
            return false;
        }

        currentAmmo--;
        UpdateAmmoUI();
        return true;
    }

    private void UpdateHPUI()
    {
        if (hpSlider == null)
        {
            return;
        }

        hpSlider.minValue = 0;
        hpSlider.maxValue = maxHP;
        hpSlider.direction = Slider.Direction.LeftToRight;
        hpSlider.value = currentHP;
    }

    private void UpdateAmmoUI()
    {
        if (ammoText == null)
        {
            return;
        }

        ammoText.text = currentAmmo + "/" + maxAmmo;
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleBulletHit(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleBulletHit(collision.gameObject);
    }

    private void HandleBulletHit(GameObject hitObject)
    {
        if (hitObject == null || !hitObject.CompareTag(BulletTag)) return;

        TakeDamage(BulletDamage);
        Destroy(hitObject);
    }
}




