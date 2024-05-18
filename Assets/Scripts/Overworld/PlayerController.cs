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
        Vector3 MoveVector = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0);

        Rigidbody2D.MovePosition(transform.position + MoveVector * Time.deltaTime * Speed);

        if (Input.GetAxis("Vertical") > 0 || Input.GetAxis("Vertical") < 0)
        {
            animator.SetBool("Moving", true);
            animator.SetFloat("X axis", Input.GetAxis("Vertical"));
        }
        else
        {
            animator.SetBool("Moving", false);
        }

        Transform playerTransform = GetComponent<Transform>();
    }
}