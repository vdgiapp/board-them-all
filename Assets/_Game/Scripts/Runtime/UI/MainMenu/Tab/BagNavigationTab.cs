using DG.Tweening;
using UnityEngine;

namespace Game.UI.MainMenu
{
    public class BagNavigationTab : NavigationTab
    {
        [Header("Settings")]
        [SerializeField] private float fadeTweenDuration = 0.1f;
        [SerializeField] private float decorationTopTweenDuration = 0.5f;
        [SerializeField] private float decorationTopEndHeight;
        
        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform decorationTopRect;

        private Sequence _activateTween;
        
        private void Start()
        {
            SetVisible(false);
            ResetTopDecoHeightToStart();
        }

        public override void OnActivate(bool activate)
        {
            _activateTween?.Kill();
            if (!activate)
            {
                SetVisible(false);
                ResetTopDecoHeightToStart();
            }
            else
            {
                _activateTween = DOTween.Sequence();
                _activateTween.Insert(0f, canvasGroup.DOFade(1f, fadeTweenDuration));
                _activateTween.Insert(0f, decorationTopRect.DOSizeDelta(decorationTopRect.sizeDelta.WithY(decorationTopEndHeight), decorationTopTweenDuration).SetEase(Ease.OutBounce));
                _activateTween.SetUpdate(true).SetLink(gameObject);
                SetInteractable(true);
            }
        }
        
        private void SetVisible(bool visible)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            SetInteractable(visible);
        }

        private void SetInteractable(bool interactable)
        {
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }

        private void ResetTopDecoHeightToStart()
        {
            decorationTopRect.sizeDelta = decorationTopRect.sizeDelta.WithY(0f);
        }
    }
}