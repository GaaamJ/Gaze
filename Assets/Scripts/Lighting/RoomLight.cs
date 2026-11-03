using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Gaze.Save;

namespace Gaze.Lighting
{
    // Room 씬의 Global Light 2D에 붙여, 따르는 등불(lamp)이 켜지면 방을 밝힘.
    // 씬에 적힌 밝기가 어두운 상태. 켜진 등불이면 들어올 때부터 litIntensity, 켜는 순간이면 fadeSeconds에 걸쳐 밝아짐.
    [RequireComponent(typeof(Light2D))]
    public class RoomLight : MonoBehaviour
    {
        [Tooltip("이 등불이 켜지면 밝아진다. 비우면 늘 어둡다.")]
        [SerializeField] Lamp lamp;
        [SerializeField] float litIntensity = 4f;
        [SerializeField] float fadeSeconds = 2f;

        // 지금 불러와 있는 방 빛들. LightUp이 등불을 따르는 것을 찾을 때 씀.
        static readonly List<RoomLight> loaded = new List<RoomLight>();

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
            if (lamp != null && LitLamps.Contains(lamp.Id))
                light2D.intensity = litIntensity;
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
                    room.FadeIn();
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
