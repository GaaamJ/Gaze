using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectT.Core.GameFolder;
using ProjectT.Puzzle;
using ProjectT.Save;
using ProjectT.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjectT.Death.Tests
{
    // 에디트 모드에서는 Awake가 불리지 않아 Apply를 직접 부른다.
    public class CorpseOnDeathTests
    {
        const string NodeId = "Death_DeathObject";

        string tempBase;
        Sprite aliveSprite;
        Sprite corpseSprite;
        Texture2D texture;

        [SetUp]
        public void SetUp()
        {
            tempBase = Path.Combine(Path.GetTempPath(), "ProjectT.Tests." + Guid.NewGuid().ToString("N"));
            SaveManager.Paths = new GameFolderPaths(Path.Combine(tempBase, GameFolderPaths.RootFolderName));
            SaveManager.Load();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            texture = new Texture2D(2, 2);
            aliveSprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
            corpseSprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(aliveSprite);
            UnityEngine.Object.DestroyImmediate(corpseSprite);
            UnityEngine.Object.DestroyImmediate(texture);
            SaveManager.Paths = null;
            SaveManager.Load();
            if (Directory.Exists(tempBase))
                Directory.Delete(tempBase, true);
        }

        static void Set(UnityEngine.Object target, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        CorpseOnDeath NewCorpse(MonoBehaviour source, out SpriteRenderer renderer, bool withSprite = true)
        {
            var go = source != null ? source.gameObject : new GameObject("Corpse");
            renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = aliveSprite;
            var corpse = go.AddComponent<CorpseOnDeath>();
            SpriteRenderer captured = renderer;
            Sprite sprite = withSprite ? corpseSprite : null;
            Set(corpse, so =>
            {
                so.FindProperty("source").objectReferenceValue = source;
                so.FindProperty("target").objectReferenceValue = captured;
                so.FindProperty("corpseSprite").objectReferenceValue = sprite;
            });
            return corpse;
        }

        static DeathInteractable NewDeathInteractable(string nodeId)
        {
            var source = new GameObject("DeathObject").AddComponent<DeathInteractable>();
            Set(source, so => so.FindProperty("deathNodeId").stringValue = nodeId);
            return source;
        }

        [Test]
        public void WithRecord_SwapsToCorpseSprite()
        {
            SaveManager.Data.diedNodeIds.Add(NodeId);
            var corpse = NewCorpse(NewDeathInteractable(NodeId), out SpriteRenderer renderer);

            corpse.Apply();

            Assert.AreSame(corpseSprite, renderer.sprite);
        }

        [Test]
        public void WithoutRecord_KeepsSprite()
        {
            SaveManager.Data.diedNodeIds.Add("Death_FallingDeath");
            var corpse = NewCorpse(NewDeathInteractable(NodeId), out SpriteRenderer renderer);

            corpse.Apply();

            Assert.AreSame(aliveSprite, renderer.sprite);
        }

        [Test]
        public void DeathZoneAndPasswordPuzzle_ExposeTheirNodeIds()
        {
            var zoneGo = new GameObject("Zone");
            zoneGo.AddComponent<BoxCollider2D>().isTrigger = true;
            var zone = zoneGo.AddComponent<DeathZone>();
            Set(zone, so => so.FindProperty("deathNodeId").stringValue = "Death_FallingDeath");
            var puzzle = new GameObject("Puzzle").AddComponent<PasswordPuzzle>();
            Set(puzzle, so => so.FindProperty("wrongDeathNodeId").stringValue = "Death_PasswordWrong");

            Assert.AreEqual("Death_FallingDeath", ((IDeathSource)zone).DeathNodeId);
            Assert.AreEqual("Death_PasswordWrong", ((IDeathSource)puzzle).DeathNodeId);

            SaveManager.Data.diedNodeIds.Add("Death_PasswordWrong");
            var zoneCorpse = NewCorpse(zone, out SpriteRenderer zoneRenderer);
            var puzzleCorpse = NewCorpse(puzzle, out SpriteRenderer puzzleRenderer);
            zoneCorpse.Apply();
            puzzleCorpse.Apply();

            Assert.AreSame(aliveSprite, zoneRenderer.sprite);
            Assert.AreSame(corpseSprite, puzzleRenderer.sprite);
        }

        [Test]
        public void SourceEmpty_WarnsAndKeepsSprite()
        {
            SaveManager.Data.diedNodeIds.Add(NodeId);
            var corpse = NewCorpse(null, out SpriteRenderer renderer);

            LogAssert.Expect(LogType.Warning, new Regex(@"\[CorpseOnDeath\].*source가 비었거나"));
            Assert.DoesNotThrow(() => corpse.Apply());

            Assert.AreSame(aliveSprite, renderer.sprite);
        }

        [Test]
        public void SourceNotDeathSource_WarnsAndKeepsSprite()
        {
            SaveManager.Data.diedNodeIds.Add(NodeId);
            var other = new GameObject("Other").AddComponent<InputLock>();
            var corpse = NewCorpse(other, out SpriteRenderer renderer);

            LogAssert.Expect(LogType.Warning, new Regex(@"\[CorpseOnDeath\].*source가 비었거나"));
            corpse.Apply();

            Assert.AreSame(aliveSprite, renderer.sprite);
        }

        [Test]
        public void CorpseSpriteEmpty_WarnsAndKeepsSprite()
        {
            SaveManager.Data.diedNodeIds.Add(NodeId);
            var corpse = NewCorpse(NewDeathInteractable(NodeId), out SpriteRenderer renderer, withSprite: false);

            LogAssert.Expect(LogType.Warning, new Regex(@"\[CorpseOnDeath\].*target 또는 corpseSprite가 비어"));
            corpse.Apply();

            Assert.AreSame(aliveSprite, renderer.sprite);
        }
    }
}
