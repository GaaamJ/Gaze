using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Gaze.Save;

namespace Gaze.Lighting
{
    // Room 씬의 Global Light 2D에 붙여, 따르는 등불(lamp)이 켜지면 방을 밝힘.
    // 씬에 적힌 밝기가 어두운 상태. 켜진 등불이면 들어올 때부터 litIntensity, 켜는 순간이면 fadeSeconds에 걸쳐 밝아짐.
    [RequireComponent(typeof(Light2D))]
    public class RoomLight : MonoBehaviour
    {
        [Tooltip("해당 lamp가 켜지면 밝아짐. empty = 항상 어두움")]
        [SerializeField] Lamp lamp;
        [SerializeField] float litIntensity = 4f;
        [SerializeField] float fadeSeconds = 2f;

        // 지금 불러와 있는 방 빛들. LightUp이 등불을 따르는 것을 찾을 때 씀.
        static readonly List<RoomLight> loaded = new List<RoomLight>();

        // 방이 밝아지기 시작할 때 그 방의 Scene을 알림. 이미 밝은 채 들어온 방에는 오지 않음.
        // 방 안에서 밝기에 따라 바뀌는 부품(LightVariant)이 들음.
        public static event Action<Scene> SceneLit;

        Light2D light2D;
        Coroutine fade;

        void Awake()
        {
            light2D = GetComponent<Light2D>();
        }

        // Room 씬이 불러와질 때 켜짐. 죽어서 다시 시작해도 씬을 새로 불러오므로 다시 확인함.
        void OnEnable()
        {
            loaded.Add(this);
            if (IsLit)
                light2D.intensity = litIntensity;
        }

        // 따르는 등불이 켜져 있으면 참. 밝아지는 중에도 참.
        public bool IsLit => lamp != null && LitLamps.Contains(lamp.Id);

        // scene에 불러와 있는 방 빛 중 하나라도 밝으면 참. 방 빛이 없으면 거짓.
        // Room 씬 안의 부품이 자기 방이 밝은지 물을 때 씀.
        // 방 빛은 OnEnable에서 등록되므로, 같은 씬의 OnEnable·Awake에서 부르면 아직 없을 수 있음. Start 이후에 부를 것.
        public static bool IsSceneLit(Scene scene)
        {
            foreach (RoomLight room in loaded)
            {
                if (room.gameObject.scene == scene && room.IsLit)
                    return true;
            }
            return false;
        }

        void OnDisable()
        {
            loaded.Remove(this);
            fade = null;
        }

        // 이 등불을 따르는 방 빛을 모두 서서히 밝힘. LampResponse가 부름.
        // 지금 불러와 있지 않은 방은 나중에 들어올 때 OnEnable에서 세이브를 보고 밝아짐.
        public static void LightUp(Lamp lamp)
        {
            foreach (RoomLight room in loaded)
            {
                if (room.lamp == lamp)
                {
                    room.FadeIn();
                    SceneLit?.Invoke(room.gameObject.scene);
                }
            }
        }

        void FadeIn()
        {
            if (fade != null)
                StopCoroutine(fade);
            fade = StartCoroutine(Fade());
        }

        IEnumerator Fade()
        {
            float from = light2D.intensity;
            for (float t = 0f; t < fadeSeconds; t += Time.deltaTime)
            {
                light2D.intensity = Mathf.Lerp(from, litIntensity, t / fadeSeconds);
                yield return null;
            }
            light2D.intensity = litIntensity;
            fade = null;
        }
    }
}
