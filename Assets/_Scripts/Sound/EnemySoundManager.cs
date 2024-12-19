using UnityEngine;

namespace _Scripts.Sound
{
    public class EnemySoundManager : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        
        [SerializeField] private AudioClip sniperShotClip;
        [SerializeField] private AudioClip sniperReloadedClip;

        [SerializeField] private AudioClip[] skreacherScreachClips;

        [SerializeField] private AudioClip patrollerFlashlightClip;
        
        #region Singleton

        public static EnemySoundManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType(typeof(EnemySoundManager)) as EnemySoundManager;

                return _instance;
            }
            set { _instance = value; }
        }

        private static EnemySoundManager _instance;

        #endregion

        #region Sniper Clips
        public void PlaySniperShotClip()
        {
            if (sniperShotClip is null) return;

            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(sniperShotClip);
        }
        
        public void PlaySniperReloadedClip()
        {
            if (sniperReloadedClip is null) return;

            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(sniperReloadedClip);
        }
        
        #endregion
        
        #region Skreacher Clips
        
        public void PlayerSkreacherClip()
        {
            if (skreacherScreachClips is null) return;

            foreach (var clip in skreacherScreachClips)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(clip);
            }
        }
        
        #endregion
        
        #region Patroller

        public void PlayPatrollerFlashlightClip()
        {
            if (patrollerFlashlightClip is null) return;

            audioSource.volume = 0.033f;
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(patrollerFlashlightClip);
            audioSource.volume = 0.2f;
        }
        
        #endregion
    }
}