using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.UI;
using System.Reflection;
using static UnityEngine.GraphicsBuffer;

//----------Game----------\\

public class CombatHandler : MonoBehaviour
{
    public GameObject _endTurnButton;//Static variables don't show in the inspector, so I have to do this to make it work.
    public static Button endTurnButton;

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
        endTurnButton = _endTurnButton.GetComponent<Button>();

        AttackHandler.Start();

        GameObject prefab = Resources.Load<GameObject>("CombatPrefabs/CharacterPlaceholder");

        GameObject OAKObject = Instantiate(prefab, new Vector3Int(), Quaternion.identity);//One Armed Knight
        OAKObject.transform.parent = transform;

        Player OAK = OAKObject.AddComponent(typeof(Player)) as Player;

        OAK.New(50, new Vector2Int(0, 1), "One Armed Knight", Resources.Load<Sprite>("CombatPrefabs/CharacterSprites/OneArmedKnight"), new List<string>
        {"Spear Strike", "Bifurcated Strike", "Guard"});

        participants.Add(OAK);

        GameObject PCObject = Instantiate(prefab, new Vector3Int(), Quaternion.identity);//Plague Caster
        PCObject.transform.parent = transform;

        Player PC = PCObject.AddComponent(typeof(Player)) as Player;

        PC.New(50, new Vector2Int(0, 3), "Plague Caster", Resources.Load<Sprite>("CombatPrefabs/CharacterSprites/PlagueCaster"), new List<string>
        {"Spear Strike", "Contagion"});

        participants.Add(PC);

        GameObject enemyObject = Instantiate(prefab, new Vector3Int(), Quaternion.identity);
        enemyObject.transform.parent = transform;

        Enemy enemy = enemyObject.AddComponent(typeof(Enemy)) as Enemy;
        enemy.New(40, new Vector2Int(1, Random.Range(0,5)), "Skeleton", Resources.Load<Sprite>("CombatPrefabs/CharacterSprites/Skeleton"), new List<string>
        {"Spear Strike", "Bifurcated Strike", "Guard"});

        participants.Add(enemy);

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
                }
            }

            yield return new WaitForSeconds(1);
            Debug.Log("Round end.");

            bool foundPlayer = false;
            bool foundEnemy = false;

            foreach (Character character in participants)
            {
                foundPlayer = character.GetGridPos().x == 0 || foundPlayer;
                foundEnemy = character.GetGridPos().x == 1 || foundEnemy;

                if (foundPlayer && foundEnemy)
                {
                    break;
                }
            }

            if (!foundPlayer || !foundEnemy)
            {
                gameEnded = true;
            }

            round++;
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
            -1);
    }
}

public class AttackHandler : MonoBehaviour//handles the creation and storage of each card
{
    private static Dictionary<string, Attack> attacks = new Dictionary<string, Attack>();//stores the actuall code behind each card

    private static Dictionary<string, Dictionary<string, string>> cardInfo = new Dictionary<string, Dictionary<string, string>>();//stores the information of each card (used for player gui)

    private static Dictionary<string, Dictionary<string, float>> cardSize = new Dictionary<string, Dictionary<string, float>>();//stores the font size of each card's name and description

    private static Dictionary<string, TargetType> targetTypes = new Dictionary<string, TargetType>();//stores the different target types that cards can use

    private static GameObject cardPrefab = Resources.Load<GameObject>("CombatPrefabs/Card");

