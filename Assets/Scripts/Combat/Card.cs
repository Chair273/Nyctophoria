using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Random = UnityEngine.Random;

public class Attack
{
    private List<String> behaviors;

    private Dictionary<String, object> info;
    private Dictionary<TargetType, List<Effect>> targetEffects;

    private float waitTime;

    public Attack(Dictionary<TargetType, List<Effect>> targetEffects)
    {
        this.targetEffects = targetEffects;
        waitTime = 0;
    }

    public Attack(float waitTime, Dictionary<TargetType, List<Effect>> targetEffects)
    {
        this.targetEffects = targetEffects;
        this.waitTime = waitTime;
    }

    public IEnumerator Activate(Character user)
    {
        info = new Dictionary<string, object>();

        info["TargetPosList"] = new List<Vector2Int>();
        info["User"] = user;

        foreach (TargetType targetType in targetEffects.Keys)
        {
            yield return user.StartCoroutine(targetType.GetTargetPos(user.GetGridPos(), callback =>
            {
                List<Vector2Int> targetPosList = (List<Vector2Int>)info["TargetPosList"];

                foreach (Vector2Int targetPos in callback)
                {
                    targetPosList.Add(targetPos);
                }
            }));

            foreach (Effect effect in targetEffects[targetType])
            {
                effect.Activate(this);

                if (effect.GetWaitTime() > 0)
                {
                    yield return new WaitForSeconds(effect.GetWaitTime());
                }
            }
        }

        user.usingCard = false;
    }

    public List<TargetType> GetTargetTypes()
    {
        List<TargetType> targetTypes = new List<TargetType>();

        foreach (TargetType targetType in targetEffects.Keys)
        {
            targetTypes.Add(targetType);
        }

        return targetTypes;
    }

    public Dictionary<string, object> GetInfo()
    {
        return info;
    }

    public void AddValue(string key, object value)
    {
        if (!info.ContainsKey(key))
        {
            info.Add(key, value);
        }
        else
        {
            info[key] = value;
        }
    }
}

public class Card : MonoBehaviour
{
    private List<GameObject> chainObjects = new List<GameObject>();

    private GameObject spikePrefab;

    private Transform frontCard;
    private Transform backCard;

    private Dictionary<string, string> cardInfo = new Dictionary<string, string>();

    private Attack attack;

    private Button button;

    private Character character;

    private Animator animator;

    private bool debounce = false;
    private bool visualDebounce = false;

    public bool move = false;

    public string GetName()
    {
        return cardInfo["Name"];
    }

    public void Awake()
    {
        spikePrefab = Resources.Load<GameObject>("CombatPrefabs/Gui/Spike");
    }

    public void New(Attack attack, Dictionary<string, string> cardInfo, Dictionary<string, float> cardSize, Character character)
    {
        this.attack = attack;
        this.cardInfo = cardInfo;
        this.character = character;

        transform.position = new Vector3(-7.25f, -3, 1);
        transform.rotation = Quaternion.Euler(0, 0, -10);

        gameObject.name = character.name + " " + cardInfo["Name"];
        animator = GetComponent<Animator>();

        frontCard = transform.Find("Root").Find("FrontCard");
        backCard = transform.Find("Root").Find("BackCard");

        button = frontCard.GetComponent<Button>();
        button.onClick.AddListener(() => StartCoroutine(Activate()));

        TextMeshProUGUI nameText = frontCard.Find("NameCanvas").Find("Name").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descriptionText = frontCard.Find("DescriptionCanvas").Find("Description").GetComponent<TextMeshProUGUI>();

        nameText.text = cardInfo["Name"];
        nameText.fontSize = cardSize["Name"];

        descriptionText.text = cardInfo["Description"];
        descriptionText.fontSize = cardSize["Description"];

        gameObject.SetActive(false);
    }

    public void Organize(List<Card> hand, int index)
    {
        float xPos = 9 * (index + 0.5f) / hand.Count - 4.5f;
        float yPos = -0.05f * Mathf.Pow(xPos, 2) - 3;
        float zPos = 0.15f * Mathf.Pow(xPos - 0.1f, 2) - 15;
        float rotation = -Mathf.Pow(xPos, 3) / 5;

        Vector3 newPos = new Vector3(xPos, yPos, zPos);

        gameObject.SetActive(true);

        StartCoroutine(Tween.New(newPos, transform, 0.25f));
        StartCoroutine(Tween.New(Quaternion.Euler(0, 0, rotation), transform, 0.25f));
    }

    public void OnMouseEnter()
    {
        if (!character.usingCard)
        {
            animator.SetBool("Selected", true);
            animator.Play("Select");

            StartCoroutine(Visualize());
        }
    }

    public void OnMouseExit()
    {
        if (!debounce)
        {
            StartCoroutine(RemoveChain());

            animator.SetBool("Selected", false);
            List<Card> hand = ((Player)character).GetHand();

            Organize(hand, hand.IndexOf(this));
        }
    }

