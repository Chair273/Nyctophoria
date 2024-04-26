using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.UI;
using System.Reflection;

//----------Game----------\\

public class CombatHandler : MonoBehaviour
{
    public static Button endTurnButton;

    public static GameObject preTurnGui;
    public static GameObject exhaustionDC;
    public static GameObject drawPile;

    private Transform Gui;

    public static TextMeshProUGUI movementGui;

    private bool gameEnded = false;

    private static List<Character> participants = new List<Character>();
    private List<Character> turnOrder = new List<Character>();

    private static float[,] xReference = { { 0, 1, 2, 3, 4, 5 }, { 0.7f, 1.3f, 2.1f, 2.9f, 3.7f, 4.3f } }; //used to convert a grid position to a world position, first index is the valid player x positions, second index is the valid enemy x positions

    public static float getXPos(int x, int y)//returns the world x position of a grid position
    {
        return xReference[x, y];
    }

    void Start()//basically all of this is a placeholder for testing purposes
    {
        Gui = transform.Find("Gui");

        endTurnButton = Gui.Find("EndTurnButtonCanvas").Find("EndTurnButton").GetComponent<Button>();
        preTurnGui = Gui.Find("PreTurnGui").gameObject;
        exhaustionDC = Gui.Find("Exhaustion").Find("ExhaustionDC").gameObject;
        drawPile = Gui.Find("DrawPile").gameObject;

        movementGui = Gui.Find("Movement").Find("Movement").GetComponent<TextMeshProUGUI>();

        AttackHandler.Start();

        GameObject prefab = Resources.Load<GameObject>("CombatPrefabs/CharacterPlaceholder");

        GameObject OAKObject = Instantiate(prefab, new Vector3Int(), Quaternion.identity);//One Armed Knight
        OAKObject.transform.parent = transform;

        Player OAK = OAKObject.AddComponent(typeof(Player)) as Player;

        OAK.New(40, new Vector2Int(0, Random.Range(3, 5)), "One Armed Knight", Resources.Load<Sprite>("CombatPrefabs/CharacterSprites/OneArmedKnight"), new Dictionary<string, int>//the name and sprite of the character
        { {"Spear Strike", 4 }, {"Bifurcated Strike", 3 }, {"Guard", 2 } });//the cards they have acess to, and the amount of each.

        participants.Add(OAK);

        GameObject PCObject = Instantiate(prefab, new Vector3Int(), Quaternion.identity);//Plague Caster
        PCObject.transform.parent = transform;

        Player PC = PCObject.AddComponent(typeof(Player)) as Player;

        PC.New(30, new Vector2Int(0, Random.Range(0, 3)), "Plague Caster", Resources.Load<Sprite>("CombatPrefabs/CharacterSprites/PlagueCaster"), new Dictionary<string, int>
        { {"Summon Bees", 3 }, {"Contagion", 2}, {"Lesser Ooze", 2} });

        participants.Add(PC);

        GameObject skeletonObject = Instantiate(prefab, new Vector3Int(), Quaternion.identity);
        skeletonObject.transform.parent = transform;

        Enemy skeleton = skeletonObject.AddComponent(typeof(Enemy)) as Enemy;
        skeleton.New(25, new Vector2Int(1, Random.Range(0, 3)), "Skeleton", Resources.Load<Sprite>("CombatPrefabs/CharacterSprites/Skeleton"), new Dictionary<string, int>
        {{"Spear Strike", 3 }, {"Bifurcated Strike", 3 }, {"Guard", 1 } });

        participants.Add(skeleton);

        GameObject CKObject = Instantiate(prefab, new Vector3Int(), Quaternion.identity);
        CKObject.transform.parent = transform;

        Enemy cryptKeeper = CKObject.AddComponent(typeof(Enemy)) as Enemy;
        cryptKeeper.New(45, new Vector2Int(1, Random.Range(3, 5)), "Crypt Keeper", Resources.Load<Sprite>("CombatPrefabs/CharacterSprites/CryptKeeper"), new Dictionary<string, int>
        {{"Spear Strike", 4 }, {"Bifurcated Strike", 2 }, {"Contagion", 1}, {"Lesser Ooze", 1} });

        participants.Add(cryptKeeper);

        StartCoroutine(Combat());//starts the combat encounter
    }

    IEnumerator Combat()
    {
        int round = 1;

        while (!gameEnded)//while there is at least 1 player and 1 enemy alive
        {
            turnOrder = new List<Character>();

            foreach (Character character in participants)
            {
                character.RollSpeed();

                turnOrder.Add(character);
            }

            {
                int index = 1;

                while (index < turnOrder.Count)
                {
                    if (index >= 1 && turnOrder[index].GetSpeed() > turnOrder[index - 1].GetSpeed())
                    {
                        Character temp = turnOrder[index];
                        turnOrder[index] = turnOrder[index - 1];
                        turnOrder[index - 1] = temp;

                        index--;
                    }
                    else
                    {
                        index++;
                    }
                }
            }

            foreach (Character character in turnOrder)
            {
                Debug.Log(character.name + " speed: " + character.GetSpeed());
                yield return new WaitForSeconds(1);
            }

            foreach (Character character in turnOrder)
            {
                if (character != null)
                {
                    Debug.Log("----------------------");
                    Debug.Log(character.GetName() + "'s turn.");

                    yield return StartCoroutine(character.Turn(round == 1));
                    yield return new WaitForSeconds(1);

                    bool foundPlayer = false;
                    bool foundEnemy = false;

                    foreach (Character checkCharacter in participants)
                    {
                        foundPlayer = checkCharacter.GetGridPos().x == 0 || foundPlayer;
                        foundEnemy = checkCharacter.GetGridPos().x == 1 || foundEnemy;

                        if (foundPlayer && foundEnemy)
                        {
                            break;
                        }
                    }

                    if (!foundPlayer || !foundEnemy)
                    {
                        gameEnded = true;
                        break;
                    }
                }
            }

            if (!gameEnded)
            {
                yield return new WaitForSeconds(1);
                Debug.Log("Round end.");

                round++;
            }
        }

        Debug.Log("Combat Ended");
    }

