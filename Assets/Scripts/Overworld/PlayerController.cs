using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public bool canMove;

    public float Speed;
    public float size;

    public Animator animator;

    private Rigidbody2D Rigidbody2D;

    void Start()
    {
        Rigidbody2D = transform.GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (!MainManager.combatManager.combat && canMove)
        {
            Vector3 MoveVector = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0);

            animator.SetBool("Moving", !MoveVector.Equals(Vector3.zero));
            animator.SetBool("Up", MoveVector.y > 0);

            Rigidbody2D.MovePosition(transform.position + new Vector3(MoveVector.x, MoveVector.y * (86f / 150f), 0) * Time.deltaTime * Speed);
            transform.position = new Vector3(transform.position.x, transform.position.y, 0);

            float yScale = MoveVector.y != 0 ? MoveVector.y : -1;
            float xScale = MoveVector.x != 0 ? MoveVector.x : 1;

            transform.localScale = new Vector3(xScale * yScale * size, size, 1);
        }
    }
}