    public static void Start()
    {
        //targetTypes initialization
        targetTypes["ForwardHit"] = new BasicTarget(new List<int> { 0 }, new List<int> { 1 });
        targetTypes["DiagonalHit"] = new BasicTarget(new List<int> { -1, 1 }, new List<int> { 1 });
        targetTypes["SelfEffect"] = new BasicTarget(new List<int> { 0 }, new List<int> { 0 }, new List<int> {-5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5 });

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
            {targetTypes["ForwardHit"], new List<Effect> { new DamageDice(1, 6, 2) } }//This card targets the character in the opposite column to the user, and deals 1d4 + 2 damage, thus it uses the ForwardHit target type and its only effect is a 1d4 + 2 DamageDice
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
            {targetTypes["DiagonalHit"], new List<Effect> { new DamageDice(1, 4, 0) } }
        }));

        MakeCardIndex(new Dictionary<string, string>()
        {
            {"Name", "Guard"},
            {"Description", "*The user gains the guarded status.\n*Repeated use increases the duration."}
        }, new Dictionary<string, float>
        {
            {"Name", 0.15f},
            {"Description",  0.12f}
        }, new BasicAttack(new Dictionary<TargetType, List<Effect>>
        {
            {targetTypes["SelfEffect"], new List<Effect> { new ApplyStatus("Guarded", 1) } },
        }));

        MakeCardIndex(new Dictionary<string, string>()
        {
            {"Name", "Contagion"},
            {"Description", "*Call forth a terrible disease to inflict apon your target."}
        }, new Dictionary<string, float>
        {
            {"Name", 0.15f},
            {"Description",  0.12f}
        }, new BasicAttack(new Dictionary<TargetType, List<Effect>>
        {
            {targetTypes["ForwardHit"], new List<Effect> { new ApplyStatus("Contagion", 1) } },
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
        {"Skeleton", new Color32(200, 200, 200, 255)}
    };

    protected string characterName;

    protected int health;
    protected int speed;
    protected int speedMod;
    protected int movement;

    protected Color32 baseColor;

    protected SpriteRenderer spriteRenderer;

    protected List<StatusEffect> statusEffects = new List<StatusEffect>();

    protected List<string> validCards = new List<string>();

    protected Vector2Int gridPos;

    public virtual void New(int health, Vector2Int gridPos, string name, Sprite sprite, List<string> validCards)//character constructor
    {
        this.health = health;
        this.gridPos = gridPos;
        this.name = name;
        characterName = name;
        this.validCards = validCards;

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

        Move(gridPos, false);
    }

    public virtual IEnumerator Turn(bool first)//signals that it is this character's turn
    {
        for (int i = 0; i < statusEffects.Count; i += 0)
        {
            StatusEffect status = statusEffects[i];

            if (status.triggers.Contains("ReduceOnTurnStart"))
            {
                Debug.Log(status.ToString());
                if (status.Reduce() == true)
                {
                    i++;
                }
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

        transform.position = CombatHandler.getNewPos(moveTo);
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
                Debug.Log(status.ToString());
                damage = status.Activate(damage);
            }
        }

        health -= damage;

        if (health <= 0)
        {
            CombatHandler.RemoveCharacter(this);
            Debug.Log(name + " has perished.");
            Destroy(gameObject);
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
    private bool isTurn = false;
    private bool turnEnd = false;
    private bool dragging = false;

    protected List<Card> hand = new List<Card>();

    public override void New(int health, Vector2Int gridPos, string name, Sprite sprite, List<string> validCards)//player constructor
    {
        base.New(health, gridPos, name, sprite, validCards);

        CombatHandler.endTurnButton.onClick.AddListener(Click);
    }

    public override IEnumerator Turn(bool first)
    {
        StartCoroutine(base.Turn(first));

        turnEnd = false;
        isTurn = true;

        movement = 2;

        if (first)
        {
            DrawCard(5);
        }
        else
        {
            foreach (Card card in hand)
            {
                card.gameObject.SetActive(true);
            }

            DrawCard(1);
        }

        yield return new WaitUntil(() => turnEnd);

        isTurn = false;
        movement = 0;

        foreach (Card card in hand)
        {
            card.gameObject.SetActive(false);

            yield return new WaitForSeconds(0.25f / hand.Count);
        }
    }

    public override void DrawCard(int amount)
    {
        StartCoroutine(DrawCardCoroutine(amount));
    }

    public IEnumerator DrawCardCoroutine(int amount)
    {
        List<Card> flipThese = new List<Card>();

        for (int i = 0; i < amount; i++)
        {
            Card card = AttackHandler.MakeCardObject(validCards[Random.Range(0, validCards.Count)], this);
            hand.Add(card);
            flipThese.Add(card);
        }

        foreach(Card card in hand)
        {
            if (!flipThese.Contains(card))
            {
                card.Organize(hand, hand.IndexOf(card));
            }
        }

        foreach(Card card in flipThese)
        {
            card.gameObject.SetActive(true);

            card.GetComponent<Animator>().Play("Card Flip");
            StartCoroutine(Tween.New(new Vector3(-5, 0, 0), card.transform, 0.2f));

            yield return new WaitForSeconds(0.5f);

            card.Organize(hand, hand.IndexOf(card));
        }
    }

    public override void RemoveCard(Card card)
    {
        hand.Remove(card);

        foreach (Card _card in hand)
        {
            _card.Organize(hand, hand.IndexOf(_card));
        }
    }

    public List<Card> GetHand()
    {
        return hand;
    }

    private void OnMouseDrag()
    {
        if (movement > 0 && isTurn)
        {
            dragging = true;

            Vector3 pos_move = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));
            transform.position = new Vector3(pos_move.x, pos_move.y, -2);
        }
    }

    private void OnMouseUp()
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

                Debug.Log(name + " used " + movementCost + " movement points. " + movement + " remaining.");

                Move(newPos, true);
            }
            else
            {
                transform.position = CombatHandler.getNewPos(gridPos);
            }
        }
    }

    private void Click()
    {
        if (isTurn && !turnEnd)
        {
            turnEnd = true;
        }
    }
}