    public static Character GetCharacter(Vector2Int gridPos)
    {
        if (gridPos.y < 0 || gridPos.y > 5)
        {
            return null;
        }

        foreach (Character character in participants)
        {
            if (character.GetGridPos().Equals(gridPos))
            {
                return character;
            }
        }

        return null;
    }

    public static Character GetLowestHealth(int targetRow)//2 means any row
    {
        int lowestHealth = int.MaxValue;
        Character lowestHealthCharacter = null;

        foreach (Character character in participants)
        {
            int health = character.GetHealth();

            if (health < lowestHealth && (targetRow == 2 || character.GetGridPos().x == targetRow) )
            {
                lowestHealth = health;
                lowestHealthCharacter = character;
            }
        }

        return lowestHealthCharacter;
    }

    public static void RemoveCharacter(Character character)
    {
        participants.Remove(character);
    }

    public static Vector2Int GetClosestGridPos(Vector3 pos, int xIndex)
    {
        float closestDistance = float.MaxValue;
        Vector2Int closestPos = new Vector2Int(xIndex, 0);

        for (int i = 0; i < 6; i ++)
        {
            if (Vector3.Distance(pos, getNewPos(new Vector2Int(xIndex, i))) < closestDistance)
            {
                closestDistance = Vector3.Distance(pos, getNewPos(new Vector2Int(xIndex, i)));
                closestPos = new Vector2Int(xIndex, i);
            }
        }

        return closestPos;
    }

    public static Vector3 getNewPos(Vector2Int moveTo)//gets the physical world space the character should be in corresponding to each grid position
    {
        float xPos = getXPos(moveTo.x, moveTo.y);

        return new Vector3(xPos, moveTo.x == 0 ?
            0.1f * Mathf.Pow(xPos - 2.5f, 2) - 0.525f :
            0.145f * Mathf.Pow(xPos - 2.5f, 2) + 0.518f,
            moveTo.x == 0 ? -3: -1);
    }
}

public class AttackHandler : MonoBehaviour//handles the creation and storage of each card
{
    private static Dictionary<string, Attack> attacks = new Dictionary<string, Attack>();//stores the actuall code behind each card

    private static Dictionary<string, Dictionary<string, string>> cardInfo = new Dictionary<string, Dictionary<string, string>>();//stores the information of each card (used for player gui)

    private static Dictionary<string, Dictionary<string, float>> cardSize = new Dictionary<string, Dictionary<string, float>>();//stores the font size of each card's name and description

    private static Dictionary<string, TargetType> targetTypes = new Dictionary<string, TargetType>();//stores the different target types that cards can use

    private static GameObject cardPrefab = Resources.Load<GameObject>("CombatPrefabs/Gui/Card");

    public static void Start()
    {
        //targetTypes initialization
        targetTypes["ForwardHit"] = new BasicTarget(new List<int> { 0 }, new List<int> { 1 });
        targetTypes["DiagonalHit"] = new BasicTarget(new List<int> { -1, 1 }, new List<int> { 1 });
        targetTypes["SelfEffect"] = new BasicTarget(new List<int> { 0 }, new List<int> { 0 }, new List<int> {-1, 0, 1});

        //attacks initialization
        MakeCardIndex(new Dictionary<string, string>() //the name and description on the card
        {
            {"Name", "Spear Strike"},
            {"Description", "*The user deals 1d6 + 2 damage." }
        }, new Dictionary<string, float> //the font size of the name and description
        {
            {"Name", 0.15f},
            {"Description",  0.15f}
        }, new BasicAttack(new Dictionary<TargetType, List<Effect>> //the dictionary containing the cards target types, and the effects corrisponding to each
        {
            {targetTypes["ForwardHit"], new List<Effect> { new DamageDice(1, 6, 2), new PokeVFX("Spear") } }//This card targets the character in the opposite column to the user, and deals 1d4 + 2 damage, thus it uses the ForwardHit target type and its only effect is a 1d4 + 2 DamageDice
        }));

        MakeCardIndex(new Dictionary<string, string>()
        {
            {"Name", "Bifurcated Strike"},
            {"Description", "*The user deals 1d4 damage along both diagonals."}
        }, new Dictionary<string, float>
        {
            {"Name", 0.11f},
            {"Description",  0.15f}
        }, new BasicAttack(new Dictionary<TargetType, List<Effect>>
        {
            {targetTypes["DiagonalHit"], new List<Effect> { new DamageDice(1, 4, 0), new PokeVFX("Spear") } }
        }));

        MakeCardIndex(new Dictionary<string, string>()
        {
            {"Name", "Guard"},
            {"Description", "*The user gains the guarded status for one turn.\n*Reduces damage by half."}
        }, new Dictionary<string, float>
        {
            {"Name", 0.15f},
            {"Description",  0.12f}
        }, new BasicAttack(new Dictionary<TargetType, List<Effect>>
        {
            {targetTypes["SelfEffect"], new List<Effect> { new ApplyStatus("Guarded", 1), new SelfApplyAnimVFX("Guard") } },
        }));

        MakeCardIndex(new Dictionary<string, string>()
        {
            {"Name", "Contagion"},
            {"Description", "*Call forth a terrible disease to inflict apon your target.\n*Increases damage based on amount.\n*Contagious."}
        }, new Dictionary<string, float>
        {
            {"Name", 0.15f},
            {"Description",  0.1f}
        }, new BasicAttack(new Dictionary<TargetType, List<Effect>>
        {
            {targetTypes["ForwardHit"], new List<Effect> { new ApplyStatus("Contagion", 1) } },
        }));

        MakeCardIndex(new Dictionary<string, string>()
        {
            {"Name", "Summon Bees"},
            {"Description", "*Manifest a wave of bees.\n*Poisons and deals damage.\n*Effect diminishes with distance."}
        }, new Dictionary<string, float>
        {
            {"Name", 0.15f},
            {"Description",  0.1f}
        }, new BasicAttack(new Dictionary<TargetType, List<Effect>>
        {
            {targetTypes["ForwardHit"], new List<Effect> { new DamageDice(1, 4, 0), new ApplyStatus("Poison", 3) }},
            {targetTypes["DiagonalHit"], new List<Effect> {new DamageDice(1, 2, 0), new ApplyStatus("Poison", 2) }}
        }));

        MakeCardIndex(new Dictionary<string, string>()
        {
            {"Name", "Lesser Ooze"},
            {"Description", "*Coat your enemy in a debilitating slime.\n*Reduces movement for 3 turns."}
        }, new Dictionary<string, float>
        {
            {"Name", 0.15f},
            {"Description",  0.12f}
        }, new BasicAttack(new Dictionary<TargetType, List<Effect>>
        {
            {targetTypes["ForwardHit"], new List<Effect> { new ApplyStatus("Oozed", 2) } },
        }));
    }

