using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace KenneyUIPack
{
    [RequireComponent(typeof(UIDocument))]
    [RequireComponent(typeof(AudioSource))]
    public class UISoundManager : MonoBehaviour
    {
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioClip hoverSound;
        private AudioSource source;

        void OnEnable()
        {
            source = GetComponent<AudioSource>();
            var root = GetComponent<UIDocument>().rootVisualElement;

            root.RegisterCallback<ClickEvent>(OnButtonClick, TrickleDown.TrickleDown);
            root.RegisterCallback<MouseEnterEvent>(OnButtonHover, TrickleDown.TrickleDown);
        }

        private void OnButtonClick(ClickEvent evt)
        {
            if (evt.target is Button button)
            {
                if (button.enabledSelf)
                    source.PlayOneShot(clickSound);
            }
            if (evt.target is Toggle toggle)
            {
                if (toggle.enabledSelf)
                    source.PlayOneShot(clickSound);
            }
        }

        private void OnButtonHover(MouseEnterEvent evt)
        {
            if (evt.target is Button button)
            {
                if (button.enabledSelf)
                    source.PlayOneShot(hoverSound);
            }
        }
    }
}