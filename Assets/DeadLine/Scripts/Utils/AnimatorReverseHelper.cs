using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace DeadLine
{
    /// <summary>
    /// Helper estático para reproducir AnimationClips en reversa usando Playables.
    /// Devuelve el PlayableGraph creado; guárdalo y destrúyelo con StopAndDestroyGraph cuando quieras parar.
    /// </summary>
    public static class AnimatorReverseHelper
    {
        /// <summary>
        /// Reproduce el <paramref name="clip"/> en reversa en el <paramref name="animator"/>.
        /// <paramref name="normalizedStart"/> es 0..1 indicando la posición inicial dentro del clip (1 = final).
        /// Retorna el PlayableGraph que se está ejecutando (válido) — debes almacenarlo para detenerlo.
        /// </summary>
        public static PlayableGraph PlayClipInReverse(Animator animator, AnimationClip clip, float normalizedStart = 1f)
        {
            if (animator == null || clip == null)
                return default;

            var graph = PlayableGraph.Create($"ReverseAnim_{animator.gameObject.name}");
            var output = AnimationPlayableOutput.Create(graph, "AnimOutput", animator);

            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false);

            float clamped = Mathf.Clamp01(normalizedStart);
            double startTime = clamped * clip.length;

            // Aseguramos duración y ponemos el tiempo inicial
            playable.SetDuration(clip.length);
            playable.SetTime(startTime);

            // Velocidad negativa para reproducir en reversa
            playable.SetSpeed(-1.0);

            output.SetSourcePlayable(playable);
            graph.Play();

            return graph;
        }

        /// <summary>
        /// Detecta el clip actual en el <paramref name="animator"/> (por la capa <paramref name="layer"/>)
        /// y lo reproduce en reversa desde el frame actual.
        /// Devuelve el PlayableGraph creado (guárdalo y destrúyelo con <see cref="StopAndDestroyGraph"/>).
        /// </summary>
        public static PlayableGraph PlayCurrentAnimatorStateInReverse(Animator animator, int layer = 0)
        {
            if (animator == null) return default;

            var clipInfos = animator.GetCurrentAnimatorClipInfo(layer);
            if (clipInfos == null || clipInfos.Length == 0) return default;

            var clipInfo = clipInfos[0];
            var clip = clipInfo.clip;
            if (clip == null) return default;

            var state = animator.GetCurrentAnimatorStateInfo(layer);
            float normalized = Mathf.Repeat(state.normalizedTime, 1f);

            var graph = PlayableGraph.Create($"ReverseAnim_{animator.gameObject.name}");
            var output = AnimationPlayableOutput.Create(graph, "AnimOutput", animator);

            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false);

            double startTime = Mathf.Clamp01(normalized) * clip.length;
            playable.SetDuration(clip.length);
            playable.SetTime(startTime);
            playable.SetSpeed(-1.0);

            output.SetSourcePlayable(playable);
            graph.Play();

            return graph;
        }

        /// <summary>
        /// Detiene y destruye un PlayableGraph creado por PlayClipInReverse.
        /// </summary>
        public static void StopAndDestroyGraph(PlayableGraph graph)
        {
            if (!graph.IsValid()) return;
            graph.Stop();
            graph.Destroy();
        }

        /// <summary>
        /// Obtiene el tiempo normalizado actual (0..1) del playable en el graph.
        /// Retorna -1 si el graph no es válido.
        /// </summary>
        public static float GetNormalizedTime(PlayableGraph graph)
        {
            if (!graph.IsValid()) return -1f;
            
            var rootPlayable = graph.GetRootPlayable(0);
            if (!rootPlayable.IsValid()) return -1f;
            
            double time = rootPlayable.GetTime();
            double duration = rootPlayable.GetDuration();
            
            if (duration <= 0) return 0f;
            
            return Mathf.Clamp01((float)(time / duration));
        }
    }
}
