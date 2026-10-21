using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class LoadingCanvas : MonoBehaviourSingleton<LoadingCanvas>
    {
        [Header("Settings")]
        [SerializeField] private float timeBeforePlayEnd = 0.5f;
        //[SerializeField] private float tweenTime = 0.5f;
        
        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image fillImage;
        
        //private Tween _startTween;
        //private Tween _endTween;

        protected override void Awake()
        {
            base.Awake();
            SetLoadingActive(false);
            gameObject.SetActive(false);
        }

        public IEnumerator IEPlayStartLoading()
        {
            gameObject.SetActive(true);
            SetLoadingActive(true);
            // _startTween?.Kill();
            // _startTween = canvasGroup.DOFade(1f, tweenTime).SetUpdate(true).SetLink(gameObject).SetAutoKill();
            // yield return _startTween.WaitForCompletion();
            yield return null;
        }

        public IEnumerator IEPlayEndLoading()
        {
            yield return CoroutineUtils.WaitRealtime(timeBeforePlayEnd);
            // _endTween?.Kill();
            // _endTween = canvasGroup.DOFade(0f, tweenTime).SetUpdate(true).SetLink(gameObject).SetAutoKill();
            // yield return _endTween.WaitForCompletion();
            yield return null;
            SetLoadingActive(false);
            gameObject.SetActive(false);
        }

        public void SetFillAmount(float fillAmount)
        {
            fillImage.fillAmount = fillAmount;
        }
        
        public void SetLoadingActive(bool isActive)
        {
            canvasGroup.alpha = isActive ? 1 : 0;
            SetLoadingBlock(isActive);
        }

        public void SetLoadingBlock(bool isActive)
        {
            canvasGroup.blocksRaycasts = isActive;
            canvasGroup.interactable = isActive;
        }
    }
}