using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.MainMenu
{
    public class NavigationButton : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float tweenDuration = 0.1f;
        [SerializeField] private float activatedBackgroundHeight;
        [SerializeField] private float deactivatedBackgroundHeight;
        [SerializeField] private float activatedMainPosY;
        [SerializeField] private float deactivatedMainPosY;
        
        [Header("References")]
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform main;
        [SerializeField] private RectTransform iconRectTransform;
        [SerializeField] private CanvasGroup titleCanvasGroup;

        private Sequence _activateTween;
        
        public void PlayActivated(bool activate)
        {
            if (activate)
            {
                Vector2 bgEnd = background.sizeDelta;
                bgEnd.y = activatedBackgroundHeight;
                _activateTween?.Kill();
                _activateTween = DOTween.Sequence();
                _activateTween.Join(background.DOSizeDelta(bgEnd, tweenDuration).SetEase(Ease.OutBack));
                _activateTween.Join(main.DOAnchorPosY(activatedMainPosY, tweenDuration).SetEase(Ease.OutBack));
                _activateTween.Join(titleCanvasGroup.DOFade(1f, tweenDuration).SetEase(Ease.Linear));
                _activateTween.SetUpdate(true).SetLink(gameObject);
            }
            else
            {
                Vector2 bgEnd = background.sizeDelta;
                bgEnd.y = deactivatedBackgroundHeight;
                _activateTween?.Kill();
                _activateTween = DOTween.Sequence();
                _activateTween.Join(background.DOSizeDelta(bgEnd, tweenDuration).SetEase(Ease.InBack));
                _activateTween.Join(main.DOAnchorPosY(deactivatedMainPosY, tweenDuration).SetEase(Ease.InBack));
                _activateTween.Join(titleCanvasGroup.DOFade(0f, tweenDuration).SetEase(Ease.Linear));
                _activateTween.SetUpdate(true).SetLink(gameObject);
            }
        }
    }
}
