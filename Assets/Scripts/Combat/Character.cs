using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Random = UnityEngine.Random;

public class Character : MonoBehaviour //the superclass of both enemies and players
{
    public bool usingCard;

    protected static Dictionary<string, Color32> CharacterColors = new Dictionary<string, Color32>
    {
        //-----Players-----\\
        {"One Armed Knight", new Color32(255, 255, 255, 255)},
        {"Plague Caster", new Color32(255, 255, 255, 255) },
        //-----Enemies-----\\
        {"Skeleton", new Color32(255, 255, 255, 255)},
        {"Crypt Keeper", new Color32(200, 180, 210, 255) }
    };

    protected string characterName;

    protected Dictionary<string, int> drawPile;//when drawing a card, they pull from this list
    protected Dictionary<string, int> discardPile;//when using a card, it moves to this (if it doesnt proc exhaustion)
    protected Dictionary<string, int> items;

    protected Dictionary<string, Sprite> statusSymbols;

    private static float[,] scaleReference = { { 1, 1.1f, 1.15f, -1.15f, -1.1f, -1 }, { 0.85f, 1, 1.1f, -1.1f, -1, -0.85f } };//size of the character based on grid position

    protected bool exhaustedLastTurn;

    protected int cardsUsed;//the amount of cards used this turn
    protected int exhaustionChance;//the chance of a card proccing exhaustion, represented as a d20
    protected int health;//self explanatory, if it reaches 0 you die.
    protected int speed;//determines the turn order by rolling a d20, higher speeds go first
    protected int speedMod;//arbitrary number to add on to the speed roll
    protected int movement;//amount of grids the character can move on their turn

    protected Color32 baseColor;//color of the character

    protected SpriteRenderer spriteRenderer;

    protected GameObject effectTemplate;

    protected Transform statusContainer;
    protected Transform diceContainer;

    protected List<StatusEffect> statusEffects;//list of status effects applied to this character

    protected Vector2Int gridPos;//the grid position of the character.


    public virtual void New(int health, Vector2Int gridPos, string name, Sprite sprite, Dictionary<string, int> drawPile, Dictionary<string, int> items)//character constructor
    {
        this.health = health;
        this.gridPos = gridPos;
        this.name = name;
        this.drawPile = drawPile;
        this.items = items;

        characterName = name;

        statusSymbols = new Dictionary<string, Sprite>
        {
            {"Guarded", Sprite.Create(Resources.Load<Texture2D>("StatusEffectSprites/Guarded"), new Rect(0, 0, 49, 49), new Vector2(0.5f, 0.5f)) },
            {"Contagion", Sprite.Create(Resources.Load<Texture2D>("StatusEffectSprites/Contagion"), new Rect(0, 0, 49, 49), new Vector2(0.5f, 0.5f)) },
            {"Poison", Sprite.Create(Resources.Load<Texture2D>("StatusEffectSprites/Poison"), new Rect(0, 0, 49, 49), new Vector2(0.5f, 0.5f)) },
            {"Oozed", Sprite.Create(Resources.Load<Texture2D>("StatusEffectSprites/Oozed"), new Rect(0, 0, 49, 49), new Vector2(0.5f, 0.5f)) }
        };

        statusEffects = new List<StatusEffect>();

        statusContainer = transform.Find("StatusContainer");
        diceContainer = transform.Find("DiceContainer");

        effectTemplate = Resources.Load<GameObject>("CombatPrefabs/GUI/StatusEffect");

        discardPile = new Dictionary<string, int>();

        if (CharacterColors.ContainsKey(name))
        {
            baseColor = CharacterColors[name];
        }
        else
        {
            baseColor = new Color32(255, 255, 255, 255);
        }

        spriteRenderer = GetComponent<SpriteRenderer>();

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = baseColor;

        Move(gridPos, false, true);
    }


