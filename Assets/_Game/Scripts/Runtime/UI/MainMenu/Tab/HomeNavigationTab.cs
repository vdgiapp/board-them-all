using System;
using DG.Tweening;
using UnityEngine;

namespace Game.UI.MainMenu
{
    public class HomeNavigationTab : NavigationTab
    {
        [Header("Settings")]
        [SerializeField] private float fadeTweenDuration = 0.1f;

        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;

        private Sequence _activateTween;
        
        private void Start()
        {
            SetVisible(false);
        }

        public override void OnActivate(bool activate)
        {
            _activateTween?.Kill();
            if (!activate)
            {
                SetVisible(false);
            }
            else
            {
                _activateTween = DOTween.Sequence();
                _activateTween.Insert(0f, canvasGroup.DOFade(1f, fadeTweenDuration));
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
    }
}