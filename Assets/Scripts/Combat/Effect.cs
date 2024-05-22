using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class Effect
{
    float waitTime;

    public Effect(float waitTime)
    {
        this.waitTime = waitTime;
    }

    public virtual void Activate(Attack attack)
    {
    }

    public float GetWaitTime()
    {
        return waitTime;
    }
}

public class ApplyStatus : Effect
{
    public static Dictionary<string, Type> statusEffects = new Dictionary<string, Type>
    {
        {"Guarded", typeof(Guarded)},
        {"Contagion", typeof(Contagion)},
        {"Oozed", typeof(Oozed)},
        {"Poison", typeof(Poison)}
    };

    private string statusIndex;

    private int amount;

    public ApplyStatus(string statusIndex, int amount) : base(0)
    {
        this.statusIndex = statusIndex;
        this.amount = amount;
    }

    public ApplyStatus(string statusIndex, int amount, float waitTime) : base(waitTime)
    {
        this.statusIndex = statusIndex;
        this.amount = amount;
    }

    public override void Activate(Attack attack)
    {
        Type statusType = statusEffects.ContainsKey(statusIndex) ? statusEffects[statusIndex] : null;

        if (statusType == null)
        {
            return;
        }

        ConstructorInfo cons = statusType.GetConstructor(new[] { typeof(int), typeof(Character) });

        Dictionary<String, object> info = attack.GetInfo();

        Character user = (Character)info["User"];

        List<Character> targetList = new List<Character>();
        List<Vector2Int> targetPosList = (List<Vector2Int>)info["TargetPosList"];

        foreach (Vector2Int targetPos in targetPosList)
        {
            targetList.Add(CombatHandler.main.GetCharacter(targetPos));
        }

        targetList.RemoveAll(target => target == null);

        foreach (Character target in targetList)
        {
            StatusEffect status = (StatusEffect)cons.Invoke(new object[] { amount, target });

            target.AddStatus(status);

            if (target.Equals(user))
            {
                Debug.Log(target.name + " applied " + status.ToString() + " to themself.");
            }
            else
            {
                Debug.Log(user.name + " inflicted " + status.ToString() + " on " + target.name + ".");

            }
        }
    }
}

public class ModifyStatus : Effect
{
    private Dictionary<String, int> effectList;

    public ModifyStatus(Dictionary<String, int> effectList, float waitTime) : base(waitTime)
    {
        this.effectList = effectList;
    }

    public ModifyStatus(Dictionary<String, int> effectList) : base(0)
    {
        this.effectList = effectList;
    }

    public override void Activate(Attack attack)
    {
        Dictionary<String, object> info = attack.GetInfo();

        List<Character> targetList = new List<Character>();
        List<Vector2Int> targetPosList = (List<Vector2Int>)info["TargetPosList"];

        foreach (Vector2Int targetPos in targetPosList)
        {
            targetList.Add(CombatHandler.main.GetCharacter(targetPos));
        }

        targetList.RemoveAll(target => target == null);

        foreach (Character target in targetList)
        {
            List<StatusEffect> targetStatus = target.GetStatus();

            if (targetStatus.Count == 0)
            {
                continue;
            }

            int i = 0;
            while (i < targetStatus.Count)
            {
                if (effectList.ContainsKey(targetStatus[i].type))
                {
                    if (targetStatus[i].Stack(effectList[targetStatus[i].type]))
                    {
                        i++;
                    }
                }
                else
                {
                    i++;
                }
            }
        }
    }
}

public class DamageDice : Effect
{
    private int amount;
    private int faces;
    private int modifier;

    public DamageDice(int amount, int faces, int modifier) : base(0)
    {
        this.amount = amount;
        this.faces = faces;
        this.modifier = modifier;
    }

    public DamageDice(int amount, int faces, int modifier, float waitTime) : base(waitTime)
    {
        this.amount = amount;
        this.faces = faces;
        this.modifier = modifier;
    }

    public override void Activate(Attack attack)
    {
        Dictionary<String, object> info = attack.GetInfo();

        Character user = (Character)info["User"];

        List<Vector2Int> targetPosList = (List<Vector2Int>)info["TargetPosList"];
        List<Character> targetList = new List<Character>();

        foreach (Vector2Int targetPos in targetPosList)
        {
            targetList.Add(CombatHandler.main.GetCharacter(targetPos));
        }

        targetList.RemoveAll(target => target == null);

        foreach (Character target in targetList)
        {
            int damage = modifier;

            for (int i = 0; i < amount; i++)
            {
                int currentDamage = Random.Range(1, faces + 1);
                damage += currentDamage;

                new DiceVFX(target, target.transform.Find("DiceContainer"), currentDamage, faces);
            }

            List<int> health = target.TakeDamage(damage);

            Debug.Log(user.GetName() + " dealt " + health[1] + " damage to " + target.GetName() + ". " + target.GetName() + " is now at " + health[0] + " health.");
        }
    }
}

public class MoveUser : Effect
{
    public MoveUser() : base(0) { }