    public virtual IEnumerator Turn(bool first)//signals that it is this character's turn
    {
        exhaustedLastTurn = false;
        movement = 2;
        cardsUsed = 0;

        for (int i = 0; i < statusEffects.Count; i++)//loops through and activates/reduces any status effects that have an effect at the begining of a turn
        {
            StatusEffect status = statusEffects[i];

            if (status.triggers.Contains("ModifyMovement"))
            {
                movement = Mathf.Clamp(status.Activate(movement), 0, int.MaxValue);
            }

            if (status.triggers.Contains("ReduceOnTurnStart"))
            {
                if (!status.Reduce())
                {
                    i--;
                    OrganizeStatusEffects();
                }
            }
        }

        yield return new WaitForFixedUpdate();
    }

    public void Move(Vector2Int moveTo, bool moveOthers, bool instant)//changes the grid position to the argument, and updates the world position using getNewPos()
    {
        if (moveOthers)
        {
            Character otherCharacter = CombatHandler.main.GetCharacter(moveTo);

            if (otherCharacter != null)
            {
                Debug.Log(name + " swapped places with " + otherCharacter.name + ".");
                otherCharacter.Move(gridPos, false, false);
            }
        }

        float scale = scaleReference[moveTo.x, moveTo.y];

        if (instant)
        {
            transform.position = CombatHandler.main.getNewPos(moveTo);
            transform.localScale = new Vector3(scale, Mathf.Abs(scale), 1);
        }
        else
        {
            StartCoroutine(Tween.New(CombatHandler.main.getNewPos(moveTo), transform, 0.25f));
            StartCoroutine(Tween.NewScale(new Vector3(scale, Mathf.Abs(scale), 1), transform, 0.25f));
        }

        gridPos = moveTo;

        if (gridPos.y >= 3)
        {
            statusContainer.localScale = new Vector3(-1, 1, 1);
            diceContainer.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            statusContainer.localScale = new Vector3(1, 1, 1);
            diceContainer.localScale = new Vector3(1, 1, 1);
        }
    }

    public void RollSpeed()//rolls a random speed between 1 and 20, then adds the speedMod to that number
    {
        speed = Random.Range(1, 20) + speedMod;
        new DiceVFX(this, transform.Find("DiceContainer"), speed - speedMod, 20);
        speedMod = 0;
    }

    public void AddSpeed(int speedMod)
    {
        this.speedMod += speedMod;
    }

    public virtual void AddExhaustionDC(int amount)
    {
        exhaustionChance = Mathf.Clamp(exhaustionChance + amount, 0, 20);
    }


    public virtual List<int> TakeDamage(int damage)//updates the character's health, and destroys them if it is at or below 0
    {
        foreach (StatusEffect status in statusEffects)
        {
            if (status.triggers.Contains("ModifyTakenDamage"))
            {
                damage = status.Activate(damage);
            }
        }

        health -= damage;

        if (health <= 0)
        {
            CombatHandler.main.RemoveCharacter(this);
            Debug.Log(name + " has perished.");

            StartCoroutine(Die());
        }
        else
        {
            StartCoroutine(DamageVisuals());
        }

        return new List<int> { health, damage };
    }

    private IEnumerator DamageVisuals()
    {
        spriteRenderer.color = new Color32(255, 0, 0, 255);
        yield return StartCoroutine(Tween.New(baseColor, spriteRenderer, 0.2f));
    }

    protected virtual IEnumerator Die()
    {
        List<Dictionary<string, object>> charInfo = MainManager.characterManager.GetCharacters();

        for (int i = 0; i < charInfo.Count; i++)
        {
            if (((GameObject)charInfo[i]["CombatReference"]).Equals(gameObject))
            {
                if (charInfo[i]["OverworldReference"] != null)
                {
                    MainManager.roomManager.RemoveObject((GameObject)charInfo[i]["OverworldReference"]);
                }

                charInfo.RemoveAt(i);
                break;
            }
        }

        yield return new WaitForFixedUpdate();

        Destroy(gameObject);
    }


