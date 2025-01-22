using UnityEngine;

public class FPSCharController : MonoBehaviour
{
    public float speed = 5.0f;
    private float gravity = 0f;
    private Vector3 velocity;

    private CharacterController characterController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Get input
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Calculate movement direction
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        // Apply movement
        characterController.Move(move * speed * Time.deltaTime);

        // Apply gravity
        if (!characterController.isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else
        {
            velocity.y = 0; // Reset velocity when grounded
        }

        characterController.Move(velocity * Time.deltaTime);
    }
}
