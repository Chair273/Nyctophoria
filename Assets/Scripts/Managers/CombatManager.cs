using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public bool combat;

    private static GameObject cardPrefab;

    private Dictionary<string, Attack> attacks = new Dictionary<string, Attack>();//stores the actuall code behind each card
    private Dictionary<string, Dictionary<string, string>> cardInfo = new Dictionary<string, Dictionary<string, string>>();//stores the information of each card (used for player gui)
    private Dictionary<string, Dictionary<string, float>> cardSize = new Dictionary<string, Dictionary<string, float>>();//stores the font size of each card's name and description
    private Dictionary<string, TargetType> targetTypes = new Dictionary<string, TargetType>();//stores the different target types that cards can use

    public Card MakeCardObject(string cardName, Character character)
    {
        if (!attacks.ContainsKey(cardName))
        {
            Debug.LogWarning("Invalid card key");
            return null;
        }

        GameObject cardObject = Instantiate(cardPrefab);
        cardObject.transform.parent = character.transform.parent;
        cardObject.transform.GetComponent<Canvas>().worldCamera = MainManager.mainCamera;

        Card card = cardObject.AddComponent<Card>();
        card.New(attacks[cardName], cardInfo[cardName], cardSize[cardName], character);

        return card;
    }

    public List<int> GetStandPositions(string attackName)//remember to add a way to distinguish between cards that help and cards that hurt.
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

    public void DefineCards()
    {
        cardPrefab = Resources.Load<GameObject>("CombatPrefabs/Gui/Card"); ;

        //targetTypes initialization
        {
            targetTypes["ForwardHit"] = new BasicTarget(new List<int> { 0 }, new List<int> { 1 });
            targetTypes["DiagonalHit"] = new BasicTarget(new List<int> { -1, 1 }, new List<int> { 1 });

            targetTypes["AdjacentHit"] = new BasicTarget(new List<int> { -1, 1 }, new List<int> { 0 });
            targetTypes["AdjacentChoice"] = new RangedTarget(new List<int> { -1, 1 }, new List<int> { 0 });


            targetTypes["SelfEffect"] = new BasicTarget(new List<int> { 0 }, new List<int> { 0 }, new List<int> { -1, 0, 1 });

            targetTypes["SmallRangedAttack"] = new RangedTarget(new List<int> { -1, 0, 1 }, new List<int> { 1 });
        }


        //attacks
        {
            MakeCardIndex(new Dictionary<string, string>() //the name and description on the card
                {
                    {"Name", "Spear Strike"},
                    {"Description", "*The user deals 1d6 + 2 damage." }
                }, new Dictionary<string, float> //the font size of the name and description
                {
                    {"Name", 0.15f},
                    {"Description",  0.15f}
                }, new Attack(new Dictionary<TargetType, List<Effect>> //the dictionary containing the cards target types, and the effects corrisponding to each
                {
                    {targetTypes["ForwardHit"], new List<Effect> { new DamageDice(1, 6, 2), new PokeVFX("Spear"), new Weight(-1) } }//This card targets the character in the opposite column to the user, and deals 1d4 + 2 damage, thus it uses the ForwardHit target type and its only effect is a 1d4 + 2 DamageDice
                }));

            MakeCardIndex(new Dictionary<string, string>()
                {
                    {"Name", "Bifurcated Strike"},
                    {"Description", "*The user deals 1d4 damage along both diagonals."}
                }, new Dictionary<string, float>
                {
                    {"Name", 0.11f},
                    {"Description",  0.15f}
                }, new Attack(0.2f, new Dictionary<TargetType, List<Effect>> //optional argument to add a delay between each character effected by the attack.
                {
                    {targetTypes["DiagonalHit"], new List<Effect> { new DamageDice(1, 4, 0), new PokeVFX("Spear"), new Weight(-2) } }
                }));

            MakeCardIndex(new Dictionary<string, string>()
                {
                    {"Name", "Guard"},
                    {"Description", "*The user gains the guarded status for one turn.\n*Reduces damage by half."}
                }, new Dictionary<string, float>
                {
                    {"Name", 0.15f},
                    {"Description",  0.12f}
                }, new Attack(new Dictionary<TargetType, List<Effect>>
                {
                    {targetTypes["SelfEffect"], new List<Effect> { new ApplyStatus("Guarded", 1), new AnimVFX("Guard", new Vector3(0, 0.75f, -1f)), new Weight(-4) } },
                }));

            MakeCardIndex(new Dictionary<string, string>()
                {
                    {"Name", "Contagion"},
                    {"Description", "*Call forth a terrible disease to inflict apon your target.\n*Increases damage based on amount.\n*Contagious."}
                }, new Dictionary<string, float>
                {
                    {"Name", 0.15f},
                    {"Description",  0.1f}
                }, new Attack(new Dictionary<TargetType, List<Effect>>
                {
                    {targetTypes["ForwardHit"], new List<Effect> { new ApplyStatus("Contagion", 1), new PokeVFX("Contagion"), new Weight(-2) } },
                }));

            MakeCardIndex(new Dictionary<string, string>()
                {
                    {"Name", "ContagionSpread"},
                    {"Description", "*The user's contagion becomes too much to bear, infesting those nearby.\n*You aren't supposed to be here."}
                }, new Dictionary<string, float>
                {
                    {"Name", 0.15f},
                    {"Description",  0.15f}
                }, new Attack(new Dictionary<TargetType, List<Effect>>
                {
                    {targetTypes["AdjacentHit"], new List<Effect> { new ApplyStatus("Contagion", 2), new PokeVFX("Contagion"), new ResetTargetList() } },
                    {targetTypes["ForwardHit"], new List<Effect> { new ApplyStatus("Contagion", 1), new PokeVFX("Contagion"), new ResetTargetList() } },
                    {targetTypes["SelfEffect"], new List<Effect> { new WaitEffect(0.2f), new Weight(-5) } }
                }));

            MakeCardIndex(new Dictionary<string, string>()
                {
                    {"Name", "Summon Bees"},
                    {"Description", "*Manifest a wave of bees.\n*Poisons and deals damage.\n*Effect diminishes with distance."}
                }, new Dictionary<string, float>
                {
                    {"Name", 0.15f},
                    {"Description",  0.1f}
                }, new Attack(0.1f, new Dictionary<TargetType, List<Effect>>
                {
                    {targetTypes["ForwardHit"], new List<Effect> { new DamageDice(1, 4, 0), new ApplyStatus("Poison", 3), new PokeVFX("BeeLarge"), new ResetTargetList() } },
                    {targetTypes["DiagonalHit"], new List<Effect> {new DamageDice(1, 2, 0), new ApplyStatus("Poison", 2), new PokeVFX("BeeSmall"), new Weight(-2) } }
                }));

            MakeCardIndex(new Dictionary<string, string>()
                {
                    {"Name", "Lesser Ooze"},
                    {"Description", "*Coat your enemy in a debilitating slime.\n*Reduces movement for 3 turns.\n*Short range"}
                }, new Dictionary<string, float>
                {
                    {"Name", 0.15f},
                    {"Description",  0.1f}
                }, new Attack(new Dictionary<TargetType, List<Effect>>
                {
                    {targetTypes["SmallRangedAttack"], new List<Effect> { new ApplyStatus("Oozed", 2), new AnimVFX("Ooze", new Vector3(0, 0, -0.5f)), new Weight(-1) } },
                }));

            MakeCardIndex(new Dictionary<string, string>()
                {
                    {"Name", "Lunge"},
                    {"Description", "*Lunge forwards and deal heavy damage.\n*Directional choice.\n*Strike diagonally in direction of movement."}
                }, new Dictionary<string, float>
                {
                    {"Name", 0.15f},
                    {"Description",  0.1f}
                }, new Attack(new Dictionary<TargetType, List<Effect>>
                {
                    {targetTypes["AdjacentChoice"], new List<Effect> { new RecordUserPos(), new MoveUser(0.15f), new RetargetFromMovement(1, 1), new DamageDice(1, 10, 0), new PokeVFX("Spear"), new Weight(-3) } },
                }));
        }

        //items
        {
            MakeCardIndex(new Dictionary<string, string>()
                {
                    {"Name", "Placebo"},
                    {"Description", "*Decreases negative status effects.\n*All in your head."}
                }, new Dictionary<string, float>
                {
                    {"Name", 0.15f},
                    {"Description",  0.12f}
                }, new Attack(new Dictionary<TargetType, List<Effect>>
                {
                    {targetTypes["SelfEffect"], new List<Effect> { new ModifyStatus(new Dictionary<string, int> { { "Contagion", -2 }, { "Poison", -2 }, { "Oozed", -1 } }) } },
                }));

            MakeCardIndex(new Dictionary<string, string>()
                {
                    {"Name", "Rabbit Bones"},
                    {"Description", "*Ameliorates the weight of combat."}
                }, new Dictionary<string, float>
                {
                    {"Name", 0.13f},
                    {"Description",  0.15f}
                }, new Attack(new Dictionary<TargetType, List<Effect>>
                {
                    {targetTypes["SelfEffect"], new List<Effect> { new ChangeExhaustionDC(-2), new Weight(-5) } },
                }));
        }
    }

    public void UseAttack(string cardIndex, Character user)
    {
        StartCoroutine(attacks[cardIndex].Activate(user));
    }

    public void Begin()
    {
        combat = true;
        CombatHandler.main.Begin();
    }

    private void MakeCardIndex(Dictionary<string, string> newCardInfo, Dictionary<string, float> newCardSize, Attack attack)
    {
        attacks.Add(newCardInfo["Name"], attack);
        cardInfo.Add(newCardInfo["Name"], newCardInfo);
        cardSize.Add(newCardInfo["Name"], newCardSize);
    }
}