    public void AddStatus(StatusEffect status)
    {
        string effectName = status.type;
        bool found = false;

        int i = 0;
        while (i < statusEffects.Count)
        {
            if (statusEffects[i].type.Equals(effectName))
            {
                if (!statusEffects[i].Stack(status.duration))
                {
                    OrganizeStatusEffects();
                    return;
                }

                found = true;
                break;
            }
            i++;
        }

        if (!found)
        {
            statusEffects.Add(status);

            GameObject effectVisual = Instantiate(effectTemplate, statusContainer, false);

            effectVisual.name = effectName;
            effectVisual.transform.Find("Canvas").Find("Amount").GetComponent<TextMeshProUGUI>().text = status.duration.ToString();
            status.effectVisual = effectVisual;

            if (statusSymbols.ContainsKey(effectName))
            {
                effectVisual.GetComponent<SpriteRenderer>().sprite = statusSymbols[effectName];
            }
            else
            {
                effectVisual.GetComponent<SpriteRenderer>().color = new Color32(255, 0, 0, 255);
            }

            OrganizeStatusEffects();
        }
    }

    public void RemoveStatus(StatusEffect status)
    {
        statusEffects.Remove(status);
        OrganizeStatusEffects();
    }

    private void OrganizeStatusEffects()
    {
        {
            int i = 0;

            while (i < statusContainer.childCount)
            {
                Transform v = statusContainer.GetChild(i);
                string effectName = v.name;
                bool found = false;

                foreach (StatusEffect effect in statusEffects)
                {
                    if (effect.type.Equals(effectName))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Destroy(v.gameObject);
                }

                i++;
            }
        }

        int childCount = statusContainer.childCount;
        for (int i = 0; i < childCount; i++)
        {
            statusContainer.GetChild(i).localPosition = new Vector3(-(childCount - 1) / 4f + (i * 0.5f), 0, 0);
        }
    }


    public virtual void DrawCard(int amount) { }

    public virtual void RemoveCard(Card card) { }


    public int GetSpeed()//returns the character's speed
    {
        return speed;
    }

    public Vector2Int GetGridPos()//returns the character's grid position
    {
        return gridPos;
    }

    public int GetHealth()//return's the character's health
    {
        return health;
    }

    public string GetName()// returns the character's name
    {
        return name;
    }

    public int GetDrawPileCount()
    {
        int count = 0;

        foreach (int amount in drawPile.Values)
        {
            count += amount;
        }

        return count;
    }

    public List<StatusEffect> GetStatus()
    {
        return statusEffects;
    }


    private void OnMouseEnter()
    {
        statusContainer.gameObject.SetActive(true);
    }

    private void OnMouseExit()
    {
        statusContainer.gameObject.SetActive(false);
    }
}

public class Player : Character
{
    private int turnStage = 0; //0: not this characters turn, 1: pre turn, 2: turn
    private int drawAmount = 5;

    private bool turnEnd = false;
    private bool dragging = false;

    private List<Card> hand = new List<Card>();

    private List<Card> discardObjects = new List<Card>(); //not actually objects, it contains the list of half destroyed cards that need to be used to show refilling the draw pile.

    private static Dictionary<string, Sprite> Emblems;

    private GameObject itemPrefab;

    private void Awake()
    {
        Emblems = new Dictionary<string, Sprite>
        {
            {"One Armed Knight", Sprite.Create(Resources.Load<Texture2D>("CombatPrefabs/CharacterSprites/Emblems/OneArmedKnight"), new Rect(0, 0, 81, 81), new Vector2(0.5f, 0.5f)) },
            {"Plague Caster", Sprite.Create(Resources.Load<Texture2D>("CombatPrefabs/CharacterSprites/Emblems/PlagueCaster"), new Rect(0, 0, 81, 81), new Vector2(0.5f, 0.5f))  }
        };

        itemPrefab = Resources.Load<GameObject>("CombatPrefabs/Gui/Item");
    }

