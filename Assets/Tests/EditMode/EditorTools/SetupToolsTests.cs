using NUnit.Framework;
using UnityEngine;

public class SetupToolsTests : EditModeTest
{
    [Test]
    public void SetField_SetsAPrivateSerializedReference()
    {
        var round = Make("Round").AddComponent<TunnelRound>();
        GameEvent caught = Track(TestData.Event());

        SetupTools.SetField(round, "playerCaught", caught);

        Assert.AreSame(caught, TestUtils.Get<GameEvent>(round, "playerCaught"));
    }

    [Test]
    public void SetField_WithAWrongName_LogsAnError()
    {
        var round = Make("Round").AddComponent<TunnelRound>();
        UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "TunnelRound has no field called noSuchField.");

        SetupTools.SetField(round, "noSuchField", null);
    }

    [Test]
    public void SetArray_SetsEveryElement()
    {
        var freeze = Make("Round").AddComponent<LevelFreeze>();
        GameObject[] objects = { Make("A"), Make("B") };

        SetupTools.SetArray(freeze, "stopWhenLevelEnds", objects);

        CollectionAssert.AreEqual(objects, TestUtils.Get<GameObject[]>(freeze, "stopWhenLevelEnds"));
    }

    [Test]
    public void GetOrAdd_DoesNotAddASecondComponent()
    {
        GameObject player = Make("Player");

        Hope first = SetupTools.GetOrAdd<Hope>(player);
        Hope second = SetupTools.GetOrAdd<Hope>(player);

        Assert.AreSame(first, second);
        Assert.AreEqual(1, player.GetComponents<Hope>().Length);
    }

    [Test]
    public void Recreate_ReplacesTheOldObject()
    {
        GameObject old = Make("Recreated Thing");

        GameObject made = Track(SetupTools.Recreate("Recreated Thing"));

        Assert.IsTrue(old == null);
        Assert.AreEqual("Recreated Thing", made.name);
    }

    [Test]
    public void TunnelAssets_PointToTheRealAssets()
    {
        Assert.IsNotNull(TunnelAssets.BottlePrefab);
        Assert.AreEqual("PlayerCaught", TunnelAssets.PlayerCaught.name);
        Assert.AreEqual("TutorialLevelSettings", TunnelAssets.LevelSettings.name);
        Assert.AreEqual("GuardVision", TunnelAssets.GuardVision.name);
        Assert.AreEqual("CrateSounds", TunnelAssets.CrateSounds.name);
    }
}
