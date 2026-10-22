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
        [SerializeField] private float activatedWidth;
        [SerializeField] private float deactivatedWidth;
        [SerializeField] private float activatedBackgroundHeight;
        [SerializeField] private float deactivatedBackgroundHeight;
        [SerializeField] private float activatedMainPosY;
        [SerializeField] private float deactivatedMainPosY;

        [Header("References")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform main;
        [SerializeField] private RectTransform iconRectTransform;
        [SerializeField] private CanvasGroup titleCanvasGroup;

        private Sequence _activateTween;
        
        public void PlayActivateAnimation(bool activate)
        {
            _activateTween?.Kill();
            _activateTween = DOTween.Sequence();
            _activateTween.Join(
                rectTransform.DOSizeDelta(rectTransform.sizeDelta.WithX(activate ? activatedWidth : deactivatedWidth), tweenDuration)
                    .SetUpdate(true)
                    .SetLink(gameObject)
            );
            _activateTween.Join(
                background.DOSizeDelta(new Vector2(activate ? activatedWidth : deactivatedWidth, activate ? activatedBackgroundHeight : deactivatedBackgroundHeight), tweenDuration)
                    .SetUpdate(true)
                    .SetLink(gameObject)
            );
            _activateTween.Join(
                main.DOAnchorPosY(activate ? activatedMainPosY : deactivatedMainPosY, tweenDuration)
                    .SetUpdate(true)
                    .SetLink(gameObject)
            );
            _activateTween.Join(
                titleCanvasGroup.DOFade(activate ? 1f : 0f, tweenDuration)
                    .SetUpdate(true)
                    .SetLink(gameObject)
            );
            _activateTween.SetUpdate(true).SetLink(gameObject);
        }
    }
}