    public override void New(int health, Vector2Int gridPos, string name, Sprite sprite, Dictionary<string, int> drawPile, Dictionary<string, int> items)//player constructor
    {
        base.New(health, gridPos, name, sprite, drawPile, items);

        transform.Find("StatusContainer").localPosition = new Vector3(0, -0.3f, -1);

        CombatHandler.main.endTurnButton.onClick.AddListener(Click);

        CombatHandler.main.preTurnGui.transform.Find("Draw").GetComponent<Button>().onClick.AddListener(() =>
        {
            if (turnStage == 1)
            {
                drawAmount++;
                turnStage = 2;
            }
        });

        CombatHandler.main.preTurnGui.transform.Find("Fold").GetComponent<Button>().onClick.AddListener(() =>
        {
            if (turnStage == 1)
            {
                Debug.Log(name + " folded.");

                drawAmount += 2;
                turnStage = 0;
            }
        });

        CombatHandler.main.preTurnGui.transform.Find("Item").GetComponent<Button>().onClick.AddListener(() =>
        {
            if (turnStage == 1 && items.Count > 0)
            {
                CombatHandler.main.itemGui.transform.parent.gameObject.SetActive(true);

                MakeItems();
            }
        });

        CombatHandler.main.preTurnGui.transform.Find("Flee").GetComponent<Button>().onClick.AddListener(() =>
        {
            if (turnStage == 1)
            {
                List<Character> charList = CombatHandler.main.GetParticipants(1);

                int roll = Random.Range(1, 21);
                new DiceVFX(this, transform, roll, 20);

                if (roll >= 3 + charList.Count * 2)
                {
                    CombatHandler.main.RemoveCharacter(this);

                    turnStage = 0;

                    Debug.Log(name + " fled.");

                    StartCoroutine(Tween.New(transform.position + new Vector3(2 * (gridPos.y - 2.5f), -3, 0), transform, 5));
                    StartCoroutine(Tween.New(new Color32(255, 255, 255, 0), spriteRenderer, 5));
                }
                else
                {
                    turnStage = 2;
                }

            }
        });
    }

    public override IEnumerator Turn(bool first)
    {
        turnEnd = false;
        turnStage = 1;

        CombatHandler.main.drawPile.GetComponent<Animator>().Play("Open");
        CombatHandler.main.drawPile.transform.Find("Deck").Find("Emblem").GetComponent<SpriteRenderer>().sprite = Emblems.ContainsKey(name) ? Emblems[name] : null;

        if (drawPile.Count > 0)
        {
            CombatHandler.main.drawPile.transform.Find("Card").gameObject.SetActive(true);
        }

        CombatHandler.main.preTurnGui.SetActive(true);
        CombatHandler.main.preTurnGui.GetComponent<Animator>().Play("Enable");

        yield return new WaitUntil(() => turnStage != 1);

        CombatHandler.main.preTurnGui.GetComponent<Animator>().Play("Disable");

        yield return new WaitForSeconds(0.3f);

        CombatHandler.main.preTurnGui.SetActive(false);

        StartCoroutine(base.Turn(first));//trigger any begining-of-turn status effects

        if (drawPile.Count == 0 && discardPile.Count == 0 && hand.Count == 0)
        {
            int damage = Random.Range(1, 11);
            TakeDamage(damage);
            new DiceVFX(this, transform.Find("DiceContainer"), damage, 10);
        }

        if (turnStage == 2)//if the player did not forfeit their turn (didnt fold)
        {
            if (!exhaustedLastTurn)
            {
                exhaustionChance = Mathf.Clamp(exhaustionChance - 1, 0, 20);
            }

            yield return new WaitForSeconds(0.75f);

            CombatHandler.main.exhaustionDC.GetComponent<TextMeshProUGUI>().text = "Exhaustion\nRoll " + exhaustionChance + " or above.";
            CombatHandler.main.exhaustionDC.SetActive(true);

            CombatHandler.main.movementGui.text = "Movement remaining: " + movement;
            CombatHandler.main.movementGui.gameObject.SetActive(true);

            yield return StartCoroutine(DrawCardCoroutine(drawAmount));

            CombatHandler.main.endTurnButton.gameObject.SetActive(true);
            CombatHandler.main.endTurnButton.GetComponent<Animator>().Play("Enable");

            yield return new WaitUntil(() => turnEnd);

            CombatHandler.main.endTurnButton.GetComponent<Animator>().Play("Disable");
        }

        turnStage = 0;
        movement = 0;

        foreach (Card card in hand)
        {
            card.gameObject.SetActive(false);

            yield return new WaitForSeconds(0.25f / hand.Count);
        }

        if (drawPile.Count == 0)
        {
            drawPile = discardPile;
            discardPile = new Dictionary<string, int>();

            int amount = discardObjects.Count;
            for (int i = 0; i < amount; i++)//Remove every card and make it do the return visuals one by one
            {
                StartCoroutine(discardObjects[0].Return());
                discardObjects.RemoveAt(0);

                yield return new WaitForSeconds(0.2f / amount);
            }
        }

        CombatHandler.main.exhaustionDC.SetActive(false);
        CombatHandler.main.endTurnButton.gameObject.SetActive(false);
        CombatHandler.main.movementGui.gameObject.SetActive(false);

        CombatHandler.main.drawPile.GetComponent<Animator>().SetBool("Open", false);

        yield return new WaitForSeconds(1);
    }

