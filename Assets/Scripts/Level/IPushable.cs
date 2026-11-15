/// <summary>
/// Something the player can walk into and push. PlayerPush tells it when a push starts,
/// so it can react (for example with a sound) without the player knowing what kind of object it is.
/// </summary>
public interface IPushable
{
    /// <summary>Called once when the player starts pushing, with how fast the player was walking.</summary>
    void PushStarted(float playerSpeed);
}
