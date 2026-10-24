using System;
using BrunoMikoski.AnimationSequencer;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public class CommonResourceBar : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private ECommonResource type;
        [SerializeField] private float tweenDuration = 1f;
        
        [Header("References")]
        [SerializeField] private RectTransform iconRectTransform;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text timerText;

        private Tweener numTween;
        private long displayValue;
        
        private void Start()
        {
            TigerForge.EventManager.StartListening(Constant.EVENT_ON_PLAYER_RESOURCE_UPDATED, OnResourceUpdate);
            displayValue = PlayerResourcePref.Instance.GetResource(type);
            OnResourceUpdate();
        }

        private void OnDestroy()
        {
            TigerForge.EventManager.StopListening(Constant.EVENT_ON_PLAYER_RESOURCE_UPDATED, OnResourceUpdate);
        }

        private void OnResourceUpdate()
        {
            switch (type)
            {
                case ECommonResource.Coin:
                {
                    long currentValue = PlayerResourcePref.Instance.GetResource(type);
                    numTween?.Kill();
                    numTween = DOTween.To(() => displayValue, x => displayValue = x, currentValue, tweenDuration);
                    numTween.OnUpdate(() => valueText.SetText(displayValue.GetShortValueText()));
                    numTween.SetEase(Ease.OutQuad);
                    break;
                }
                case ECommonResource.Energy:
                {
                    long currentValue = PlayerResourcePref.Instance.GetResource(type);
                    displayValue = currentValue;
                    valueText.SetText($"{displayValue.GetShortValueText()}/{Constant.MAX_ENERGY}");
                    if (timerText)
                    {
                        if (displayValue >= Constant.MAX_ENERGY) timerText.SetText("");
                        else
                        {
                            // TODO: Show energy recovery timer
                            timerText.SetText("TIMER");
                        }
                    }
                    break;
                }
            }
        }
        
        // TODO: Add pop icon anim and particle attractor
    }
}