using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace DeadLine
{
    // Only the standalone sample uses this scene host. Puzzle rules and player input
    // are still handled by the level's original components.
    [DefaultExecutionOrder(-50)]
    public sealed class OriginalDemoFlow : MonoBehaviour
    {
        public CanvasGroup fade;
        public FullScreenPassRendererFeature fogFeature;
        public Material fogMaterial;
        public string soundtrack;
        public static OriginalDemoFlow Instance { get; private set; }
        static int entry = -1;
        Coroutine fadeRoutine;
        bool respawning;
        bool loading;
        float unlockAt;
        static readonly Vector3[] entrances = {
            new Vector3(-16.10659f,8.05f,4.85407f),
            new Vector3(31.4134f,8.1897f,.1300f),
            new Vector3(117.8f,8.93f,-1.05f)
        };
        void Awake()
        {
            Instance=this;
            if (fade) fade.alpha=0f;
            if (fogFeature && fogMaterial) fogFeature.passMaterial=fogMaterial;
        }
        IEnumerator Start()
        {
            // All original checkpoint managers and the character have run Start.
            yield return null;
            if(!string.IsNullOrEmpty(soundtrack) && AudioManager.Instance)
                AudioManager.Instance.PlayMusic(soundtrack);
            if (entry>=0 && SceneManager.GetActiveScene().name=="WaterLight")
            {
                var player=FindFirstObjectByType<Player>();
                var pos=entrances[entry];
                if (player)
                {
                    var body=player.GetComponent<Rigidbody>();
                    if (body){body.position=pos;body.linearVelocity=Vector3.zero;}
                    player.transform.position=pos;
                    Physics.SyncTransforms();
                    OriginalRoomCameras.Restore(entry,player.transform);
                }
                var cp=CheckpointsManager.Instance;
                if (cp)
                {
                    var nearest=cp.GetAllCheckpoints().Where(c=>c).OrderBy(c=>(c.GetRespawnPosition()-pos).sqrMagnitude).FirstOrDefault();
                    if (nearest) cp.ReachCheckpoint(nearest);
                }
            }
            entry=-1;
            Debug.Log("DeadLine original sample ready: "+SceneManager.GetActiveScene().name);
        }
        void Update()
        {
            if (unlockAt>0 && Time.time>=unlockAt)
            {
                unlockAt=0;
                InputManager.Instance?.EnablePlayerInputActions();
            }
            var keys=Keyboard.current;
            if (keys==null || loading) return;
            if (keys.f1Key.wasPressedThisFrame) Load(-1,"Memory");
            else if (keys.f2Key.wasPressedThisFrame) Load(0,"WaterLight");
            else if (keys.f3Key.wasPressedThisFrame) Load(1,"WaterLight");
            else if (keys.f4Key.wasPressedThisFrame) Load(2,"WaterLight");
            else if(keys.escapeKey.wasPressedThisFrame) Application.Quit();
        }
        void Load(int entrance,string scene)
        {
            loading=true;
            entry=entrance;
            if(InputManager.Instance) InputManager.Instance.EnablePlayerInputActions();
            SceneManager.LoadScene(scene);
        }
        void OnDestroy(){if(Instance==this) Instance=null;}
        public void BlockPlayerInput(float seconds)
        {
            unlockAt=Mathf.Max(unlockAt,Time.time+seconds);
            InputManager.Instance?.DisablePlayerInputActions();
        }
        public void OnPlayerDeath(float fadeOutDuration=.5f,float fadeInDuration=.5f)
        {
            if (!respawning) StartCoroutine(Respawn(fadeOutDuration,fadeInDuration));
        }
        IEnumerator Respawn(float away,float back)
        {
            respawning=true;
            BlockPlayerInput(away+back);
            FadeOut(away);
            yield return new WaitForSeconds(away);
            CheckpointsManager.Instance.RespawnPlayer();
            FadeIn(back);
            yield return new WaitForSeconds(back);
            respawning=false;
        }
        public void FadeOut(float duration) => SetFade(1f,duration);
        public void FadeIn(float duration) => SetFade(0f,duration);
        void SetFade(float alpha,float seconds)
        {
            if(!fade) return;
            if(fadeRoutine!=null) StopCoroutine(fadeRoutine);
            fadeRoutine=StartCoroutine(Blend(alpha,seconds));
        }
        IEnumerator Blend(float target,float seconds)
        {
            float from=fade.alpha;
            for(float t=0;t<seconds;t+=Time.unscaledDeltaTime)
            {
                fade.alpha=Mathf.Lerp(from,target,t/seconds);
                yield return null;
            }
            fade.alpha=target;
            fadeRoutine=null;
        }
    }
}