    public override List<int> TakeDamage(int damage)
    {
        List<int> returnVal = base.TakeDamage(damage);

        CombatHandler.main.UpdateHealthVignette();

        return returnVal;
    }

    protected override IEnumerator Die()
    {
        turnEnd = true;
        turnStage = 0;

        Vector3 newPos = new Vector3(transform.position.x, transform.position.y - 0.125f, transform.position.z);//the only difference is that players go down and enemies go up

        StartCoroutine(Tween.New(new Color32(0, 0, 0, 255), spriteRenderer, 0.25f));
        StartCoroutine(Tween.New(newPos, transform, 0.5f));
        StartCoroutine(Tween.New(Quaternion.Euler(0, 0, Random.Range(-4, 5) * 5), transform, 0.25f));

        yield return new WaitForSeconds(0.15f);

        StartCoroutine(Tween.New(new Color32(0, 0, 0, 0), spriteRenderer, 0.4f));

        yield return new WaitForSeconds(0.6f);

        StartCoroutine(base.Die());
    }

    public override void DrawCard(int amount)
    {
        StartCoroutine(DrawCardCoroutine(amount));
    }

    public IEnumerator DrawCardCoroutine(int amount)
    {
        List<Card> flipThese = new List<Card>();
        List<string> keys = new List<string>(drawPile.Keys);

        for (int i = 0; i < amount && drawPile.Count > 0; i++)//create new random cards and remove them from the draw pile
        {
            string randomCard = keys[Random.Range(0, keys.Count)];
            drawPile[randomCard]--;

            Card card = MainManager.combatManager.MakeCardObject(randomCard, this);

            if (drawPile[randomCard] <= 0)
            {
                drawPile.Remove(randomCard);
                keys.Remove(randomCard);
            }

            hand.Add(card);//prematurely add them to the hand list so other cards will organize properly
            flipThese.Add(card);
        }

        drawAmount = 0;

        foreach (Card card in hand)//loop through all the cards already in the characters hand to instantly organize them.
        {
            if (!flipThese.Contains(card))
            {
                card.Organize(hand, hand.IndexOf(card));
            }
        }

        if (drawPile.Count == 0)
        {
            Debug.Log(name + " ran out of cards in their draw pile.");
            CombatHandler.main.drawPile.transform.Find("Card").gameObject.SetActive(false);
        }

        foreach (Card card in flipThese)//loop through all the new cards and play the flip animation on them.
        {
            card.gameObject.SetActive(true);

            card.GetComponent<Animator>().Play("Card Flip");
            StartCoroutine(Tween.New(new Vector3(-6.65f, 0.4f, 0), card.transform, 0.2f));

            yield return new WaitForSeconds(0.1f);

            StartCoroutine(Tween.New(Quaternion.Euler(0, 0, 0), card.transform, 0.15f));

            yield return new WaitForSeconds(0.2f);

            card.Organize(hand, hand.IndexOf(card));
        }
    }