public class Enemy : Character
{
    protected List<string> hand = new List<string>();

    public override void New(int health, Vector2Int gridPos, string name, Sprite sprite, List<string> validCards)
    {
        base.New(health, gridPos, name, sprite, validCards);
    }

    public override IEnumerator Turn(bool first)
    {
        StartCoroutine(base.Turn(first));

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
    }

    public override void DrawCard(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            hand.Add(validCards[Random.Range(0, validCards.Count)]);
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
        {"Contagion", typeof(Contagion)}
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
        List<int> health = target.TakeDamage(Random.Range(1, 10));

        for (int i = -1; i < 2; i += 2)
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

        Debug.Log("Increased contagion, new value: " + this.duration);

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

        if (duration >= 4)
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
    private List<List<GameObject>> chainObjects = new List<List<GameObject>>();

    private GameObject spikePrefab;
    private GameObject chainPrefab;
    private GameObject chainLinkPrefab;

    private Dictionary<string, string> cardInfo = new Dictionary<string, string>();    

    private Attack attack;

    private Button button;

    private Character character;

    private Animator animator;

    private bool debounce = false;
    private bool visualDebounce = false;

    public bool move = false;

    public Vector3 goHere = new Vector3(-5, -4.3f, 0);

    public void Awake()
    {
        spikePrefab = Resources.Load<GameObject>("CombatPrefabs/Spike");
        chainPrefab = Resources.Load<GameObject>("CombatPrefabs/Chain");
        chainLinkPrefab = Resources.Load<GameObject>("CombatPrefabs/ChainLink");
    }

    public void New(Attack attack, Dictionary<string, string> cardInfo, Dictionary<string, float> cardSize, Character character)
    {
        this.attack = attack;
        this.cardInfo = cardInfo;
        this.character = character;

        transform.position = goHere;
        gameObject.name = character.name + " " + cardInfo["Name"];
        animator = transform.GetComponent<Animator>();

        Transform frontCard = transform.Find("FrontCard");

        button = frontCard.GetComponent<Button>();

        button.onClick.AddListener(Activate);

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
        float zPos = 0.1f * Mathf.Pow(xPos - 2.0f, 2) - 10;
        float rotation = -Mathf.Pow(xPos - 2.5f, 3) / 5;

        Vector3 newPos = new Vector3(xPos, yPos, zPos);

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

        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private IEnumerator Visualize()
    {
        if (visualDebounce)
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

            chainObjects.Add(new List<GameObject>());

            if (targetPos.y + gridPos.y >= 0 && targetPos.y + gridPos.y <= 5)
            {
                Vector3 endPos = CombatHandler.getNewPos(new Vector2Int(targetPos.x, targetPos.y + gridPos.y));

                int amount = Mathf.Clamp((int)(
                    Vector3.Distance(transform.position, (transform.position + new Vector3(transform.position.x, endPos.y, 0)) / 2) +
                    Vector3.Distance((transform.position + new Vector3(transform.position.x, endPos.y, 0)) / 2, endPos) * 2.5f + 0.5f),
                    3, 30);

                if (amount % 2 == 0)
                {
                    amount += 1;
                }

                Vector3[] bezeir = Bezeir.GetPointsAlongCurve(transform.position, endPos, amount);

                chainObjects[t].Add(Instantiate(chainPrefab, bezeir[0], Quaternion.identity));

                for (int i = 1; i < amount - 1; i++)
                {
                    if (i == amount - 2)
                    {
                        chainObjects[t].Add(Instantiate(spikePrefab, bezeir[i], Quaternion.identity));
                        chainObjects[t][i].transform.position += new Vector3(0, 0, -2);
                    }
                    else if(i%2 == 0)
                    {
                        chainObjects[t].Add(Instantiate(chainLinkPrefab, bezeir[i], Quaternion.identity));
                        chainObjects[t][i].transform.position += new Vector3(0, 0, -1);
                    }
                    else
                    {
                        chainObjects[t].Add(Instantiate(chainPrefab, bezeir[i], Quaternion.identity));
                    }

                    float size = i / amount / 2;

                    chainObjects[t][i].transform.localScale -= new Vector3(size, size, 0);

                    //chainObjects[t][i].gameObject.SetActive(false);

                    Vector2 direction = (bezeir[i - 1] - bezeir[i]).normalized;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                    direction = (bezeir[i + 1] - bezeir[i]).normalized;
                    angle += (float)(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

                    angle /= 2;

                    if (i != amount - 2)
                    {
                        angle -= 90;
                    }

                    chainObjects[t][i].transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                }

            }
        }

        /*foreach (List<GameObject> list in chainObjects)
        {
            for (int i = 0; i < chainObjects.Count && !debounce; i++)
            {
                list[i].SetActive(true);

                yield return 0;
            }
        }*/

        yield return 0;

        visualDebounce = false;
    }

    private IEnumerator RemoveChain()
    {
        //yield return new WaitUntil(() => !visualDebounce);

        visualDebounce = true;
        foreach (List<GameObject> list in chainObjects)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Destroy(list[i]);
                list.RemoveAt(i);
                //yield return 0;
            }   
        }

        chainObjects = new List<List<GameObject>>();
        yield return 0;
        visualDebounce = false;
    }
}