    public static void UseAttack(Vector2Int userPos, Character character, string attackName) //allows characters to use cards
    {
        attacks[attackName].Activate(userPos, character);
    }

    public static List<int> GetStandPositions(string attackName)//remember to add a way to distinguish between cards that help and cards that hurt.
    {
        List<int> hitPositions = new List<int>();

        foreach (TargetType targetType in attacks[attackName].GetTargetTypes())//loop through each target type of the attack
        {
            foreach (int pos in targetType.GetStandPositions())//loop through each position the enemy can be in for the attack to land
            {
                if (!hitPositions.Contains(pos))//if the position has not already been considered, add it to the list.
                {
                    hitPositions.Add(pos);
                }
            }
        }

        return hitPositions;
    }

    private static void MakeCardIndex(Dictionary<string, string> newCardInfo, Dictionary<string, float> newCardSize, Attack attack)
    {
        attacks.Add(newCardInfo["Name"], attack);
        cardInfo.Add(newCardInfo["Name"], newCardInfo);
        cardSize.Add(newCardInfo["Name"], newCardSize);
    }

    public static Card MakeCardObject(string cardName, Character character)
    {
        if (!attacks.ContainsKey(cardName))
        {
            Debug.LogWarning("Invalid card key");
            return null;
        }

        GameObject cardObject = Instantiate(cardPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        cardObject.transform.parent = character.transform.parent;

        Card card = cardObject.AddComponent<Card>();
        card.New(attacks[cardName], cardInfo[cardName], cardSize[cardName], character);

        return card;
    }
}

//----------Characters----------\\

public class Character : MonoBehaviour //the superclass of both enemies and players
{
    protected static Dictionary<string, Color32> CharacterColors = new Dictionary<string, Color32>
    {
        //-----Players-----\\
        {"One Armed Knight", new Color32(0, 200, 100, 255)},
        {"Plague Caster", new Color32(170, 50, 0, 255) },
        //-----Enemies-----\\
        {"Skeleton", new Color32(255, 255, 255, 255)},
        {"Crypt Keeper", new Color32(200, 180, 210, 255) }
    };

    protected string characterName;

    protected Dictionary<string, int> drawPile;//when drawing a card, they pull from this list
    protected Dictionary<string, int> discardPile;//when using a card, it moves to this (if it doesnt proc exhaustion)

    protected bool exhaustedLastTurn;

    protected int cardsUsed;//the amount of cards used this turn
    protected int exhaustionChance;//the chance of a card proccing exhaustion, represented as a d20
    protected int health;//self explanatory, if it reaches 0 you die.
    protected int speed;//determines the turn order by rolling a d20, higher speeds go first
    protected int speedMod;//arbitrary number to add on to the speed roll
    protected int movement;//amount of grids the character can move on their turn

    protected Color32 baseColor;//color of the character

    protected SpriteRenderer spriteRenderer;

    protected List<StatusEffect> statusEffects = new List<StatusEffect>();//list of status effects applied to this character

    protected Vector2Int gridPos;//the grid position of the character.

    public virtual void New(int health, Vector2Int gridPos, string name, Sprite sprite, Dictionary<string, int> drawPile)//character constructor
    {
        this.health = health;
        this.gridPos = gridPos;
        this.name = name;
        characterName = name;
        this.drawPile = drawPile;

        discardPile = new Dictionary<string, int>();

        if (CharacterColors.ContainsKey(name))
        {
            baseColor = CharacterColors[name];
        }
        else
        {
            baseColor = new Color32(255, 255, 255, 255);
        }

        spriteRenderer = transform.GetComponent<SpriteRenderer>();

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = baseColor;

        transform.position = CombatHandler.getNewPos(gridPos);
        Move(gridPos, false);
    }

    public virtual IEnumerator Turn(bool first)//signals that it is this character's turn
    {
        int i = 0;

        while (i < statusEffects.Count)//loops through and activates/reduces any status effects that have an effect at the begining of a turn
        {
            StatusEffect status = statusEffects[i];

            if (status.triggers.Contains("ReduceOnTurnStart"))
            {
                if (status.Reduce())
                {
                    i++;
                }
            }

            if (status.triggers.Contains("ModifySpeed"))
            {
                speed = status.Activate(speed);
            }
        }
        yield return 0;
    }

