using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

using Random = UnityEngine.Random;

public class StatusEffect
{
    protected Character target;

    public string type;

    public int duration;

    public List<string> triggers;

    public GameObject effectVisual;

    public StatusEffect(string type, Character target, List<string> triggers)
    {
        this.type = type;
        this.target = target;
        this.triggers = triggers;
    }

    protected void UpdateAmount()
    {
        if (effectVisual == null)
        {
            return;
        }
        effectVisual.transform.Find("Canvas").Find("Amount").GetComponent<TextMeshProUGUI>().text = duration.ToString();
    }

    public virtual bool Stack(int amount)
    {
        return true;
    }

    public virtual void Activate()
    {
    }

    public virtual int Activate(int armount)
    {
        return 0;
    }

    public virtual bool Reduce()
    {
        return true;
    }
}

public class Guarded : StatusEffect
{
    public Guarded(int duration, Character target) : base("Guarded", target, new List<string> { "ModifyTakenDamage", "ReduceOnTurnStart" })
    {
        this.duration = duration;
        UpdateAmount();
    }

    public override bool Reduce()
    {
        duration--;
        UpdateAmount();

        Debug.Log("Guarded decreased, new value: " + duration);

        if (duration <= 0)
        {
            Debug.Log("Guarded wore off on " + target.name + ".");
            target.RemoveStatus(this);

            return false;
        }

        return true;
    }

    public override bool Stack(int duration)
    {
        this.duration += duration;
        UpdateAmount();

        if (duration > 0)
        {
            Debug.Log("Increased Guarded, new value: " + this.duration);
        }
        else
        {
            Debug.Log("Reduced Guarded, new value: " + this.duration);
        }

        if (this.duration > 0)
        {
            return true;
        }
        else
        {
            Debug.Log("Guarded wore off on " + target.name + ".");
            target.RemoveStatus(this);
            return false;
        }
    }

    public override int Activate(int damage)
    {
        return (int)Math.Round(damage / 2.0f + 0.5f);
    }

    public override string ToString()
    {
        return "Guarded";
    }
}

public class Contagion : StatusEffect
{
    public Contagion(int duration, Character target) : base("Contagion", target, new List<string> { "ModifyTakenDamage", "ReduceOnTurnStart" })
    {
        this.duration = duration;
        UpdateAmount();
    }

    private void Explode()
    {
        Debug.Log(target.name + "'s infection progressed to its final stage.");
        for (int i = 0; i < 2; i++)
        {
            int damage = Random.Range(1, 7);

            new DiceVFX(target, target.transform.Find("DiceContainer"), damage, 6);
            target.TakeDamage(damage);
        }

        MainManager.combatManager.UseAttack("ContagionSpread", target);

        target.RemoveStatus(this);
    }

    public override bool Reduce()
    {
        duration++;
        UpdateAmount();

        Debug.Log("Increased contagion, new value: " + duration);

        if (duration >= 4)
        {
            Explode();

            return false;
        }

        return true;
    }

    public override bool Stack(int duration)
    {
        this.duration += duration;
        UpdateAmount();

        if (duration > 0)
        {
            Debug.Log("Increased Contagion, new value: " + this.duration);
        }
        else
        {
            Debug.Log("Decreased Contagion, new value: " + this.duration);
        }


        if (this.duration >= 4)
        {
            Explode();
            return false;
        }
        else if (this.duration <= 0)
        {
            Debug.Log(target.name + " was cured of their contagion.");
            target.RemoveStatus(this);

            return false;
        }
        else
        {
            return true;
        }
    }

    public override int Activate(int damage)
    {
        return (damage + duration);
    }

    public override string ToString()
    {
        return "Contagion";
    }
}

public class Poison : StatusEffect
{
    public Poison(int duration, Character target) : base("Poison", target, new List<string> { "ReduceOnTurnStart" })
    {
        this.duration = duration;
    }

    public override bool Reduce()
    {
        duration--;
        UpdateAmount();

        int damage = Random.Range(1, 5);

        target.TakeDamage(damage);

        new DiceVFX(target, target.transform.Find("DiceContainer"), damage, 4);

        Debug.Log("Poison decreased, new duration: " + duration);

        if (duration <= 0)
        {
            Debug.Log("Poison wore off on " + target.name + ".");
            target.RemoveStatus(this);

            return false;
        }

        return true;
    }

    public override bool Stack(int duration)
    {
        this.duration += duration;

        if (duration > 0)
        {
            Debug.Log("Increased Poison, new value: " + this.duration);
        }
        else
        {
            Debug.Log("Decreased Poison, new value: " + this.duration);
        }

        UpdateAmount();

        if (this.duration > 0)
        {
            return true;
        }
        else
        {
            Debug.Log("Poison wore off on " + target.name + ".");
            target.RemoveStatus(this);

            return false;
        }

    }

    public override string ToString()
    {
        return "Poison";
    }
}

public class Oozed : StatusEffect
{
    public Oozed(int duration, Character target) : base("Oozed", target, new List<string> { "ReduceOnTurnStart", "ModifyMovement" })
    {
        this.duration = duration;
    }

    public override int Activate(int movement)
    {
        Debug.Log("Reduced movement");
        return Mathf.Clamp(movement - 1, 0, int.MaxValue);
    }

    public override bool Reduce()
    {
        duration--;
        UpdateAmount();
        Debug.Log("Oozed decreased, new duration: " + duration);

        if (duration <= 0)
        {
            Debug.Log("Oozed wore off on " + target.name + ".");
            target.RemoveStatus(this);

            return false;
        }

        return true;
    }

    public override bool Stack(int duration)
    {
        this.duration += duration;
        UpdateAmount();

        if (duration > 0)
        {
            Debug.Log("Increased Oozed, new value: " + this.duration);
        }
        else
        {
            Debug.Log("Decreased Oozed, new value: " + this.duration);
        }

        if (this.duration > 0)
        {
            return true;
        }
        else
        {
            Debug.Log("Oozed wore off on " + target.name + ".");
            target.RemoveStatus(this);

            return false;
        }
    }

    public override string ToString()
    {
        return "Oozed";
    }
}