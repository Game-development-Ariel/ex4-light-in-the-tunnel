using UnityEngine;

/// <summary>
/// Builds a player in code, like the scene's player: a CharacterController that is a ball on the X-Y plane,
/// with PlayerMovement and its default keys (WASD and arrows).
/// </summary>
public static class TestHero
{
    public static PlayerMovement Create(GameObject off, float moveSpeed = 2f)
    {
        off.layer = LayerMask.NameToLayer("Ignore Raycast");
        var controller = off.AddComponent<CharacterController>();
        controller.radius = 0.3f;
        controller.height = 0.6f;
        controller.stepOffset = 0f;
        controller.skinWidth = 0.02f;
        controller.minMoveDistance = 0f;
        PlayerMovement movement = off.AddComponent<PlayerMovement>();
        TestUtils.Set(movement, "moveSpeed", moveSpeed);
        TestUtils.Call(movement, "OnValidate"); // Adds the default keys, as the editor does.
        return movement;
    }

    /// <summary>A static wall: a box collider, like the wall colliders of the map.</summary>
    public static GameObject Wall(GameObject wall, Vector3 position, Vector3 size)
    {
        wall.transform.position = position;
        wall.AddComponent<BoxCollider>().size = size;
        return wall;
    }
}
