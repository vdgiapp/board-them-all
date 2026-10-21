using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class ActionDialog : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] protected float fadeInTime = 0.05f;
        [SerializeField] protected float fadeOutTime = 0.05f;
        
        [Header("References")]
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected TMP_Text title;
        [SerializeField] protected TMP_Text description;
        [SerializeField] protected TMP_Text positiveText;
        [SerializeField] protected TMP_Text negativeText;
        [SerializeField] protected Button positiveButton;
        [SerializeField] protected Button negativeButton;
        
        private readonly Queue<ActionRequest> _queue = new();
        private ActionRequest _currentRequest;
        private Tween _fadeTween;
        private bool _isDisplaying;
        
        private void Start()
        {
            positiveButton.onClick.AddListener(OnPositiveButtonClicked);
            negativeButton.onClick.AddListener(OnNegativeButtonClicked);
        }

        private void OnDisable()
        {
            _fadeTween?.Kill();
        }

        private void OnDestroy()
        {
            positiveButton.onClick.RemoveAllListeners();
            negativeButton.onClick.RemoveAllListeners();
            _queue.Clear();
        }

        public static void ShowAction(string titleString, string descString,
            bool showImmediately = false, bool swapPosition = false,
            string positiveTxt = "Yes", string negativeTxt = "No",
            Action positiveAction = null, Action negativeAction = null, Action onCancel = null)
        {
            UIManager.Instance.actionDialog.Show(titleString, descString, 
                showImmediately, swapPosition, 
                positiveTxt, negativeTxt, 
                positiveAction, negativeAction, onCancel
            );
        }

        private void Show(string titleString, string descString,
            bool showImmediately = false, bool swapPosition = false,
            string positiveTxt = "Yes", string negativeTxt = "No", 
            Action positiveAction = null, Action negativeAction = null, Action onCancel = null)
        {
            ActionRequest request = new ActionRequest(
                titleString, descString, swapPosition, 
                positiveTxt, negativeTxt, 
                positiveAction, negativeAction, onCancel
            );
            
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
        
        private void ShowInternal(ActionRequest request)
        {
            _currentRequest = request;
            _isDisplaying = true;

            gameObject.SetActive(true);

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            title.SetText(request.title);
            description.SetText(request.description);
            positiveText.SetText(request.positiveText);
            negativeText.SetText(request.negativeText);
            ApplyButtonPosition(request.swapPosition);

            _fadeTween?.Kill();
            _fadeTween = canvasGroup.DOFade(1f, fadeInTime).SetUpdate(true).SetLink(gameObject).SetAutoKill();
        }

        private void CloseCurrent(Action callback)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            _fadeTween?.Kill();
            _fadeTween = canvasGroup.DOFade(0f, fadeOutTime).SetUpdate(true).SetLink(gameObject).SetAutoKill();
            _fadeTween.OnComplete(() =>
            {
                if (!gameObject) return;
                gameObject.SetActive(false);

                _currentRequest.Clear();
                _isDisplaying = false;

                // Invoke the callback of the request
                // that was just closed.
                callback?.Invoke();

                // Then show next request.
                ShowNext();
            });
        }

        private void ShowNext()
        {
            if (_queue.Count == 0) return;
            ActionRequest next = _queue.Dequeue();
            ShowInternal(next);
        }

        private void ClearCurrent()
        {
            _fadeTween?.Kill();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            _currentRequest.Clear();
            _isDisplaying = false;
        }

        private void ApplyButtonPosition(bool swapPosition)
        {
            if (swapPosition)
            {
                positiveButton.transform.SetAsFirstSibling();
                negativeButton.transform.SetAsLastSibling();
            }
            else
            {
                positiveButton.transform.SetAsLastSibling();
                negativeButton.transform.SetAsFirstSibling();
            }
        }

        private void OnPositiveButtonClicked()
        {
            Action callback = _currentRequest.positiveAction;
            CloseCurrent(callback);
        }

        private void OnNegativeButtonClicked()
        {
            Action callback = _currentRequest.negativeAction;
            CloseCurrent(callback);
        }

        public void OnCancelButtonClicked()
        {
            Action callback = _currentRequest.cancelAction;
            CloseCurrent(callback);
        }
        
        private struct ActionRequest
        {
            public string title;
            public string description;
            public readonly bool swapPosition;

            public string positiveText;
            public string negativeText;

            public Action positiveAction;
            public Action negativeAction;
            public Action cancelAction;

            public ActionRequest(string title, string description, bool swapPosition,
                string positiveText, string negativeText,
                Action positiveAction, Action negativeAction, Action cancelAction)
            {
                this.title = title;
                this.description = description;
                this.swapPosition = swapPosition;

                this.positiveText = positiveText;
                this.negativeText = negativeText;

                this.positiveAction = positiveAction;
                this.negativeAction = negativeAction;
                this.cancelAction = cancelAction;
            }

            public void Clear()
            {
                title = null;
                description = null;
                positiveText = null;
                negativeText = null;

                positiveAction = null;
                negativeAction = null;
                cancelAction = null;
            }
        }
    }
}