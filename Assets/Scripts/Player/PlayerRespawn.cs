using UnityEngine;

/// <summary>
/// Sends the player back to the start point (the cell) when the player is caught.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private Transform startPoint;
    [SerializeField] private GameEvent playerCaught;

    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        playerCaught.Subscribe(GoToStart);
    }

    private void OnDisable()
    {
        playerCaught.Unsubscribe(GoToStart);
    }

    private void GoToStart()
    {
        // A CharacterController puts the object back where it was if we move the transform while it is on,
        // so it is turned off for the jump.
        controller.enabled = false;
        transform.position = new Vector3(startPoint.position.x, startPoint.position.y, transform.position.z);
        controller.enabled = true;
    }
}
