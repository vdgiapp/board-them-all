using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [RequireComponent(typeof(RawImage))]
    public class UVScroll : MonoBehaviour
    {
        public float scrollSpeedX = 0.5f;
        public float scrollSpeedY = 0.5f;

        [SerializeField] private RawImage _rawImage;
        private Rect _currentUvRect;

        private void Start()
        {
            _rawImage ??= GetComponent<RawImage>();
            _currentUvRect = _rawImage.uvRect;
        }

        private void Update()
        {
            _currentUvRect.x += scrollSpeedX * Time.deltaTime;
            _currentUvRect.y += scrollSpeedY * Time.deltaTime;
            _rawImage.uvRect = _currentUvRect;
        }
    }
}