//----------Misc----------\\

public class Tween
{
    public static IEnumerator New(Vector3 targetPos, Transform transform, float tweenTime)
    {
        float startTime = Time.time;

        Vector3 startPos = transform.position;

        while (Time.time - startTime <= tweenTime)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, Mathf.Clamp((Time.time - startTime) / tweenTime, 0, 1));

            yield return null;
        }

        transform.position = targetPos;
    }

    public static IEnumerator New(Quaternion targetRot, Transform transform, float tweenTime)
    {
        float startTime = Time.time;

        Quaternion startRot = transform.rotation;

        while (Time.time - startTime <= tweenTime)
        {
            transform.rotation = Quaternion.Lerp(startRot, targetRot, Mathf.Clamp((Time.time - startTime) / tweenTime, 0, 1));

            yield return null;
        }

        transform.rotation = targetRot;
    }

    public static IEnumerator New(Color32 targetColor, SpriteRenderer spriteRenderer, float tweenTime)
    {
        float startTime = Time.time;

        Color32 startColor = spriteRenderer.color;

        while (Time.time - startTime <= tweenTime)
        {
            spriteRenderer.color = Color32.Lerp(startColor, targetColor, Mathf.Clamp((Time.time - startTime) / tweenTime, 0, 1));

            yield return null;
        }

        spriteRenderer.color = targetColor;
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

            points[i] = new Vector3(position.x, position.y, -2f);
        }

        return points;
    }
}
