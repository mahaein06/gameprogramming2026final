using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyStatus : MonoBehaviour
{
    private const int BulletDamage = 10;
    private const string BulletTag = "Bullet";

    [SerializeField] private int maxHP = 100;
    [SerializeField] private int currentHP = 100;
    [SerializeField] private float deathDuration = 2f;
    [SerializeField] private float fallDegrees = 90f;
    [SerializeField] private Vector3 hpBarOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private Vector2 hpBarSize = new Vector2(1.4f, 0.16f);
    [SerializeField] private float hpBarBorder = 0.025f;

    [SerializeField] private Slider hpSlider;
    [SerializeField] private GameObject hpBarRoot;
    private Renderer[] renderers;
    private bool isDead;

    public bool IsDead => isDead;

    public void EnsureEditableHPBar()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        EnsureHPBar();
        SetHPBarVisible(true);
        UpdateHPUI();
    }

    public void SetEnemyHPBarVisible(bool visible)
    {
        SetHPBarVisible(visible);
    }
    private void Awake()
    {
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        renderers = GetComponentsInChildren<Renderer>(true);
        EnsureHPBar();
        UpdateHPUI();
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            ResetEnemy();
        }
    }

    private void LateUpdate()
    {
        UpdateHPBarPose();
    }

    public void ResetEnemy()
    {
        StopAllCoroutines();
        isDead = false;
        currentHP = maxHP;
        renderers = GetComponentsInChildren<Renderer>(true);
        EnsureHPBar();
        RestoreRendererAlpha();
        SetHPBarVisible(true);
        UpdateHPUI();
    }

    public void TakeDamage(int damage)
    {
        if (!enabled || isDead || damage <= 0) return;

        currentHP = Mathf.Max(0, currentHP - damage);
        UpdateHPUI();

        if (currentHP <= 0)
        {
            StartCoroutine(DieRoutine());
        }
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
        if (!enabled || isDead || hitObject == null || !hitObject.CompareTag(BulletTag)) return;
        if (hitObject.GetComponent<EnemyBullet>() != null) return;

        TakeDamage(BulletDamage);
        Destroy(hitObject);
    }

    private IEnumerator DieRoutine()
    {
        if (isDead) yield break;
        isDead = true;
        DisableDeathBehaviours();
        SetHPBarVisible(false);
        EnemyKillTracker.NotifyEnemyKilled(this);

        Vector3 pivot = CalculateFootPivot();
        Vector3 fallAxis = transform.forward;
        float elapsed = 0f;
        float rotated = 0f;

        while (elapsed < deathDuration)
        {
            float nextElapsed = Mathf.Min(deathDuration, elapsed + Time.deltaTime);
            float t = deathDuration > 0f ? nextElapsed / deathDuration : 1f;
            float targetAngle = Mathf.SmoothStep(0f, fallDegrees, t);
            float deltaAngle = targetAngle - rotated;

            transform.RotateAround(pivot, fallAxis, -deltaAngle);
            rotated = targetAngle;
            elapsed = nextElapsed;

            SetRendererAlpha(1f - t);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    private void DisableDeathBehaviours()
    {
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.enabled = false;
        }

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }
    }
    private Vector3 CalculateFootPivot()
    {
        Bounds bounds = GetRendererBounds();
        Vector3 right = transform.right;
        float lowestY = bounds.min.y;
        float bestScore = float.NegativeInfinity;
        Vector3 bestPoint = bounds.center;
        bool found = false;

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            Vector3 point = child.position;
            if (point.y > lowestY + 0.25f) continue;

            float rightScore = Vector3.Dot(point - bounds.center, right);
            float floorScore = 1f - Mathf.Abs(point.y - lowestY);
            float score = rightScore + floorScore;
            if (!found || score > bestScore)
            {
                bestScore = score;
                bestPoint = point;
                found = true;
            }
        }

        if (found)
        {
            bestPoint.y = lowestY;
            return bestPoint;
        }

        return bounds.center + right * bounds.extents.x + Vector3.down * bounds.extents.y;
    }

    private Bounds GetRendererBounds()
    {
        Renderer[] currentRenderers = renderers != null && renderers.Length > 0 ? renderers : GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = new Bounds(transform.position, Vector3.one);

        foreach (Renderer renderer in currentRenderers)
        {
            if (renderer == null || renderer.GetComponentInParent<Canvas>() != null) continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return bounds;
    }

    private void EnsureHPBar()
    {
        if (hpSlider != null && hpBarRoot == null)
        {
            hpBarRoot = hpSlider.gameObject;
        }

        if (hpSlider == null || hpBarRoot == null)
        {
            GameObject source = GameObject.Find("HPBar");
            if (source != null)
            {
                hpBarRoot = Instantiate(source);
                hpBarRoot.name = $"{name}_EnemyHPBar";
                hpBarRoot.transform.SetParent(transform, false);
                hpSlider = hpBarRoot.GetComponent<Slider>();
            }
        }

        if (hpSlider == null || hpBarRoot == null)
        {
            hpBarRoot = new GameObject($"{name}_EnemyHPBar", typeof(RectTransform), typeof(Canvas), typeof(Slider));
            hpBarRoot.transform.SetParent(transform, false);
            hpSlider = hpBarRoot.GetComponent<Slider>();
            CreateSliderVisuals(hpBarRoot.transform, hpSlider);
        }

        Canvas canvas = hpBarRoot.GetComponent<Canvas>();
        if (canvas == null) canvas = hpBarRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        RectTransform rect = hpBarRoot.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = hpBarSize;
            rect.localScale = Vector3.one;
        }

        RepairSliderVisuals();

        hpSlider.minValue = 0;
        hpSlider.maxValue = maxHP;
        hpSlider.direction = Slider.Direction.LeftToRight;
        SetFillColor(Color.red);
        UpdateHPBarPose();
    }
    private void CreateSliderVisuals(Transform root, Slider slider)
    {
        RectTransform rootRect = root.GetComponent<RectTransform>();
        if (rootRect != null)
        {
            rootRect.sizeDelta = hpBarSize;
        }

        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(root, false);
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = Color.black;
        backgroundImage.raycastTarget = false;

        GameObject fillAreaObject = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaObject.transform.SetParent(root, false);
        RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        ApplyFillAreaBorder(fillAreaRect);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(fillAreaObject.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillObject.GetComponent<Image>().color = Color.red;

        slider.targetGraphic = fillObject.GetComponent<Image>();
        slider.fillRect = fillRect;
    }
    private void ApplyFillAreaBorder(RectTransform fillAreaRect)
    {
        if (fillAreaRect == null) return;

        float border = Mathf.Max(0f, hpBarBorder);
        fillAreaRect.offsetMin = new Vector2(border, border);
        fillAreaRect.offsetMax = new Vector2(-border, -border);
    }

    private void RepairBackgroundVisual()
    {
        if (hpBarRoot == null) return;

        Transform background = hpBarRoot.transform.Find("Background");
        if (background == null)
        {
            GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(hpBarRoot.transform, false);
            backgroundObject.transform.SetAsFirstSibling();
            background = backgroundObject.transform;
        }

        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        if (backgroundRect != null)
        {
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            backgroundRect.localScale = Vector3.one;
        }

        Image backgroundImage = background.GetComponent<Image>();
        if (backgroundImage == null) backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.enabled = true;
        backgroundImage.color = Color.black;
        backgroundImage.raycastTarget = false;
    }
    private void RepairSliderVisuals()
    {
        if (hpBarRoot == null || hpSlider == null) return;

        RectTransform rootRect = hpBarRoot.GetComponent<RectTransform>();
        RepairBackgroundVisual();
        Transform fillArea = hpBarRoot.transform.Find("Fill Area");
        if (fillArea == null)
        {
            GameObject fillAreaObject = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaObject.transform.SetParent(hpBarRoot.transform, false);
            fillArea = fillAreaObject.transform;
        }

        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        if (fillAreaRect != null)
        {
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            ApplyFillAreaBorder(fillAreaRect);
        }

        Transform fill = fillArea.Find("Fill");
        if (fill == null)
        {
            foreach (Image image in hpBarRoot.GetComponentsInChildren<Image>(true))
            {
                if (image.name == "Fill")
                {
                    fill = image.transform;
                    fill.SetParent(fillArea, false);
                    break;
                }
            }
        }

        if (fill == null)
        {
            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(fillArea, false);
            fill = fillObject.transform;
        }

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        if (fillRect != null)
        {
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.localScale = Vector3.one;
        }

        Image fillImage = fill.GetComponent<Image>();
        if (fillImage == null) fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.enabled = true;
        fillImage.color = Color.red;
        fillImage.raycastTarget = false;

        hpSlider.fillRect = fillRect;
        hpSlider.targetGraphic = fillImage;

        if (rootRect != null)
        {
            rootRect.sizeDelta = hpBarSize;
        }
    }
    private void SetFillColor(Color color)
    {
        if (hpSlider == null || hpSlider.fillRect == null) return;

        Image fill = hpSlider.fillRect.GetComponent<Image>();
        if (fill != null)
        {
            fill.color = color;
        }
    }

    private void UpdateHPUI()
    {
        if (hpSlider == null) return;
        hpSlider.maxValue = maxHP;
        hpSlider.value = currentHP;
    }

    private void UpdateHPBarPose()
    {
        if (hpBarRoot == null) return;

        hpBarRoot.transform.position = transform.position + hpBarOffset;
        Camera camera = Camera.main;
        if (camera != null)
        {
            hpBarRoot.transform.rotation = Quaternion.LookRotation(hpBarRoot.transform.position - camera.transform.position, Vector3.up);
        }
    }

    private void SetHPBarVisible(bool visible)
    {
        if (hpBarRoot != null)
        {
            hpBarRoot.SetActive(visible);
        }
    }

    private void RestoreRendererAlpha()
    {
        SetRendererAlpha(1f);
    }

    private void SetRendererAlpha(float alpha)
    {
        if (renderers == null) return;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer.GetComponentInParent<Canvas>() != null) continue;

            foreach (Material material in renderer.materials)
            {
                if (material == null || !material.HasProperty("_Color")) continue;
                Color color = material.color;
                color.a = alpha;
                material.color = color;
            }
        }
    }
}









