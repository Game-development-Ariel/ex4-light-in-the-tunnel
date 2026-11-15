using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerRespawnTests : PlayModeTest
{
    private GameEvent playerCaught;
    private Transform player;
    private Transform start;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        playerCaught = Track(TestData.Event());
        start = Make("PlayerStart").transform;
        start.position = new Vector3(-3f, 2f, 7f);

        GameObject hero = MakeOff("Player");
        TestHero.Create(hero);
        PlayerRespawn respawn = hero.AddComponent<PlayerRespawn>();
        TestUtils.Set(respawn, "startPoint", start);
        TestUtils.Set(respawn, "playerCaught", playerCaught);
        hero.transform.position = new Vector3(4f, -1f, 0f);
        hero.SetActive(true);
        player = hero.transform;
        yield return null;
    }

    [UnityTest]
    public IEnumerator Caught_JumpsToTheStartPoint_AtOnce()
    {
        playerCaught.Raise();

        Assert.AreEqual(-3f, player.position.x, 1e-4f);
        Assert.AreEqual(2f, player.position.y, 1e-4f);
        yield return null;
        Assert.AreEqual(-3f, player.position.x, 1e-4f, "The controller didn't pull the player back");
    }

    [UnityTest]
    public IEnumerator Caught_KeepsThePlayersDepth()
    {
        playerCaught.Raise();
        yield return null;

        Assert.AreEqual(0f, player.position.z, 1e-4f, "The player stays on the map's plane");
    }

    [UnityTest]
    public IEnumerator Caught_LeavesTheControllerOn()
    {
        playerCaught.Raise();
        yield return null;

        Assert.IsTrue(player.GetComponent<CharacterController>().enabled);
    }

    [UnityTest]
    public IEnumerator Disabled_DoesNotListen()
    {
        player.GetComponent<PlayerRespawn>().enabled = false;

        playerCaught.Raise();
        yield return null;

        Assert.AreEqual(4f, player.position.x, 1e-4f);
    }
}
