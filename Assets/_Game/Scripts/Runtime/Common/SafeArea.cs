using UnityEngine;

namespace Game
{
    /// <summary>
    /// Safe area implementation for notched mobile devices. Usage:
    ///  (1) Add this component to the top level of any GUI panel. 
    ///  (2) If the panel uses a full screen background image, then create an immediate child and put the component on that instead, with all other elements childed below it.
    ///      This will allow the background image to stretch to the full extents of the screen behind the notch, which looks nicer.
    ///  (3) For other cases that use a mixture of full horizontal and vertical background stripes, use the Conform X & Y controls on separate elements as needed.
    /// </summary>
    public class SafeArea : MonoBehaviour
    {
#if UNITY_EDITOR
        #region Simulations
        /// <summary>
        /// Simulation device that uses safe area due to a physical notch or software home bar. For use in Editor only.
        /// </summary>
        public enum SimDevice
        {
            /// <summary>
            /// Don't use a simulated safe area - GUI will be full screen as normal.
            /// </summary>
            None,
            /// <summary>
            /// Simulate the iPhone X and Xs (identical safe areas).
            /// </summary>
            iPhoneX,
            /// <summary>
            /// Simulate the iPhone Xs Max and XR (identical safe areas).
            /// </summary>
            iPhoneXsMax,
            /// <summary>
            /// Simulate the Google Pixel 3 XL using landscape left.
            /// </summary>
            Pixel3XL_LSL,
            /// <summary>
            /// Simulate the Google Pixel 3 XL using landscape right.
            /// </summary>
            Pixel3XL_LSR
        }

        /// <summary>
        /// Simulation mode for use in editor only. This can be edited at runtime to toggle between different safe areas.
        /// </summary>
        public static SimDevice Sim = SimDevice.None;

        /// <summary>
        /// Normalised safe areas for iPhone X with Home indicator (ratios are identical to Xs, 11 Pro). Absolute values:
        ///  PortraitU x=0, y=102, w=1125, h=2202 on full extents w=1125, h=2436;
        ///  PortraitD x=0, y=102, w=1125, h=2202 on full extents w=1125, h=2436 (not supported, remains in Portrait Up);
        ///  LandscapeL x=132, y=63, w=2172, h=1062 on full extents w=2436, h=1125;
        ///  LandscapeR x=132, y=63, w=2172, h=1062 on full extents w=2436, h=1125.
        ///  Aspect Ratio: ~19.5:9.
        /// </summary>
        private readonly Rect[] NSA_iPhoneX = {
            new(0f, 102f / 2436f, 1f, 2202f / 2436f),  // Portrait
            new(132f / 2436f, 63f / 1125f, 2172f / 2436f, 1062f / 1125f)  // Landscape
        };

        /// <summary>
        /// Normalised safe areas for iPhone Xs Max with Home indicator (ratios are identical to XR, 11, 11 Pro Max). Absolute values:
        ///  PortraitU x=0, y=102, w=1242, h=2454 on full extents w=1242, h=2688;
        ///  PortraitD x=0, y=102, w=1242, h=2454 on full extents w=1242, h=2688 (not supported, remains in Portrait Up);
        ///  LandscapeL x=132, y=63, w=2424, h=1179 on full extents w=2688, h=1242;
        ///  LandscapeR x=132, y=63, w=2424, h=1179 on full extents w=2688, h=1242.
        ///  Aspect Ratio: ~19.5:9.
        /// </summary>
        private readonly Rect[] NSA_iPhoneXsMax = {
            new(0f, 102f / 2688f, 1f, 2454f / 2688f),  // Portrait
            new(132f / 2688f, 63f / 1242f, 2424f / 2688f, 1179f / 1242f)  // Landscape
        };

        /// <summary>
        /// Normalised safe areas for Pixel 3 XL using landscape left. Absolute values:
        ///  PortraitU x=0, y=0, w=1440, h=2789 on full extents w=1440, h=2960;
        ///  PortraitD x=0, y=0, w=1440, h=2789 on full extents w=1440, h=2960;
        ///  LandscapeL x=171, y=0, w=2789, h=1440 on full extents w=2960, h=1440;
        ///  LandscapeR x=0, y=0, w=2789, h=1440 on full extents w=2960, h=1440.
        ///  Aspect Ratio: 18.5:9.
        /// </summary>
        private readonly Rect[] NSA_Pixel3XL_LSL = {
            new(0f, 0f, 1f, 2789f / 2960f),  // Portrait
            new(0f, 0f, 2789f / 2960f, 1f)  // Landscape
        };