    public virtual void DrawCard(int amount)
    {
        Debug.Log("DrawCard used on Character superclass, use subclass instead.");
    }

    public void Move(Vector2Int moveTo, bool moveOthers)//changes the grid position to the argument, and updates the world position using getNewPos()
    {
        if (moveOthers)
        {
            Character otherCharacter = CombatHandler.GetCharacter(moveTo);

            if (otherCharacter != null)
            {
                Debug.Log(name + " swapped places with " + otherCharacter.name + ".");
                otherCharacter.Move(gridPos, false);
            }
        }

        StartCoroutine(Tween.New(CombatHandler.getNewPos(moveTo), transform, 0.2f));
        gridPos = moveTo;
    }

    public void RollSpeed()//rolls a random speed between 1 and 20, then adds the speedMod to that number
    {
        speed = Random.Range(1, 20) + speedMod;
        speedMod = 0;
    }

    public List<int> TakeDamage(int damage)//updates the character's health, and destroys them if it is at or below 0
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
            CombatHandler.RemoveCharacter(this);
            Debug.Log(name + " has perished.");

            StartCoroutine(Die());
        }
        else
        {
            StartCoroutine(DamageVisuals());
        }

        return new List<int> {health, damage};
    }

    private IEnumerator DamageVisuals()
    {
        spriteRenderer.color = new Color32(255, 0, 0, 255);
        yield return StartCoroutine(Tween.New(baseColor, spriteRenderer, 0.2f));
    }

    public void AddStatus(StatusEffect status)
    {
        foreach (StatusEffect i in statusEffects)
        {
            if (i.type.Equals(status.type))
            {
                i.Stack(status.duration);
                return;
            }
        }

        statusEffects.Add(status);
    }

    protected virtual IEnumerator Die()
    {
        Destroy(gameObject);
        yield return 0;
    }

    public void RemoveStatus(StatusEffect status)
    {
        statusEffects.Remove(status);
    }

    public virtual void RemoveCard(Card card)
    {
        Debug.LogWarning("RemoveCard called on character superclass, use subclass isntead");
    }

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
}

public class Player : Character
{
    private int turnStage = 0; //0: not this characters turn, 1: pre turn, 2: turn
    private int drawAmount = 5;

    private bool turnEnd = false;
    private bool dragging = false;

    private List<Card> hand = new List<Card>();

    public override void New(int health, Vector2Int gridPos, string name, Sprite sprite, Dictionary<string, int> drawPile)//player constructor
    {
        base.New(health, gridPos, name, sprite, drawPile);

        CombatHandler.endTurnButton.onClick.AddListener(Click);

        CombatHandler.preTurnGui.transform.Find("Draw").GetComponent<Button>().onClick.AddListener(() =>
        {
            if (turnStage == 1)
            {
                drawAmount++;
                turnStage = 2;
            }
        });

        CombatHandler.preTurnGui.transform.Find("Fold").GetComponent<Button>().onClick.AddListener(() =>
        {
            if (turnStage == 1)
            {
                Debug.Log(name + " folded.");

                drawAmount += 2;
                turnStage = 0;
            }
        });
    }

