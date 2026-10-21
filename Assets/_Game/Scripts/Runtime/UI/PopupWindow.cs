using UnityEngine;

namespace Game.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PopupWindow : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup canvasGroup;

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            canvasGroup ??= GetComponent<CanvasGroup>();
        }
#endif

        public virtual void Open()
        {
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public virtual void Close()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }
    }
}