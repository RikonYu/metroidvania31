using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour
{
    public static UIController instance;
    public float HPLenUnit;
    public GameObject HPParent;
    public GameObject BossHPParent;
    GameObject HP, HPBG;
    GameObject BOSSHP;

    // Start is called before the first frame update
    private void Awake()
    {
        instance = this;
        HP = HPParent.transform.Find("hp").gameObject;
        HPBG = HPParent.transform.Find("bg").gameObject;
        BOSSHP = BossHPParent.transform.Find("hp").gameObject;
        SetBossHP(20, 50);
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

    }
    public void SetBossHP(float currenthp, float maxhp)
    {
        BossHPParent.SetActive(true);
        HP.GetComponent<RectTransform>().sizeDelta = new Vector2(HPLenUnit * currenthp, 100f);
        HPBG.GetComponent<RectTransform>().sizeDelta = new Vector2(HPLenUnit * maxhp, 100f);
        BOSSHP.GetComponent<RectTransform>().sizeDelta = new Vector2(1600f*currenthp/maxhp, 100f);
    }
}