    protected virtual IEnumerator Activate()
    {
        if (debounce || character.usingCard)
        {
            yield break;
        }

        Debug.Log(character.name + " used " + cardInfo["Name"] + ".");

        debounce = true;
        character.usingCard = true;

        StartCoroutine(RemoveChain());
        yield return StartCoroutine(attack.Activate(character));

        character.RemoveCard(this);
    }

    private IEnumerator Visualize()
    {
        if (visualDebounce || debounce)
        {
            yield break;
        }

        visualDebounce = true;

        List<Vector2Int> targetPosList = new List<Vector2Int>();

        foreach (TargetType targetType in attack.GetTargetTypes())
        {
            foreach (Vector2Int targetPos in targetType.GetHitPositions())
            {
                if (!targetPosList.Contains(targetPos))
                {
                    targetPosList.Add(targetPos);
                }
            }
        }

        Vector2Int gridPos = character.GetGridPos();

        for (int t = 0; t < targetPosList.Count; t++)
        {
            Vector2Int targetPos = targetPosList[t];

            if (targetPos.y + gridPos.y >= 0 && targetPos.y + gridPos.y <= 5)
            {
                Vector3 pos = CombatHandler.main.getNewPos(new Vector2Int(targetPos.x, targetPos.y + gridPos.y));

                GameObject chainObject = Instantiate(spikePrefab, pos, Quaternion.identity);
                SpriteRenderer chainRender = chainObject.GetComponent<SpriteRenderer>();

                chainObjects.Add(chainObject);

                chainObject.transform.position += new Vector3(0, Mathf.Abs(gridPos.x - targetPos.x) == 0 ? -1.5f : 3.2f, 0);
                chainObject.transform.rotation = Quaternion.Euler(0, 0, Mathf.Abs(gridPos.x - targetPos.x) == 1 ? 180 : 0);

                chainRender.color = new Color32(255, 0, 0, 0);

                StartCoroutine(Tween.New(new Color32(255, 255, 255, 255), chainRender, 0.25f));
                StartCoroutine(Tween.New(chainObject.transform.position + new Vector3(0, Mathf.Abs(gridPos.x - targetPos.x) == 0 ? 1 : -1.5f, 0), chainObject.transform, 0.25f));
            }
        }

        yield return new WaitForFixedUpdate();

        visualDebounce = false;
    }

    private IEnumerator RemoveChain()
    {
        foreach (GameObject chainObject in chainObjects)
        {
            Destroy(chainObject, debounce ? 0 : 0.5f);
            StartCoroutine(Tween.New(new Color32(0, 0, 0, 0), chainObject.GetComponent<SpriteRenderer>(), 0.25f));
        }

        chainObjects = new List<GameObject>();

        yield return new WaitForSeconds(0.5f);
    }

    public IEnumerator Burn()
    {
        Transform root = transform.Find("Root");

        Destroy(root.Find("BackCard").gameObject);

        Vector3 newPos = new Vector3(transform.position.x, -6, transform.position.z);

        StartCoroutine(Tween.New(new Color32(0, 0, 0, 255), root.Find("FrontCard").GetComponent<SpriteRenderer>(), 0.25f));
        StartCoroutine(Tween.New(newPos, transform, 1));
        StartCoroutine(Tween.New(Quaternion.Euler(0, 0, Random.Range(-4, 5) * 5), transform, 1));

        yield return new WaitForSeconds(0.3f);

        Destroy(root.Find("FrontCard").Find("NameCanvas").gameObject);
        Destroy(root.Find("FrontCard").Find("DescriptionCanvas").gameObject);

        StartCoroutine(Tween.New(new Color32(0, 0, 0, 0), root.Find("FrontCard").GetComponent<SpriteRenderer>(), 0.8f));

        yield return new WaitForSeconds(1.2f);

        gameObject.SetActive(false);
        Destroy(gameObject, 0.8f);
    }

    public IEnumerator Remove()
    {
        StartCoroutine(Tween.New(transform.position + new Vector3(0, 2.5f, 0), transform, 0.15f));

        yield return new WaitForSeconds(0.15f);

        StartCoroutine(Tween.New(new Vector3(13.5f, -2.5f, -1), transform, 0.5f));
        StartCoroutine(Tween.New(Quaternion.Euler(0, 0, -30), transform, 0.5f));
        StartCoroutine(Tween.New(new Color32(200, 200, 200, 0), frontCard.GetComponent<SpriteRenderer>(), 0.5f));

        Destroy(backCard.gameObject);
    }

    public IEnumerator Return()
    {
        transform.position = new Vector3(13.5f, -2.5f, -1);
        StartCoroutine(Tween.New(new Vector3(-10, -2.5f, -0.5f), transform, 0.5f));
        StartCoroutine(Tween.New(Quaternion.Euler(0, 0, 30), transform, 0.5f));
        StartCoroutine(Tween.New(new Color32(200, 200, 200, 255), frontCard.GetComponent<SpriteRenderer>(), 0.2f));

        yield return new WaitForSeconds(0.3f);

        StartCoroutine(Tween.New(new Color32(200, 200, 200, 0), frontCard.GetComponent<SpriteRenderer>(), 0.2f));

        yield return new WaitForSeconds(0.3f);

        Destroy(gameObject);
    }
}