    public MoveUser(float waitTime) : base(waitTime) { }

    public override void Activate(Attack attack)
    {
        Dictionary<String, object> info = attack.GetInfo();

        Character user = (Character)info["User"];
        List<Vector2Int> targetPosList = (List<Vector2Int>)info["TargetPosList"];

        if (targetPosList.Count != 1)
        {
            Debug.Log("Invalid amount of positions: " + targetPosList.Count);
            return;
        }

        user.Move(targetPosList[0], true, false);
    }
}

public class Weight : Effect
{
    private int speedMod;

    public Weight(int speedMod) : base(0)
    {
        this.speedMod = speedMod;
    }

    public Weight(int speedMod, float waitTime) : base(waitTime)
    {
        this.speedMod = speedMod;
    }

    public override void Activate(Attack attack)
    {
        Dictionary<string, object> info = attack.GetInfo();
        Character user = (Character)info["User"];

        user.AddSpeed(speedMod);
    }
}

public class ChangeExhaustionDC : Effect
{
    private int amount;

    public ChangeExhaustionDC(int amount) : base(0)
    {
        this.amount = amount;
    }

    public ChangeExhaustionDC(int amount, float waitTime) : base(waitTime)
    {
        this.amount = amount;
    }

    public override void Activate(Attack attack)
    {
        Dictionary<string, object> info = attack.GetInfo();
        Character user = (Character)info["User"];

        user.AddExhaustionDC(amount);
    }
}


public class RecordUserPos : Effect
{
    public RecordUserPos() : base(0) { }

    public override void Activate(Attack attack)
    {
        Character user = (Character)attack.GetInfo()["User"];
        attack.AddValue("PrevPos", (object)user.GetGridPos());
    }
}

public class ResetTargetList : Effect
{
    public ResetTargetList() : base(0) { }

    public override void Activate(Attack attack)
    {
        Character user = (Character)attack.GetInfo()["User"];
        attack.AddValue("TargetPosList", (object)new List<Vector2Int>());
    }
}

public class RetargetFromMovement : Effect
{
    private int magnitude;
    private int column;

    public RetargetFromMovement(int magnitude, int column) : base(0)
    {
        this.magnitude = magnitude;
        this.column = column;
    }

    public override void Activate(Attack attack)
    {
        Dictionary<String, object> info = attack.GetInfo();

        Character user = (Character)info["User"];
        Vector2Int userPos = user.GetGridPos();
        Vector2Int prevPos = (Vector2Int)info["PrevPos"];

        Vector2Int targetPos = new Vector2Int(Math.Abs(prevPos.x - column), userPos.y + Math.Sign(userPos.y - prevPos.y) * magnitude);
        Debug.Log(targetPos.x + " " + targetPos.y);


        if (targetPos.y >= 0 && targetPos.y <= 5)
        {
            attack.AddValue("TargetPosList", (object)new List<Vector2Int>() { targetPos });
        }
        else
        {
            attack.AddValue("TargetPosList", (object)new List<Vector2Int>() { });
        }

    }
}

public class WaitEffect : Effect
{
    public WaitEffect(float waitTime) : base(waitTime) { }
}


public class PokeVFX : Effect
{
    string spriteID;

    public PokeVFX(string spriteID) : base(0)
    {
        this.spriteID = spriteID;
    }

    public PokeVFX(string spriteID, float waitTime) : base(waitTime)
    {
        this.spriteID = spriteID;
    }

    public override void Activate(Attack attack)
    {
        Dictionary<String, object> info = attack.GetInfo();

        List<Vector2Int> targetPosList = (List<Vector2Int>)info["TargetPosList"];
        List<Vector3> targetList = new List<Vector3>();

        Character user = (Character)info["User"];

        foreach (Vector2Int targetPos in targetPosList)
        {
            if (targetPos.y < 0)
            {
                targetList.Add(CombatHandler.main.getNewPos(new Vector2Int(targetPos.x, 0)) + new Vector3(-1f, 0, 0));
            }
            else if (targetPos.y > 5)
            {
                targetList.Add(CombatHandler.main.getNewPos(new Vector2Int(targetPos.x, 0)) + new Vector3(1f, 0, 0));
            }
            else
            {
                targetList.Add(CombatHandler.main.getNewPos(targetPos));
            }

        }

        foreach (Vector3 targetPos in targetList)
        {
            user.StartCoroutine(Poke(targetPos, user));
        }
    }

    private IEnumerator Poke(Vector3 target, Character user)
    {
        GameObject obj = VFXHandler.MakeObject(spriteID, new Vector3(user.transform.position.x, user.transform.position.y + 0.75f * user.transform.localScale.y, -2));
        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();

        Color32 baseColor = spriteRenderer.color;
        Vector3 targetPos = new Vector3(target.x, target.y + 0.75f, -2);

        spriteRenderer.color = new Color32(baseColor.r, baseColor.g, baseColor.b, 0);
        obj.transform.right = new Vector3(user.transform.position.x, user.transform.position.y + 0.75f * user.transform.localScale.y, -2) - targetPos;

        user.StartCoroutine(Tween.New(baseColor, spriteRenderer, 0.25f));
        user.StartCoroutine(Tween.New(targetPos, obj.transform, 0.8f));

        yield return new WaitForSeconds(0.4f);

        user.StartCoroutine(Tween.New(new Color32(baseColor.r, baseColor.g, baseColor.b, 0), spriteRenderer, 0.4f));

        yield return new WaitForSeconds(0.5f);

        Object.Destroy(obj);
    }
}

