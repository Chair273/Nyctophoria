using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D Rigidbody2D;

    public float Speed;

    public Animator animator;

    void Start()
    {
        Rigidbody2D = transform.GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        Vector3 MoveVector = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0);

        animator.SetBool("Moving", !MoveVector.Equals(Vector3.zero));
        animator.SetBool("Up", MoveVector.y > 0);

        Rigidbody2D.MovePosition(transform.position + new Vector3(MoveVector.x, MoveVector.y * (86f / 150f), 0) * Time.deltaTime * Speed);

        float yScale = MoveVector.y != 0 ? MoveVector.y : -1;
        float xScale = MoveVector.x != 0 ? MoveVector.x : 1;

        transform.localScale = new Vector3(xScale * yScale * 0.4f, 0.4f, 1);

    }
}