        /// <summary>
        /// Normalised safe areas for Pixel 3 XL using landscape right. Absolute values and aspect ratio same as above.
        /// </summary>
        private readonly Rect[] NSA_Pixel3XL_LSR = {
            new(0f, 0f, 1f, 2789f / 2960f),  // Portrait
            new(171f / 2960f, 0f, 2789f / 2960f, 1f)  // Landscape
        };
        #endregion
#endif

        [SerializeField] private RectTransform panel;
        // [SerializeField] private bool ConformX = true;  // Conform to screen safe area on X-axis (default true, disable to ignore)
        // [SerializeField] private bool ConformY = true;  // Conform to screen safe area on Y-axis (default true, disable to ignore)
        [SerializeField] private bool conformLeft  = true;
        [SerializeField] private bool conformRight = true;
        [SerializeField] private bool conformTop = true;
        [SerializeField] private bool conformBottom = true;
        public bool logging = true;
        
        private Rect _lastSafeArea = new(0, 0, 0, 0);
        private Vector2Int _lastScreenSize = new(0, 0);
        private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            panel ??= GetComponent<RectTransform>();
        }
#endif
        
        private void Start()
        {
            if (panel == null)
            {
                Debug.LogError("Cannot apply safe area - no RectTransform found on " + name);
                Destroy(gameObject);
            }
            Update();
        }

        private void Update()
        {
            // Refresh
            Rect safeArea = GetSafeArea();

            if (safeArea != _lastSafeArea
                || Screen.width != _lastScreenSize.x
                || Screen.height != _lastScreenSize.y
                || Screen.orientation != _lastOrientation)
            {
                // Fix for having auto-rotate off and manually forcing a screen orientation.
                // See https://forum.unity.com/threads/569236/#post-4473253 and https://forum.unity.com/threads/569236/page-2#post-5166467
                _lastScreenSize.x = Screen.width;
                _lastScreenSize.y = Screen.height;
                _lastOrientation = Screen.orientation;

                ApplySafeArea(safeArea);
            }
        }

        private Rect GetSafeArea()
        {
            Rect safeArea = Screen.safeArea;
#if UNITY_EDITOR
            if (Application.isEditor && Sim != SimDevice.None)
            {
                Rect nsa = new Rect (0, 0, Screen.width, Screen.height);
                switch (Sim)
                {
                    case SimDevice.iPhoneX:
                    {
                        if (Screen.height > Screen.width) // Portrait
                            nsa = NSA_iPhoneX[0];
                        else // Landscape
                            nsa = NSA_iPhoneX[1];
                        break;
                    }
                    case SimDevice.iPhoneXsMax:
                    {
                        if (Screen.height > Screen.width) // Portrait
                            nsa = NSA_iPhoneXsMax[0];
                        else // Landscape
                            nsa = NSA_iPhoneXsMax[1];
                        break;
                    }
                    case SimDevice.Pixel3XL_LSL:
                    {
                        if (Screen.height > Screen.width) // Portrait
                            nsa = NSA_Pixel3XL_LSL[0];
                        else // Landscape
                            nsa = NSA_Pixel3XL_LSL[1];
                        break;
                    }
                    case SimDevice.Pixel3XL_LSR:
                    {
                        if (Screen.height > Screen.width) // Portrait
                            nsa = NSA_Pixel3XL_LSR[0];
                        else // Landscape
                            nsa = NSA_Pixel3XL_LSR[1];
                        break;
                    }
                    default:
                        break;
                }
                safeArea = new Rect(Screen.width * nsa.x, Screen.height * nsa.y, Screen.width * nsa.width, Screen.height * nsa.height);
            }
#endif
            return safeArea;
        }