public class AnimVFX : Effect//Realy complex code, this one is.
{
    string spriteID;
    Vector3 offset;

    public AnimVFX(string spriteID, Vector3 offset) : base(0)
    {
        this.spriteID = spriteID;
        this.offset = offset;
    }

    public AnimVFX(string spriteID, Vector3 offset, float waitTime) : base(waitTime)
    {
        this.spriteID = spriteID;
        this.offset = offset;
    }

    public override void Activate(Attack attack)
    {
        Dictionary<String, object> info = attack.GetInfo();

        List<Vector2Int> targetPosList = (List<Vector2Int>)info["TargetPosList"];
        List<Character> targetList = new List<Character>();

        foreach (Vector2Int targetPos in targetPosList)
        {
            targetList.Add(CombatHandler.main.GetCharacter(targetPos));
        }

        targetList.RemoveAll(target => target == null);

        foreach (Character target in targetList)
        {
            GameObject vfxObject = VFXHandler.MakeObject(spriteID, target.transform.position + new Vector3(offset.x, offset.y * target.transform.localScale.y, offset.x));
            vfxObject.transform.localScale = target.transform.localScale;

            if (vfxObject.GetComponent<Animator>() == null)
            {
                Object.Destroy(vfxObject, 2f);
            }
        }
    }
}

public class DiceVFX : Effect //While this is an effect, it is intended to only be used by other effects, not attacks.
{
    private int faces;

    private static List<AudioClip> rollNoises = new List<AudioClip> { Resources.Load<AudioClip>("CombatPrefabs/Sounds/SFX/Dice_1"), Resources.Load<AudioClip>("CombatPrefabs/Sounds/SFX/Dice_2"), Resources.Load<AudioClip>("CombatPrefabs/Sounds/SFX/Dice_3") };

    public DiceVFX(Character user, Transform parent, int damage, int faces) : base(0)
    {
        user.StartCoroutine(Activate(parent, damage));

        this.faces = faces;
    }

    private IEnumerator Activate(Transform parent, int damage)
    {
        WaitForEndOfFrame waitTime = new WaitForEndOfFrame();

        GameObject diceObject = Object.Instantiate(Resources.Load<GameObject>("CombatPrefabs/VFX/DamageDice"), parent);
        Animator diceAnimator = diceObject.GetComponent<Animator>();
        AudioSource diceAudio = diceObject.GetComponent<AudioSource>();

        diceAnimator.Play("Roll");

        diceAudio.clip = rollNoises[Random.Range(0, 3)];
        diceAudio.Play();

        TextMeshProUGUI text = diceObject.transform.Find("Text").GetComponent<TextMeshProUGUI>();

        float endTime = Time.time + 0.5f;

        while (Time.time < endTime)
        {
            text.text = Random.Range(1, faces + 1).ToString();

            yield return waitTime;

        }

        diceAnimator.Play("Idle");

        yield return waitTime;

        text.text = damage.ToString();

        yield return new WaitForSecondsRealtime(Random.Range(1, 2.5f));

        Object.Destroy(diceObject);
    }

}

public class VFXHandler : MonoBehaviour
{
    private static Dictionary<string, GameObject> VFXSprites = new Dictionary<string, GameObject>
    {
        { "Missing", Resources.Load<GameObject>("CombatPrefabs/VFX/Missing")},
        { "Spear", Resources.Load<GameObject>("CombatPrefabs/VFX/Spear") },
        { "Guard", Resources.Load<GameObject>("CombatPrefabs/VFX/Guard") },
        { "Contagion", Resources.Load<GameObject>("CombatPrefabs/VFX/Contagion") },
        { "BeeLarge", Resources.Load<GameObject>("CombatPrefabs/VFX/BeeLarge") },
        { "BeeSmall", Resources.Load<GameObject>("CombatPrefabs/VFX/BeeSmall") },
        { "Ooze", Resources.Load<GameObject>("CombatPrefabs/VFX/Ooze") }
    };

    public static GameObject MakeObject(string spriteId, Vector3 pos)
    {
        if (VFXSprites.ContainsKey(spriteId))
        {
            return Instantiate(VFXSprites[spriteId], pos, Quaternion.identity);
        }
        else
        {
            GameObject placeholder = Instantiate(VFXSprites["Missing"], pos, Quaternion.identity);
            placeholder.GetComponent<SpriteRenderer>().color = new Color32(255, 0, 0, 255);
            return placeholder;
        }
    }
}