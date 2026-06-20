using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatus : MonoBehaviour
{
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
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        currentAmmo = Mathf.Clamp(currentAmmo, 0, maxAmmo);
        UpdateHPUI();
        UpdateAmmoUI();
    }

    private void Start()
    {
        UpdateHPUI();
        UpdateAmmoUI();
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;

        currentHP = Mathf.Max(0, currentHP - damage);
        UpdateHPUI();
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
            Debug.LogWarning("PlayerStatus: HP Slider is not assigned.", this);
            return;
        }

        hpSlider.minValue = 0;
        hpSlider.maxValue = maxHP;
        hpSlider.value = currentHP;
    }

    private void UpdateAmmoUI()
    {
        if (ammoText == null)
        {
            Debug.LogWarning("PlayerStatus: Ammo Text is not assigned.", this);
            return;
        }

        ammoText.text = currentAmmo + "/" + maxAmmo;
    }
}
