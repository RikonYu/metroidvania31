using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
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
        HP.GetComponent<RectTransform>().sizeDelta = new Vector2(HPLenUnit * currenthp, 100f);
        HPBG.GetComponent<RectTransform>().sizeDelta = new Vector2(HPLenUnit * maxhp, 100f);
        HPB.GetComponent<RectTransform>().sizeDelta = new Vector2(HPLenUnit * maxhp+200f, 200f);
    }

    public void SetEnergy(float currentEnergy, float maxEnergy)
    {
        AM.GetComponent<RectTransform>().sizeDelta = new Vector2(EnergyLenUnit * currentEnergy, 100f);
        AMBG.GetComponent<RectTransform>().sizeDelta = new Vector2(EnergyLenUnit * maxEnergy, 100f);
        AMB.GetComponent<RectTransform>().sizeDelta = new Vector2(EnergyLenUnit * maxEnergy + 200f, 200f);
    }
    public void HideBossHP()
    {

        BossHPParent.SetActive(false);
    }
    public void ShowBossHP(string name, float maxhp)
    {

        BossHPParent.SetActive(true);
        BossHPParent.transform.Find("text").GetComponent<TMP_Text>().text = name;
        SetBossHP(maxhp, maxhp);

    }
    public void SetBossHP(float currenthp, float maxhp)
    {
        HP.GetComponent<RectTransform>().sizeDelta = new Vector2(HPLenUnit * currenthp, 100f);
        HPBG.GetComponent<RectTransform>().sizeDelta = new Vector2(HPLenUnit * maxhp, 100f);
        BOSSHP.GetComponent<RectTransform>().sizeDelta = new Vector2(1600f * currenthp / maxhp, 100f);
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

}
