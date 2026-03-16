using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class UIController : MonoBehaviour
{
    public static UIController instance;

    public float HPLenUnit, EnergyLenUnit;
    public GameObject HPParent, AMParent;
    public GameObject BossHPParent;
    GameObject HP, HPBG, HPB;
    GameObject AM, AMBG, AMB;

    GameObject BOSSHP;

    public GameObject MinimapBG;

    public GameObject Bullet1Icon, Bullet2Icon, Bullet3Icon, Bullet4Icon;
    private GameObject[] bulletIconRoots;
    private Image[] bulletImages;
    private TMP_Text[] bulletTexts;

    public Image blackbg;

    // Start is called before the first frame update
    private void Awake()
    {
        instance = this;
        HP = HPParent.transform.Find("hp").gameObject;
        HPBG = HPParent.transform.Find("bg").gameObject;
        HPB = HPParent.transform.Find("border").gameObject;
        AM = AMParent.transform.Find("hp").gameObject;
        AMBG = AMParent.transform.Find("bg").gameObject;
        AMB = AMParent.transform.Find("border").gameObject;

        BOSSHP = BossHPParent.transform.Find("hp").gameObject;
        BossHPParent.SetActive(false);

        bulletIconRoots = new GameObject[4];
        bulletImages = new Image[4];
        bulletTexts = new TMP_Text[4];

        bulletIconRoots[0] = Bullet1Icon;
        bulletIconRoots[1] = Bullet2Icon;
        bulletIconRoots[2] = Bullet3Icon;
        bulletIconRoots[3] = Bullet4Icon;

        for (int i = 0; i < bulletIconRoots.Length; i++)
        {
            CacheBulletIconParts(i);
        }

        UpdateWeaponIcons(new bool[4], -1);
    }



    // Update is called once per frame
    void Update()
    {

    }
    public void ShowLose()
    {
        print("lose");
    }
    public void SetHP(float currenthp, float maxhp)
    {
        float safeMaxHP = Mathf.Max(0f, maxhp);
        float safeCurrentHP = Mathf.Clamp(currenthp, 0f, safeMaxHP);
        SetRectWidth(HP, HPLenUnit * safeCurrentHP, 100f);
        SetRectWidth(HPBG, HPLenUnit * safeMaxHP, 100f);
        SetRectWidth(HPB, HPLenUnit * safeMaxHP + 200f, 200f);
    }

    public void SetEnergy(float currentEnergy, float maxEnergy)
    {
        float safeMaxEnergy = Mathf.Max(0f, maxEnergy);
        float safeCurrentEnergy = Mathf.Clamp(currentEnergy, 0f, safeMaxEnergy);
        SetRectWidth(AM, EnergyLenUnit * safeCurrentEnergy, 100f);
        SetRectWidth(AMBG, EnergyLenUnit * safeMaxEnergy, 100f);
        SetRectWidth(AMB, EnergyLenUnit * safeMaxEnergy + 200f, 200f);
    }
    public void HideBossHP()
    {

        BossHPParent.SetActive(false);
    }
    public void ShowBossHP(string name, float maxhp)
    {

        BossHPParent.SetActive(true);
        BossHPParent.transform.Find("text").GetComponent<TMP_Text>().text = name;
        SetBossHPRevealScale(1f);
        SetBossHP(maxhp, maxhp);

    }
    public void SetBossHP(float currenthp, float maxhp)
    {
        float safeMaxHP = Mathf.Max(0.0001f, maxhp);
        float safeCurrentHP = Mathf.Clamp(currenthp, 0f, safeMaxHP);
        SetRectWidth(BOSSHP, 1600f * safeCurrentHP / safeMaxHP, 100f);
    }

    public void ToggleMinimap()
    {
        if (MinimapBG.activeSelf)
        {
            HideMinimap();
        }
        else
            ShowMinimap();
    }

    public void ShowMinimap()
    {
        MinimapBG.SetActive(true);
        Minimap.instance.gridParent.gameObject.SetActive(true);
    }

    public void HideMinimap()
    {
        MinimapBG.SetActive(false);
        Minimap.instance.gridParent.gameObject.SetActive(false);
    }

    public void UpdateWeaponIcons(bool[] unlockedWeapons, int equippedIndex)
    {
        if (bulletIconRoots == null)
        {
            return;
        }

        for (int i = 0; i < bulletIconRoots.Length; i++)
        {
            GameObject root = bulletIconRoots[i];
            if (root == null)
            {
                continue;
            }

            bool unlocked = unlockedWeapons != null && i < unlockedWeapons.Length && unlockedWeapons[i];
            root.SetActive(unlocked);

            if (!unlocked)
            {
                continue;
            }

            float alpha = i == equippedIndex ? 1f : 0.5f;
            SetGraphicAlpha(bulletImages[i], alpha);
            SetTextAlpha(bulletTexts[i], alpha);
        }
    }

    private void CacheBulletIconParts(int index)
    {
        GameObject root = bulletIconRoots[index];
        if (root == null)
        {
            return;
        }

        Transform imageTransform = root.transform.Find("Image");
        if (imageTransform != null)
        {
            bulletImages[index] = imageTransform.GetComponent<Image>();
        }

        Transform textTransform = root.transform.Find("Text(TMP)");
        if (textTransform == null)
        {
            textTransform = root.transform.Find("Text (TMP)");
        }

        if (textTransform != null)
        {
            bulletTexts[index] = textTransform.GetComponent<TMP_Text>();
        }
    }

    private void SetGraphicAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null)
        {
            return;
        }

        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }

    private void SetRectWidth(GameObject target, float width, float height)
    {
        if (target == null)
        {
            return;
        }

        RectTransform rect = target.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.sizeDelta = new Vector2(Mathf.Max(0f, width), height);
    }

    public void SetBossHPRevealScale(float scale)
    {
        if (BossHPParent == null)
        {
            return;
        }

        float clamped = Mathf.Clamp01(scale);
        BossHPParent.transform.localScale = new Vector3(clamped, 1f, 1f);
    }

}
