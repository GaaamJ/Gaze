#nullable enable

using System;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace Gaze.Dialogue
{
    // 화자가 있는 대사마다 그 인물의 초상화를 띄움. 내레이션(화자 없음), 컷신 중, 대화가 끝난 뒤에는 숨김.
    // 표정은 대사 줄 끝 태그로 고름. Ex. `Girl: 아파... #face:pain`. 태그가 없으면 기본 표정(face 칸이 빈 항목).
    // `#face:none`이면 그 대사에서 숨김.
    // Yarn 러너의 프레젠터 목록에 들어가며, 러너가 대사마다 RunLineAsync를 부름. 대사 진행은 기다리지 않음.
    public class PortraitPresenter : DialoguePresenterBase
    {
        const string FaceTagPrefix = "face:";
        const string HiddenFace = "none";

        [Serializable]
        public struct Face
        {
            [Tooltip("Yarn 대사의 화자 이름. Ex. Girl")]
            public string character;
            [Tooltip("#face: 뒤에 쓰는 표정 이름. 비우면 태그가 없을 때 쓰는 기본 표정")]
            public string face;
            public Sprite sprite;
        }

        [SerializeField] Image? portrait;
        [SerializeField] CutsceneCommand? cutscene;
        [SerializeField] Face[] faces = Array.Empty<Face>();

        void Awake()
        {
            if (portrait == null)
                Debug.LogError("[PortraitPresenter] 'portrait' 참조가 비어 있다.", this);
            Hide();
        }

        void OnEnable()
        {
            if (cutscene != null)
                cutscene.Shown += Hide;
        }

        void OnDisable()
        {
            if (cutscene != null)
                cutscene.Shown -= Hide;
        }

        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            Show(line.CharacterName, FaceTag(line.Metadata));
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;

        public override YarnTask OnDialogueCompleteAsync()
        {
            Hide();
            return YarnTask.CompletedTask;
        }

        // 화자와 표정에 맞는 그림을 띄움. 화자가 없거나, 컷신 중이거나, none이거나, 그림이 없으면 숨김.
        void Show(string? character, string? face)
        {
            if (portrait == null)
                return;
            if (string.IsNullOrEmpty(character) || face == HiddenFace || (cutscene != null && cutscene.IsShowing))
            {
                Hide();
                return;
            }

            Sprite? sprite = Find(character!, face ?? "");
            if (sprite == null && !string.IsNullOrEmpty(face))
            {
                Debug.LogWarning($"[PortraitPresenter] '{character}'의 표정 '{face}'가 없어 기본 표정을 띄운다.", this);
                sprite = Find(character!, "");
            }
            if (sprite == null)
            {
                Hide();
                return;
            }

            portrait.sprite = sprite;
            portrait.gameObject.SetActive(true);
        }

        void Hide()
        {
            if (portrait != null)
                portrait.gameObject.SetActive(false);
        }

        Sprite? Find(string character, string face)
        {
            foreach (Face entry in faces)
            {
                if (entry.character == character && (entry.face ?? "") == face)
                    return entry.sprite;
            }
            return null;
        }

        // 대사 태그 중 face:로 시작하는 것의 값. 없으면 null.
        static string? FaceTag(string[] metadata)
        {
            foreach (string tag in metadata)
            {
                if (tag.StartsWith(FaceTagPrefix, StringComparison.Ordinal))
                    return tag.Substring(FaceTagPrefix.Length).Trim();
            }
            return null;
        }
    }
}