    public override IEnumerator Turn(bool first)
    {
        turnEnd = false;
        turnStage = 1;

        CombatHandler.preTurnGui.SetActive(true);

        yield return new WaitUntil(() => turnStage != 1);

        CombatHandler.preTurnGui.SetActive(false);

        StartCoroutine(base.Turn(first));//trigger any begining-of-turn status effects

        if (turnStage == 2)//if the player did not forfeit their turn (didnt fold)
        {
            if (!exhaustedLastTurn)
            {
                exhaustionChance = Mathf.Clamp(exhaustionChance - 1, 0, 20);
            }

            CombatHandler.drawPile.GetComponent<Animator>().Play("Open");

            yield return new WaitForSeconds(0.75f);

            exhaustedLastTurn = false;
            movement = 2;
            cardsUsed = 0;

            DrawCard(drawAmount);

            CombatHandler.exhaustionDC.GetComponent<TextMeshProUGUI>().text = "Exhaustion\nRoll " + exhaustionChance + " or above.";
            CombatHandler.exhaustionDC.SetActive(true);

            CombatHandler.movementGui.text = "Movement remaining: " + movement;
            CombatHandler.movementGui.gameObject.SetActive(true);

            CombatHandler.endTurnButton.gameObject.SetActive(true);

            yield return new WaitUntil(() => turnEnd);
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
        }

        CombatHandler.exhaustionDC.SetActive(false);
        CombatHandler.endTurnButton.gameObject.SetActive(false);
        CombatHandler.movementGui.gameObject.SetActive(false);

        CombatHandler.drawPile.GetComponent<Animator>().SetBool("Open", false);

        yield return new WaitForSeconds(1);
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

        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    public override void DrawCard(int amount)
    {
        StartCoroutine(DrawCardCoroutine(amount));
    }

    public IEnumerator DrawCardCoroutine(int amount)
    {
        List<Card> flipThese = new List<Card>();
        List<string> keys = new List<string>(drawPile.Keys);

        for (int i = 0; i < amount && drawPile.Count > 0; i++)//create new random cards and remove it from the draw pile
        {
            string randomCard = keys[Random.Range(0, keys.Count)];
            drawPile[randomCard]--;

            Card card = AttackHandler.MakeCardObject(randomCard, this);


            if (drawPile[randomCard] <= 0)
            {
                drawPile.Remove(randomCard);
                keys.Remove(randomCard);
            }

            hand.Add(card);
            flipThese.Add(card);
        }

        drawAmount = 0;

        if (drawPile.Count == 0)
        {
            Debug.Log(name + " ran out of cards in their draw pile.");
        }

        foreach(Card card in hand)//loop through all the cards already in the characters hand to instantly organize them.
        {
            if (!flipThese.Contains(card))
            {
                card.Organize(hand, hand.IndexOf(card));
            }
        }

        foreach(Card card in flipThese)//loop through all the new cards and play the flip animation on them.
        {
            card.gameObject.SetActive(true);

            card.GetComponent<Animator>().Play("Card Flip");
            StartCoroutine(Tween.New(new Vector3(-4.25f, 0, 0), card.transform, 0.2f));

            yield return new WaitForSeconds(0.15f);

            if (drawPile.Count == 0)
            {
                CombatHandler.drawPile.transform.Find("Card").gameObject.SetActive(false);
            }
            else
            {
                CombatHandler.drawPile.transform.Find("Card").gameObject.SetActive(false);
            }

            StartCoroutine(Tween.New(Quaternion.Euler(0, 0, 0), card.transform, 0.15f));

            yield return new WaitForSeconds(0.2f);

            card.Organize(hand, hand.IndexOf(card));
        }
    }

    public override void RemoveCard(Card card)//removes the card from the players hand, and adds it to the discard pile
    {
        hand.Remove(card);

        cardsUsed++;

        if (cardsUsed > 1)
        {
            exhaustionChance = Mathf.Clamp(exhaustionChance + 1, 0, 20);

            CombatHandler.exhaustionDC.GetComponent<TextMeshProUGUI>().text = "Exhaustion\nRoll " + exhaustionChance + " or higher.";

            int random = Random.Range(1, 21);

            Debug.Log("Chance: " + exhaustionChance + ". Rolled: " + random + ".");

            if (random == 1 || random < exhaustionChance && random != 20)
            {
                exhaustedLastTurn = true;

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

        card.gameObject.SetActive(false);
        Destroy(card.gameObject);
    }

    public List<Card> GetHand()
    {
        return hand;
    }

    private void OnMouseDrag()//If it the players turn, and they have movement points left, allow them to move by dragging to a new grid
    {
        if (movement > 0 && turnStage == 2)
        {
            dragging = true;

            Vector3 pos_move = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));
            transform.position = new Vector3(pos_move.x, pos_move.y, -2);
        }
    }

    private void OnMouseUp()//Extension of OnMouseDrag, finds the nearest grid to where you dragged the character and moves them to the closest grid within range of it
    {
        if (dragging)
        {
            dragging = false;

            Vector2Int requestPos = CombatHandler.GetClosestGridPos(transform.position, gridPos.x);
            Vector2Int newPos = new Vector2Int(gridPos.x, gridPos.y + Mathf.Clamp(requestPos.y - gridPos.y, -movement, movement));

            int movementCost = Mathf.Abs(newPos.y - gridPos.y);

            if (movementCost > 0)
            {
                movementCost = Mathf.Clamp(movementCost, 0, movement);

                movement -= movementCost;

                CombatHandler.movementGui.text = "Movement remaining: " + movement;
                Debug.Log(name + " used " + movementCost + " movement points. " + movement + " remaining.");

                Move(newPos, true);
            }
            else
            {
                transform.position = CombatHandler.getNewPos(gridPos);
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
}

public class Enemy : Character
{
    protected List<string> hand = new List<string>();

    public override void New(int health, Vector2Int gridPos, string name, Sprite sprite, Dictionary<string, int> drawPile)
    {
        base.New(health, gridPos, name, sprite, drawPile);
    }

    public override IEnumerator Turn(bool first)
    {
        StartCoroutine(base.Turn(first));

        if (health <= 0)
        {
            yield break;
        }

        if (first)
        {
            DrawCard(5);
        }

        if (Random.Range(1, 5) == 4)
        {
            DrawCard(2);

            Debug.Log(name + " folded.");

            yield return new WaitForSeconds(1);
            yield break;
        }

        DrawCard(first == true ? 5 : 1);

        movement = 2;
        bool usedCard = true;

        while (usedCard == true && hand.Count > 0)
        {
            usedCard = false;

            Character target = CombatHandler.GetCharacter(new Vector2Int(Mathf.Abs(gridPos.x - 1), gridPos.y));

            if (target == null)
            {
                target = CombatHandler.GetLowestHealth(Mathf.Abs(gridPos.x - 1));
            }
            Vector2Int targetPos = target.GetGridPos();

            string randomCard = hand[Random.Range(0, hand.Count)];

            List<int> validPos = AttackHandler.GetStandPositions(randomCard);

            int closestPos = validPos[0];
            foreach (int pos in validPos)
            {
                if (Mathf.Abs(gridPos.y - (targetPos.y + pos)) <= Mathf.Abs(gridPos.y - (targetPos.y + closestPos)) && targetPos.y + pos >= 0 && targetPos.y + pos <= 5)
                {
                    closestPos = pos;
                }
            }

            targetPos = new Vector2Int(targetPos.x, targetPos.y + closestPos);
            int movementCost = Mathf.Abs(targetPos.y - gridPos.y);

            if (movementCost > 0 && movement > 0)
            {
                Move(new Vector2Int(gridPos.x, gridPos.y + Mathf.Clamp(targetPos.y - gridPos.y, -movement, movement)), true);

                movement = Mathf.Clamp(movementCost, 0, movement);
                movement -= movementCost;

                Debug.Log(name + " used " + movementCost + " movement points. " + movement + " remaining.");

                yield return new WaitForSeconds(1);
            }

            Debug.Log(name + " decided to use " + randomCard);

            yield return new WaitForSeconds(0.5f);

            if (gridPos.y == targetPos.y)//if the enemy is in range to use the card
            {
                AttackHandler.UseAttack(gridPos, this, randomCard);
                hand.Remove(randomCard);

                if (!discardPile.ContainsKey(randomCard))
                {
                    discardPile.Add(randomCard, 0);
                }

                discardPile[randomCard]++;

                usedCard = true;

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

                        AttackHandler.UseAttack(gridPos, this, nextCard);
                        hand.Remove(nextCard);
                        discardPile[nextCard]++;

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

        gameObject.SetActive(false);
        Destroy(gameObject);
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

//----------Target Types----------\\

public class TargetType
{
    protected List<int> hitPositions;
    protected List<int> columnPositions;//if it contains 0 it will target friendly characters, if it contains 1 it will target hostile characters, it can contain both.
    protected List<int> standPositions = new List<int>();

    public List<int> GetStandPositions()//where enemies can be relative to a target in order for the attack to land
    {
        return standPositions;
    }

    public List<Vector2Int> GetHitPositions()
    {
        List<Vector2Int> returnThis = new List<Vector2Int>();

        foreach (int i in columnPositions)
        {
            foreach (int v in hitPositions)
            {
                returnThis.Add(new Vector2Int(i, v));
            }
        }
        return returnThis;
    }

    public virtual List<Character> GetTargets(Vector2Int userPos)
    {
        Debug.Log("Get targets called on TargetType superclass, use subclass instead.");
        return null;
    }
}

public class BasicTarget : TargetType
{
    public override List<Character> GetTargets(Vector2Int userPos)
    {
        List<Character> targets = new List<Character>();

        foreach (int offset in hitPositions)
        {
            foreach (int column in columnPositions)
            {
                targets.Add(CombatHandler.GetCharacter(new Vector2Int(Mathf.Abs(userPos.x - column), userPos.y + offset)));
            }
        }

        return targets;
    }

    public BasicTarget(List<int> hitPositions, List<int> columnPositions)
    {
        this.hitPositions = hitPositions;
        this.columnPositions = columnPositions;

        foreach(int i in hitPositions)
        {
            standPositions.Add(-i);
        }
    }

    public BasicTarget(List<int> hitPositions, List<int> columnPositions, List<int> standPositions)
    {
        this.hitPositions = hitPositions;
        this.columnPositions = columnPositions;
        this.standPositions = standPositions;
    }
}

//----------Effect Types----------\\

public class Effect
{
    public virtual void Activate(Character target, Character user)
    {
        Debug.Log("Activate called on Effect superclass, use subclass instead");
    }
}

public class ApplyStatus : Effect
{
    public static Dictionary<string, Type> statusEffects = new Dictionary<string, Type>
    {
        {"Guarded", typeof(Guarded)},
        {"Contagion", typeof(Contagion)},
        {"Poison", typeof(Poison)}
    };

    private string statusIndex;

    private int amount;

    public ApplyStatus(string statusIndex, int amount)
    {
        this.statusIndex = statusIndex;
        this.amount = amount;
    }

    public override void Activate(Character target, Character user)
    {
        if (target != null)
        {
            if (statusEffects.TryGetValue(statusIndex, out Type statusType))
            {
                ConstructorInfo cons = statusType.GetConstructor(new[] { typeof(int), typeof(Character) });

                if (cons != null)
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
    }
}

public class DamageDice : Effect
{
    private int amount;
    private int faces;
    private int modifier;

    public DamageDice(int amount, int faces, int modifier)
    {
        this.amount = amount;
        this.faces = faces;
        this.modifier = modifier;
    }

    public override void Activate(Character target, Character user)
    {
        if (target != null)
        {
            int damage = modifier;

            for (int i = 0; i < amount; i++)
            {
                damage += Random.Range(1, faces + 1);
            }

            List<int> health = target.TakeDamage(damage);

            Debug.Log(user.GetName() + " dealt " + health[1] + " damage to " + target.GetName() + ". " + target.GetName() + " is now at " + health[0] + " health.");
        }
    }
}

public class VisualEffectHandler : MonoBehaviour
{
    private static Dictionary<string, GameObject> VFXSprites = new Dictionary<string, GameObject>
    {
        { "Spear", Resources.Load<GameObject>("CombatPrefabs/VFX/Spear") },
        { "Guard", Resources.Load<GameObject>("CombatPrefabs/VFX/Guard") }
    };

    public static GameObject MakeObject(string spriteId, Vector3 pos)
    {
        return Instantiate(VFXSprites[spriteId], pos, Quaternion.identity);
    }

    public static void Destroy(GameObject obj, float time)
    {
        Destroy(obj);
    }
}

public class PokeVFX : Effect
{
    string spriteID;

    public PokeVFX(string spriteID)
    {
        this.spriteID = spriteID;
    }

    public override void Activate(Character target, Character user)
    {
        if (!target || ! user)
        {
            return;
        }

        user.StartCoroutine(Poke(target, user));
    }

    private IEnumerator Poke(Character target, Character user)
    {
        GameObject obj = VisualEffectHandler.MakeObject(spriteID, user.transform.position + new Vector3(0, 1.1f, 0.5f));

        SpriteRenderer spriteRenderer = obj.transform.GetComponent<SpriteRenderer>();

        spriteRenderer.color = new Color32(255, 255, 255, 0);
        obj.transform.right = user.transform.position - (target.transform.position + new Vector3(0, 0.5f, 0));

        user.StartCoroutine(Tween.New(new Color32(255, 255, 255, 255), spriteRenderer, 0.1f));
        user.StartCoroutine(Tween.New(target.transform.position + new Vector3(0, 0.5f, 0), obj.transform, 0.5f));

        yield return new WaitForSeconds(0.3f);

        user.StartCoroutine(Tween.New(new Color32(255, 255, 255, 255), spriteRenderer, 0.2f));
        VisualEffectHandler.Destroy(obj, 0.3f);
    }
}

public class SelfApplyAnimVFX : Effect//Realy complex code, this one is.
{
    string spriteID;

    public SelfApplyAnimVFX(string spriteID)
    {
        this.spriteID = spriteID;
    }

    public override void Activate(Character target, Character user)
    {
        if (!target)
        {
            return;
        }

        VisualEffectHandler.MakeObject(spriteID, user.transform.position + new Vector3(0, 0.75f, -0.5f));
    }
}
//----------Status Effects----------\\

public class StatusEffect
{
    protected Character target;

    public string type;

    public int duration;

    public List<string> triggers;

    public StatusEffect(string type, Character target, List<string> triggers)
    {
        this.type = type;
        this.target = target;
        this.triggers = triggers;
    }

    public virtual void Stack(int amount)
    {
        Debug.Log("Stack called on StatusEffect superclass, use subclass isntead");
    }

    public virtual void Activate()
    {
        Debug.Log("void Activate called on StatusEffect superclass, use subclass instead.");
    }

    public virtual int Activate(int armount)
    {
        Debug.Log("int Activate called on StatusEffect superclass, use subclass instead.");
        return 0;
    }

    public virtual bool Reduce()
    {
        Debug.Log("Reduce called on StatusEffect superclass, use subclass isntead.");
        return true;
    }
}

public class Guarded : StatusEffect
{
    public Guarded(int duration, Character target) : base("Guarded", target, new List<string> { "ModifyTakenDamage", "ReduceOnTurnStart" })
    { 
        this.duration = duration;
    }

    public override bool Reduce()
    {
        duration--;

        Debug.Log("Guarded decreased, new value: " + duration);

        if (duration <= 0)
        {
            Debug.Log("Guarded wore off on " + target.name + ".");
            target.RemoveStatus(this);

            return false;
        }

        return true;
    }

    public override void Stack(int duration)
    {
        Debug.Log("Stacked Guarded, new value: " + this.duration);
        this.duration += duration;
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
    public Contagion(int duration, Character target) : base("Contagion", target, new List<string> { "ModifyTakenDamage",  "ReduceOnTurnStart" })
    {
        this.duration = duration;
    }

    private void Explode()
    {
        Debug.Log(target.name + "'s infection progressed to its final stage.");
        List<int> health = target.TakeDamage(Random.Range(1, 11));

        for (int i = -1; i < 3; i += 2)
        {
            Character newTarget = CombatHandler.GetCharacter(target.GetGridPos() + new Vector2Int(0, i));

            if (newTarget != null)
            {
                AttackHandler.UseAttack(newTarget.GetGridPos(), target, "Contagion");

                if (health[0] <= 0)
                {
                    target.TakeDamage(Random.Range(1, 8));
                }
                else
                {
                    target.TakeDamage(Random.Range(1, 4));
                }
            }
        }

        target.RemoveStatus(this);
    }

    public override bool Reduce()
    {
        duration++;

        Debug.Log("Increased contagion, new value: " + duration);

        if (duration >= 4)
        {
            Explode();

            return false;
        }

        return true;
    }

    public override void Stack(int duration)
    {
        this.duration += duration;
        Debug.Log("Stacked contagion, new value: " + this.duration);

        if (this.duration >= 4)
        {
            Explode();
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
    public Poison(int duration, Character target) : base("Poison", target, new List<string> {"ReduceOnTurnStart"})
    {
        this.duration = duration;
    }

    public override bool Reduce()
    {
        duration--;

        target.TakeDamage(Random.Range(1, 5));

        Debug.Log("Poison decreased, new duration: " + duration);

        if (duration <= 0)
        {
            Debug.Log("Poison wore off on " + target.name + ".");
            target.RemoveStatus(this);

            return false;
        }

        return true;
    }

    public override void Stack(int duration)
    {
        Debug.Log("Stacked Poison, new value: " + this.duration);
        this.duration += duration;
    }

    public override string ToString()
    {
        return "Poison";
    }
}

public class Oozed : StatusEffect
{
    public Oozed(int duration, Character target) : base("Oozed", target, new List<string> { "ReduceOnTurnStart", "ModifySpeed" })
    {
        this.duration = duration;
    }

    public override int Activate(int speed)
    {
        return Mathf.Clamp(speed - 1, 0, int.MaxValue);
    }

    public override bool Reduce()
    {
        duration--;

        Debug.Log("Oozed decreased, new duration: " + duration);

        if (duration <= 0)
        {
            Debug.Log("Oozed wore off on " + target.name + ".");
            target.RemoveStatus(this);

            return false;
        }

        return true;
    }

    public override void Stack(int duration)
    {
        Debug.Log("Stacked Oozed, new value: " + this.duration);
        this.duration += duration;
    }

    public override string ToString()
    {
        return "Oozed";
    }
}
//----------Cards----------\\

public class Attack
{
    protected Dictionary<TargetType, List<Effect>> targetEffects;

    public List<TargetType> GetTargetTypes()
    {
        List<TargetType> targetTypes = new List<TargetType>();

        foreach (TargetType targetType in targetEffects.Keys)
        {
            targetTypes.Add(targetType);
        }

        return targetTypes;
    }

    public Attack(Dictionary<TargetType, List<Effect>> targetEffects)
    {
        this.targetEffects = targetEffects;
    }

    public virtual void Activate(Vector2Int userPos, Character user)
    {
        Debug.Log("Activate called on Attack superclass, use subclass isntead.");
    }
}

public class BasicAttack : Attack
{
    public BasicAttack(Dictionary<TargetType, List<Effect>> targetEffects) : base(targetEffects) {}

    public override void Activate(Vector2Int userPos, Character user)
    {
        foreach (TargetType targetType in targetEffects.Keys)
        {
            List<Character> targets = targetType.GetTargets(userPos);

            foreach (Character character in targets)
            {
                foreach (Effect effect in targetEffects[targetType])
                {
                    effect.Activate(character, user);
                }
            }
        }
    }
}

public class Card : MonoBehaviour
{
    private List<GameObject> chainObjects = new List<GameObject>();

    private GameObject spikePrefab;

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

        transform.position = new Vector3(-5, -3.8f, 0);
        transform.rotation = Quaternion.Euler(0, 0, -10);

        gameObject.name = character.name + " " + cardInfo["Name"];
        animator = transform.GetComponent<Animator>();
        button = transform.Find("Root").GetComponent<Button>();

        button.onClick.AddListener(Activate);

        Transform frontCard = transform.Find("Root").Find("FrontCard");

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
        float xPos = 9 * (index + 0.5f) / hand.Count - 2;
        float yPos = -0.05f * Mathf.Pow(xPos - 2.5f, 2) - 4;
        float zPos = 0.15f * Mathf.Pow(xPos - 2.0f, 2) - 15;
        float rotation = -Mathf.Pow(xPos - 2.5f, 3) / 5;

        Vector3 newPos = new Vector3(xPos, yPos, zPos);

        gameObject.SetActive(true);

        StartCoroutine(Tween.New(newPos, transform, 0.25f));
        StartCoroutine(Tween.New(Quaternion.Euler(0, 0, rotation), transform, 0.25f));
    }

    public void OnMouseEnter()
    {
        animator.SetBool("Selected", true);
        animator.Play("Select");

        StartCoroutine(Visualize());
    }

    public void OnMouseExit()
    {
        StartCoroutine(RemoveChain());

        animator.SetBool("Selected", false);
        List<Card> hand = ((Player)character).GetHand();

        Organize(hand, hand.IndexOf(this));
    }

    private void Activate()
    {
        if (debounce)
        {
            return;
        }

        Debug.Log(character.name + " used " + cardInfo["Name"] + ".");

        debounce = true;

        StartCoroutine(RemoveChain());

        attack.Activate(character.GetGridPos(), character);

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
            foreach(Vector2Int targetPos in targetType.GetHitPositions())
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
                Vector3 pos = CombatHandler.getNewPos(new Vector2Int(targetPos.x, targetPos.y + gridPos.y));

                GameObject chainObject = Instantiate(spikePrefab, pos, Quaternion.identity);
                SpriteRenderer chainRender = chainObject.transform.GetComponent<SpriteRenderer>();

                chainObjects.Add(chainObject);

                chainObject.transform.position += new Vector3(0, Mathf.Abs(gridPos.x - targetPos.x) == 0 ? -1.5f : 3.2f, 0);
                chainObject.transform.rotation = Quaternion.Euler(0, 0, Mathf.Abs(gridPos.x - targetPos.x) == 1 ? 180 : 0);

                chainRender.color = new Color32(255, 0, 0, 0);

                StartCoroutine(Tween.New(new Color32(255, 255, 255, 255), chainRender, 0.25f));
                StartCoroutine(Tween.New(chainObject.transform.position + new Vector3(0, Mathf.Abs(gridPos.x - targetPos.x) == 0 ? 1 : -1.5f, 0), chainObject.transform, 0.25f));
            }
        }

        yield return 0;

        visualDebounce = false;
    }

    private IEnumerator RemoveChain()
    {
        foreach(GameObject chainObject in chainObjects)
        {
            Destroy(chainObject, debounce ? 0 : 0.5f);
            StartCoroutine(Tween.New(new Color32(0, 0, 0, 0), chainObject.transform.GetComponent<SpriteRenderer>(), 0.25f));
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
        Destroy(gameObject);
    }
}

//----------Misc----------\\

public class Tween
{
    public static IEnumerator New(Vector3 targetPos, Transform transform, float tweenTime)
    {
        float startTime = Time.time;

        Vector3 startPos = transform.position;

        while (Time.time - startTime <= tweenTime && transform)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, Mathf.Clamp((Time.time - startTime) / tweenTime, 0, 1));

            yield return null;
        }

        if (transform)
        {
            transform.position = targetPos;
        }
    }

    public static IEnumerator New(Quaternion targetRot, Transform transform, float tweenTime)
    {
        float startTime = Time.time;

        Quaternion startRot = transform.rotation;

        while (Time.time - startTime <= tweenTime && transform)
        {
            transform.rotation = Quaternion.Lerp(startRot, targetRot, Mathf.Clamp((Time.time - startTime) / tweenTime, 0, 1));

            yield return null;
        }

        if (transform)
        {
            transform.rotation = targetRot;
        }
    }

    public static IEnumerator New(Color32 targetColor, SpriteRenderer spriteRenderer, float tweenTime)
    {
        float startTime = Time.time;

        Color32 startColor = spriteRenderer.color;

        while (Time.time - startTime <= tweenTime && spriteRenderer)
        {
            spriteRenderer.color = Color32.Lerp(startColor, targetColor, Mathf.Clamp((Time.time - startTime) / tweenTime, 0, 1));

            yield return null;
        }

        if (spriteRenderer)
        {
            spriteRenderer.color = targetColor;
        }
    }
}

public class Bezeir : MonoBehaviour
{
    public static Vector3[] GetPointsAlongCurve(Vector3 startPos, Vector3 endPos, int amount)
    {
        Vector3[] points = new Vector3[amount];

        for (int i = 0; i < amount; i++)
        {
            float percent = i / (float)(amount - 1);

            Vector3 position = Mathf.Pow(1 - percent, 2) * startPos + 2 * (1 - percent) * percent * ((startPos + new Vector3(startPos.x, endPos.y, 0)) / 2) + Mathf.Pow(percent, 2) * endPos;

            points[i] = new Vector3(position.x, position.y, -5f);
        }

        return points;
    }
}