    public override void RemoveCard(Card card)//removes the card from the players hand, and adds it to the discard pile
    {
        hand.Remove(card);

        cardsUsed++;

        if (cardsUsed > 1 || hand.Count == 0)
        {
            exhaustionChance = Mathf.Clamp(exhaustionChance + 2, 0, 20);

            CombatHandler.main.exhaustionDC.GetComponent<TextMeshProUGUI>().text = "Exhaustion\nRoll " + exhaustionChance + " or higher.";

            int random = Random.Range(1, 21);

            Debug.Log("Chance: " + exhaustionChance + ". Rolled: " + random + ".");

            if (random == 1 || random < exhaustionChance && random != 20)
            {
                exhaustedLastTurn = true;
                Debug.Log(name + " exhausted their " + card.GetName() + " card.");

                StartCoroutine(card.Burn());

                foreach (Card otherCard in hand)//organizes the rest of the cards
                {
                    otherCard.Organize(hand, hand.IndexOf(otherCard));
                }

                return;
            }
        }

        string cardName = card.GetName();

        if (!discardPile.ContainsKey(cardName))
        {
            discardPile.Add(cardName, 0);
        }

        discardPile[card.GetName()]++;

        foreach (Card otherCard in hand)//organizes the rest of the cards
        {
            otherCard.Organize(hand, hand.IndexOf(otherCard));
        }

        discardObjects.Add(card);

        StartCoroutine(card.Remove());
    }

    public List<Card> GetHand()
    {
        return hand;
    }

    private void OnMouseDrag()//If it the players turn, and they have movement points left, allow them to move by dragging to a new grid
    {
        if (movement > 0 && turnStage == 2 && !usingCard)
        {
            dragging = true;

            Vector3 pos_move = MainManager.mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));
            transform.position = new Vector3(pos_move.x, pos_move.y, -2);
        }
    }

    private void OnMouseUp()//Extension of OnMouseDrag, finds the nearest grid to where you dragged the character and moves them to the closest grid within range of it
    {
        if (dragging)
        {
            dragging = false;

            Vector2Int requestPos = CombatHandler.main.GetClosestGridPos(transform.position, gridPos.x);
            Vector2Int newPos = new Vector2Int(gridPos.x, gridPos.y + Mathf.Clamp(requestPos.y - gridPos.y, -movement, movement));

            int movementCost = Mathf.Abs(newPos.y - gridPos.y);

            if (movementCost > 0)
            {
                movementCost = Mathf.Clamp(movementCost, 0, movement);

                movement -= movementCost;

                CombatHandler.main.movementGui.text = "Movement remaining: " + movement;
                Debug.Log(name + " used " + movementCost + " movement points. " + movement + " remaining.");

                Move(newPos, true, false);
            }
            else
            {
                transform.position = CombatHandler.main.getNewPos(gridPos);
            }
        }
    }

    private void Click()//ends the players turn, remember to rename this if more buttons require a dedicated click function.
    {
        if (turnStage == 2 && !turnEnd)
        {
            turnEnd = true;
        }
    }

    private void MakeItems()
    {
        foreach (string key in items.Keys)
        {
            GameObject item = Instantiate(itemPrefab, CombatHandler.main.itemGui.transform);
            item.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = key + " x " + items[key];

            item.GetComponent<Button>().onClick.AddListener(() =>
            {
                string thisKey = key;

                MainManager.combatManager.UseAttack(thisKey, this);
                items[thisKey]--;

                if (items[thisKey] <= 0)
                {
                    items.Remove(thisKey);
                }

                ClearItems();
            });
        }
    }

    private void ClearItems()
    {
        int childCount = CombatHandler.main.itemGui.transform.childCount;

        for (int i = childCount - 1; i >= 0; i--)
        {
            Destroy(CombatHandler.main.itemGui.transform.GetChild(i).gameObject);
        }

        turnStage = 2;

        CombatHandler.main.itemGui.transform.parent.gameObject.SetActive(false);
    }

    public override void AddExhaustionDC(int amount)
    {
        exhaustionChance = Mathf.Clamp(exhaustionChance + amount, 0, 20);
        CombatHandler.main.exhaustionDC.GetComponent<TextMeshProUGUI>().text = "Exhaustion\nRoll " + exhaustionChance + " or higher.";
    }
}

