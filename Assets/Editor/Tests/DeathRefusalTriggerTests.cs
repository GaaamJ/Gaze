using System.Collections;
using NUnit.Framework;
using ProjectT.Interaction;
using ProjectT.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjectT.Death.Tests
{
    // Play 이후의 흐름은 DeathRefusalTests가 본다. 여기서는 각 입구가 Play를 부르는지만 본다.
    public class DeathRefusalTriggerTests : DeathRefusalFixture
    {
        static DeathRefusalZone NewZone(string nodeId)
        {
            var go = new GameObject("RefusalZone");
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            var zone = go.AddComponent<DeathRefusalZone>();
            Set(zone, so => so.FindProperty("refusalNodeId").stringValue = nodeId);
            return zone;
        }

        static Collider2D NewLaris(out Collider2D detectTrigger)
        {
            var go = new GameObject("LarisBody");
            go.AddComponent<PlayerMovement>();
            var body = go.AddComponent<BoxCollider2D>();
            detectTrigger = go.AddComponent<CircleCollider2D>();
            detectTrigger.isTrigger = true;
            return body;
        }

        int DialogueCount()
        {
            return calls.FindAll(call => call.StartsWith("dialogue:")).Count;
        }

        [UnityTest]
        public IEnumerator Zone_PlaysOncePerEntry()
        {
            var zone = NewZone("Death_Falling001_Refuse");
            var body = NewLaris(out _);

            zone.HandleEntered(body);
            yield return WaitUntilDone();
            Assert.AreEqual(1, DialogueCount());

            zone.HandleEntered(body);
            yield return WaitUntilDone();
            Assert.AreEqual(1, DialogueCount(), "나가지 않고 다시 들어온 판정에는 재생하지 않는다.");

            zone.HandleExited(body);
            zone.HandleEntered(body);
            yield return WaitUntilDone();
            Assert.AreEqual(2, DialogueCount(), "나갔다가 다시 들어오면 재생한다.");
            Assert.AreEqual("dialogue:Death_Falling001_Refuse:locked", calls[1]);
            Assert.IsFalse(inputLock.IsLocked);
        }

        [Test]
        public void Zone_IgnoresDetectTriggerAndOthers()
        {
            var zone = NewZone("Death_Falling001_Refuse");
            NewLaris(out var detectTrigger);
            var other = new GameObject("Box").AddComponent<BoxCollider2D>();

            zone.HandleEntered(detectTrigger);
            zone.HandleEntered(other);

            Assert.IsFalse(DeathRefusal.IsActive);
            CollectionAssert.IsEmpty(calls);
        }

        [UnityTest]
        public IEnumerator Zone_OtherColliderExit_DoesNotRearm()
        {
            var zone = NewZone("Death_Falling001_Refuse");
            var body = NewLaris(out var detectTrigger);

            zone.HandleEntered(body);
            yield return WaitUntilDone();
            zone.HandleExited(detectTrigger);
            zone.HandleEntered(body);
            yield return WaitUntilDone();

            Assert.AreEqual(1, DialogueCount());
        }

        [UnityTest]
        public IEnumerator Interactable_Interact_PlaysRefusal()
        {
            var corpse = new GameObject("Corpse_DeathObject");
            var target = corpse.AddComponent<DeathRefusalInteractable>();
            Set(target, so => so.FindProperty("refusalNodeId").stringValue = "Death_DeathObject_Refuse");
            AddReaction(corpse, "PushBack", 0);
            Assert.IsTrue(target.CanInteract);

            target.Interact(new InteractContext(null, null));
            Assert.IsTrue(DeathRefusal.IsActive);
            yield return WaitUntilDone();

            CollectionAssert.AreEqual(new[]
            {
                "dialogue:Death_DeathObject_Refuse:locked", "PushBack:start:locked", "PushBack:end",
            }, calls);
            Assert.IsFalse(inputLock.IsLocked);
        }

    }
}