        // private void ApplySafeArea(Rect r)
        // {
        //     LastSafeArea = r;
        //
        //     // Ignore x-axis?
        //     if (!ConformX)
        //     {
        //         r.x = 0;
        //         r.width = Screen.width;
        //     }
        //
        //     // Ignore y-axis?
        //     if (!ConformY)
        //     {
        //         r.y = 0;
        //         r.height = Screen.height;
        //     }
        //
        //     // Check for invalid screen startup state on some Samsung devices (see below)
        //     if (Screen.width > 0 && Screen.height > 0)
        //     {
        //         // Convert safe area rectangle from absolute pixels to normalised anchor coordinates
        //         Vector2 anchorMin = r.position;
        //         Vector2 anchorMax = r.position + r.size;
        //         anchorMin.x /= Screen.width;
        //         anchorMin.y /= Screen.height;
        //         anchorMax.x /= Screen.width;
        //         anchorMax.y /= Screen.height;
        //
        //         // Fix for some Samsung devices (e.g. Note 10+, A71, S20) where Refresh gets called twice and the first time returns NaN anchor coordinates
        //         // See https://forum.unity.com/threads/569236/page-2#post-6199352
        //         if (anchorMin is { x: >= 0, y: >= 0 } && anchorMax is { x: >= 0, y: >= 0 })
        //         {
        //             Panel.anchorMin = anchorMin;
        //             Panel.anchorMax = anchorMax;
        //         }
        //     }
        //
        //     if (Logging)
        //     {
        //         OCDebug.LogFormat("New safe area applied to {0}: x={1}, y={2}, w={3}, h={4} on full extents w={5}, h={6}",
        //         name, r.x, r.y, r.width, r.height, Screen.width, Screen.height);
        //     }
        // }
        
        // private void ApplySafeArea(Rect r)
        // {
        //     lastSafeArea = r;
        //
        //     // ConformLeft - keep r.x (offset from left)
        //     // ConformRight - keep r.width (offset from right = Screen.width - r.x - r.width)
        //     // ConformBottom - keep r.y (offset from bottom)
        //     // ConformTop - keep r.height (offset from bottom = Screen.height - r.y - r.height)
        //
        //     float x = conformLeft ? r.x : 0f;
        //     float width = conformRight ? r.width : Screen.width - x;
        //     float y = conformBottom ? r.y : 0f;
        //     float height = conformTop ? r.height : Screen.height - y;
        //
        //     if (Screen.width > 0 && Screen.height > 0)
        //     {
        //         Vector2 anchorMin = new Vector2(x / Screen.width, y / Screen.height);
        //         Vector2 anchorMax = new Vector2((x + width) / Screen.width, (y + height) / Screen.height);
        //
        //         if (anchorMin is { x: >= 0, y: >= 0 } && anchorMax is { x: >= 0, y: >= 0 })
        //         {
        //             panel.anchorMin = anchorMin;
        //             panel.anchorMax = anchorMax;
        //         }
        //     }
        //
        //     if (logging)
        //     {
        //         OCDebug.LogFormat(
        //             "New safe area applied to {0}: x={1}, y={2}, w={3}, h={4} on full extents w={5}, h={6}",
        //             name, x, y, width, height, Screen.width, Screen.height
        //         );
        //     }
        // }
        
        private void ApplySafeArea(Rect r)
        {
            _lastSafeArea = r;

            float leftInset = conformLeft ? r.xMin : 0f;
            float rightInset = conformRight ? Screen.width - r.xMax : 0f;
            float bottomInset = conformBottom ? r.yMin : 0f;
            float topInset = conformTop ? Screen.height - r.yMax : 0f;

            float xMin = leftInset;
            float xMax = Screen.width - rightInset;
            float yMin = bottomInset;
            float yMax = Screen.height - topInset;

            if (Screen.width > 0 && Screen.height > 0)
            {
                Vector2 anchorMin = new Vector2(xMin / Screen.width, yMin / Screen.height);
                Vector2 anchorMax = new Vector2(xMax / Screen.width, yMax / Screen.height);

                if (anchorMin is { x: >= 0f, y: >= 0f } && anchorMax is { x: >= 0f, y: >= 0f })
                {
                    panel.anchorMin = anchorMin;
                    panel.anchorMax = anchorMax;
                }
            }

            if (logging)
            {
                Debug.LogFormat(
                    "New safe area applied to {0}: left={1}, right={2}, bottom={3}, top={4} on full extents w={5}, h={6}",
                    name, leftInset, rightInset, bottomInset, topInset, Screen.width, Screen.height
                );
            }
        }
    }
}
