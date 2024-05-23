using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

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

    public virtual IEnumerator GetTargetPos(Vector2Int userPos, Action<List<Vector2Int>> callback)
    {
        return null;
    }
}

public class BasicTarget : TargetType
{
    public override IEnumerator GetTargetPos(Vector2Int userPos, Action<List<Vector2Int>> callback)
    {
        List<Vector2Int> targets = new List<Vector2Int>();

        foreach (int offset in hitPositions)
        {
            foreach (int column in columnPositions)
            {
                targets.Add(new Vector2Int(Mathf.Abs(userPos.x - column), userPos.y + offset));
            }
        }

        yield return new WaitForFixedUpdate();

        callback(targets);
    }

    public BasicTarget(List<int> hitPositions, List<int> columnPositions)
    {
        this.hitPositions = hitPositions;
        this.columnPositions = columnPositions;

        foreach (int i in hitPositions)
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

public class RangedTarget : TargetType
{
    public RangedTarget(List<int> hitPositions, List<int> columnPositions)
    {
        this.hitPositions = hitPositions;
        this.columnPositions = columnPositions;

        foreach (int i in hitPositions)
        {
            standPositions.Add(-i);
        }
    }

    public override IEnumerator GetTargetPos(Vector2Int userPos, Action<List<Vector2Int>> callback)
    {
        if (userPos.x == 0)//if the user is a player
        {
            yield return new WaitUntil(() => !Input.GetMouseButton(0));

            GameObject chainObject = Object.Instantiate(Resources.Load<GameObject>("CombatPrefabs/Gui/Spike"), new Vector3(0, 0, -2), Quaternion.identity);
            Vector3 closestPos = new Vector3(-100, 0, 0);
            Vector2Int closestGrid = new Vector2Int();

            while (!Input.GetMouseButton(0))
            {
                Vector3 mouseWorldPos = MainManager.mainCamera.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = -2;

                float closest = Vector3.Distance(mouseWorldPos, closestPos);

                foreach (int offset in hitPositions)
                {
                    if (userPos.y + offset >= 0 && userPos.y + offset <= 5)
                    {
                        foreach (int column in columnPositions)
                        {
                            Vector3 worldPos = CombatHandler.main.getNewPos(new Vector2Int((int)MathF.Abs(userPos.x - column), userPos.y + offset));
                            float newDistance = Vector3.Distance(mouseWorldPos, worldPos);

                            if (newDistance < closest)
                            {
                                closestPos = worldPos;
                                closestGrid = new Vector2Int(Mathf.Abs(userPos.x - column), userPos.y + offset);
                            }
                        }
                    }
                }

                chainObject.transform.position = CombatHandler.main.getNewPos(closestGrid) + new Vector3(0, closestGrid.x == 0 ? -0.5f : 1.7f, 0);
                chainObject.transform.rotation = Quaternion.Euler(0, 0, closestGrid.x == 0 ? 0 : 180);

                yield return new WaitForFixedUpdate();
            }

            Object.Destroy(chainObject);

            callback(new List<Vector2Int> { closestGrid });
        }
        else//if the user is a enemy
        {
            Character target = CombatHandler.main.GetCharacter(new Vector2Int(0, userPos.y));

            if (target != null)
            {
                callback(new List<Vector2Int> { target.GetGridPos() });
            }
            else
            {
                int lowestHealth = int.MaxValue;

                foreach (int offset in hitPositions)
                {
                    Character newTarget = CombatHandler.main.GetCharacter(new Vector2Int(0, userPos.y + offset));

                    if (newTarget != null && newTarget.GetHealth() < lowestHealth)
                    {
                        target = newTarget;
                        lowestHealth = newTarget.GetHealth();
                    }
                }

                if (target != null)
                {
                    callback(new List<Vector2Int> { target.GetGridPos() });
                }
                else
                {
                    callback(new List<Vector2Int>());
                }
            }
        }
    }
}