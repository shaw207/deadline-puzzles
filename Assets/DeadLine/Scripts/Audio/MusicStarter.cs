using UnityEngine;

namespace DeadLine
{
    public class SceneMusicStarter : MonoBehaviour
    {
        [SerializeField] private string musicName = "level_music";
        [SerializeField] private bool fadeIn = true;
        [SerializeField] private float fadeDuration = 2f;

        private void Start()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(musicName, fadeIn, fadeDuration);
            }
        }
    }
}