using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Game.Manager;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public class TextDialog : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] protected float fadeInTime = 0.05f;
        [SerializeField] protected float fadeOutTime = 1f;
        
        [Header("References")]
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected TMP_Text title;
        
        private readonly Queue<TextRequest> _queue = new();
        private Coroutine _displayCoroutine;
        private Tween _fadeTween;
        private bool _isDisplaying;
        
        private void OnDisable()
        {
            CancelDisplayTimer();
            _fadeTween?.Kill();
        }

        private void OnDestroy()
        {
            _queue.Clear();
        }
        
        public static void ShowText(string text, float displayTime = 3f, bool showImmediately = false)
        {
            UIManager.Instance.textDialog.Show(text, displayTime, showImmediately);
        }
        
        private void Show(string text, float displayTime = 3f, bool showImmediately = false)
        {
            TextRequest request = new TextRequest(text, Mathf.Max(0f, displayTime));
            
            if (showImmediately)
            {
                ClearCurrent();
                ShowInternal(request);
                return;
            }
            
            if (!_isDisplaying)
            {
                ShowInternal(request);
                return;
            }
            
            _queue.Enqueue(request);
        }
        
        private void ShowInternal(TextRequest request)
        {
            _isDisplaying = true;
            gameObject.SetActive(true);

            CancelDisplayTimer();

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            title.SetText(request.text);

            _fadeTween?.Kill();
            _fadeTween = canvasGroup.DOFade(1f, fadeInTime).SetUpdate(true).SetLink(gameObject).SetAutoKill();
            _fadeTween.OnComplete(() =>
            {
                if (!gameObject.activeInHierarchy) return;
                StartDisplayTimer(request.displayTime);
            });
        }
        
        private void StartDisplayTimer(float displayTime)
        {
            CancelDisplayTimer();
            if (displayTime <= 0f)
            {
                CloseCurrent();
                return;
            }
            _displayCoroutine = StartCoroutine(IEDisplay(displayTime));
        }

        private IEnumerator IEDisplay(float displayTime)
        {
            yield return CoroutineUtils.WaitRealtime(displayTime);
            CloseCurrent();
        }
        
        private void CloseCurrent()
        {
            CancelDisplayTimer();

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            _fadeTween?.Kill();
            _fadeTween = canvasGroup.DOFade(0f, fadeOutTime).SetUpdate(true).SetLink(gameObject).SetAutoKill();
            _fadeTween.OnComplete(() =>
            {
                if (!gameObject) return;
                gameObject.SetActive(false);
                _isDisplaying = false;
                ShowNext();
            });
        }

        private void ShowNext()
        {
            if (_queue.Count == 0) return;
            TextRequest next = _queue.Dequeue();
            ShowInternal(next);
        }

        private void ClearCurrent()
        {
            CancelDisplayTimer();
            _fadeTween?.Kill();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            _isDisplaying = false;
        }
        
        private void CancelDisplayTimer()
        {
            if (_displayCoroutine != null) StopCoroutine(_displayCoroutine);
            _displayCoroutine = null;
        }
        
        private struct TextRequest
        {
            public readonly string text;
            public readonly float displayTime;
            
            public TextRequest(string text, float displayTime)
            {
                this.text = text;
                this.displayTime = displayTime;
            }
        }
    }
}