public class Enemy : Character
{
    protected List<string> hand = new List<string>();

    public override void New(int health, Vector2Int gridPos, string name, Sprite sprite, Dictionary<string, int> drawPile, Dictionary<string, int> items)
    {
        base.New(health, gridPos, name, sprite, drawPile, items);
        transform.Find("StatusContainer").localPosition = new Vector3(0, 1.5f, -1);

        DrawCard(5);
    }

    public override IEnumerator Turn(bool first)
    {
        StartCoroutine(base.Turn(first));

        if (drawPile.Count == 0 && discardPile.Count == 0 && hand.Count == 0)
        {
            int damage = Random.Range(1, 11);
            TakeDamage(damage);
            new DiceVFX(this, transform.Find("DiceContainer"), damage, 10);
        }

        if (health <= 0)
        {
            yield break;
        }

        if (Random.Range(1, 5) == 1 && hand.Count <= 3 && drawPile.Count >= 2)
        {
            DrawCard(2);

            Debug.Log(name + " folded.");

            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        DrawCard(1);

        bool usedCard = true;
        int bravery = Random.Range(2, 21); //change to end turn if exaustion is above this

        while (usedCard == true && hand.Count > 0 && !(Random.Range(1, 3) == 1 && exhaustionChance > bravery)) //they end their turn under 1 of 3 conditions, 1: if they werent able to use the last card they selected, 2: if they ran out of cards, 3:random 50% chance after exeeding bravery
        {
            usedCard = false;
            bravery = Random.Range(2, 21);

            Character target = CombatHandler.main.GetCharacter(new Vector2Int(Mathf.Abs(gridPos.x - 1), gridPos.y));

            if (target == null)
            {
                target = CombatHandler.main.GetLowestHealth(Mathf.Abs(gridPos.x - 1));
            }

            if (target == null)
            {
                yield break;
            }

            Vector2Int targetPos = target.GetGridPos();

            string randomCard = hand[Random.Range(0, hand.Count)];

            List<int> validPos = MainManager.combatManager.GetStandPositions(randomCard);

            int closestPos = validPos[0];
            foreach (int pos in validPos)
            {
                if (Mathf.Abs(gridPos.y - (targetPos.y + pos)) <= Mathf.Abs(gridPos.y - (targetPos.y + closestPos)) && targetPos.y + pos >= 0 && targetPos.y + pos <= 5)
                {
                    closestPos = pos;
                }
            }

            targetPos = new Vector2Int(targetPos.x, targetPos.y + closestPos);
            int moveDirection = Mathf.Clamp(targetPos.y - gridPos.y, -movement, movement);

            if (Mathf.Abs(moveDirection) > 0 && movement > 0)
            {
                Move(new Vector2Int(gridPos.x, gridPos.y + moveDirection), true, false);

                movement -= Mathf.Abs(moveDirection);

                Debug.Log(name + " used " + Mathf.Abs(moveDirection) + " movement points. " + movement + " remaining.");

                yield return new WaitForSeconds(1);
            }

            Debug.Log(name + " decided to use " + randomCard);

            yield return new WaitForSeconds(0.5f);

            if (gridPos.y == targetPos.y)//if the enemy is in range to use the card
            {
                MainManager.combatManager.UseAttack(randomCard, this);
                hand.Remove(randomCard);

                usedCard = true;
                cardsUsed++;
                exhaustionChance = Mathf.Clamp(exhaustionChance + 2, 0, 20);

                if (Random.Range(1, 21) < exhaustionChance)
                {
                    exhaustedLastTurn = true;
                    Debug.Log(name + " exhausted their " + randomCard + " card.");

                }
                else
                {
                    if (!discardPile.ContainsKey(randomCard))
                    {
                        discardPile.Add(randomCard, 1);
                    }
                    else
                    {
                        discardPile[randomCard]++;
                    }


                }

                yield return new WaitForSeconds(1);

                int random = Random.Range(0, 2);
                while (random == 1 && hand.Count > 0)//repeating 50% chance to use copies of the card, if any
                {
                    string nextCard = "";

                    foreach (string card in hand)
                    {
                        if (card.Equals(randomCard))
                        {
                            nextCard = card;
                            break;
                        }
                    }

                    if (!nextCard.Equals(""))
                    {
                        Debug.Log(name + " used " + nextCard + " again.");

                        MainManager.combatManager.UseAttack(randomCard, this);
                        hand.Remove(nextCard);

                        exhaustionChance = Mathf.Clamp(exhaustionChance + 2, 0, 20);

                        if (Random.Range(1, 21) < exhaustionChance)
                        {
                            exhaustedLastTurn = true;
                            Debug.Log(name + " exhausted their " + randomCard + " card.");

                        }
                        else
                        {
                            if (!discardPile.ContainsKey(randomCard))
                            {
                                discardPile.Add(randomCard, 0);
                            }

                            discardPile[randomCard]++;
                        }

                        random = Random.Range(0, 2);

                        yield return new WaitForSeconds(1);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else//enemy could not use a card
            {
                Debug.Log("But was not in range");
            }
        }

        if (hand.Count == 0)
        {
            Debug.Log(name + " ran out of cards in their hand.");
        }

        if (drawPile.Count == 0)
        {
            drawPile = discardPile;

            discardPile = new Dictionary<string, int>();
        }
    }

    protected override IEnumerator Die()
    {
        Vector3 newPos = new Vector3(transform.position.x, transform.position.y + 0.3f, transform.position.z);//the only difference is that players go down and enemies go up

        StartCoroutine(Tween.New(new Color32(0, 0, 0, 255), spriteRenderer, 0.15f));
        StartCoroutine(Tween.New(newPos, transform, 0.5f));
        StartCoroutine(Tween.New(Quaternion.Euler(0, 0, Random.Range(-4, 5) * 5), transform, 0.5f));

        yield return new WaitForSeconds(0.15f);

        StartCoroutine(Tween.New(new Color32(0, 0, 0, 0), spriteRenderer, 0.4f));

        yield return new WaitForSeconds(0.6f);

        StartCoroutine(base.Die());
    }

    public override void DrawCard(int amount)
    {
        List<string> keys = new List<string>(drawPile.Keys);

        for (int i = 0; i < amount && drawPile.Count > 0; i++)
        {
            string randomCard = keys[Random.Range(0, keys.Count)];

            hand.Add(randomCard);
            drawPile[randomCard]--;

            if (drawPile[randomCard] <= 0)
            {
                drawPile.Remove(randomCard);
                keys.Remove(randomCard);
            }
        }

        if (drawPile.Count == 0)
        {
            Debug.Log(name + " ran out of cards in their draw pile.");
        }
    }
}