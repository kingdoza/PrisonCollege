using UnityEngine;

[DisallowMultipleComponent]
public class FuseBoxMarkerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform _markerRoot;
    [SerializeField] private RectTransform _rotationRoot;
    [SerializeField] private RectTransform _boundsRect;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private Transform _target;
    [SerializeField] private GameObject _directionArrowObject;

    [Header("Positioning")]
    [SerializeField] private Vector3 _targetOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private float _edgePadding = 48f;
    [SerializeField] private float _rotationOffsetDegrees;

    private const float DirectionEpsilon = 0.0001f;

    private CanvasGroup _markerGroup;
    private LabLightSystem _lightSystem;
    private Transform _fallbackTarget;
    private bool _isTracking;



    private void Reset()
    {
        _markerRoot = GetComponent<RectTransform>();
        _rotationRoot = _markerRoot;
        _canvas = GetComponentInParent<Canvas>();
        _boundsRect = _markerRoot != null ? _markerRoot.parent as RectTransform : null;
    }



    private void Awake()
    {
        ResolveStaticReferences();
        EnsureMarkerGroup();
        HideMarker();
    }



    private void OnEnable()
    {
        ResolveStaticReferences();
        SubscribeLightSystem();
        RefreshVisibility();
    }



    private void Start()
    {
        ResolveStaticReferences();
        SubscribeLightSystem();
        RefreshVisibility();
    }



    private void OnDisable()
    {
        UnsubscribeLightSystem();
    }



    private void Update()
    {
        if (!_isTracking) return;

        if (!HasRequiredReferences())
        {
            SetMarkerVisible(false);
            SetDirectionArrowVisible(false);
            return;
        }

        SetMarkerVisible(true);
        UpdateMarkerPosition();
    }



    private void ResolveStaticReferences()
    {
        if (_markerRoot == null)
            _markerRoot = GetComponent<RectTransform>();

        if (_rotationRoot == null)
            _rotationRoot = _markerRoot;

        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();

        if (_boundsRect == null && _markerRoot != null)
            _boundsRect = _markerRoot.parent as RectTransform;

        if (_boundsRect == null && _canvas != null)
            _boundsRect = _canvas.GetComponent<RectTransform>();
    }



    private void EnsureMarkerGroup()
    {
        if (_markerRoot == null) return;

        _markerGroup = _markerRoot.GetComponent<CanvasGroup>();
        if (_markerGroup == null)
            _markerGroup = _markerRoot.gameObject.AddComponent<CanvasGroup>();
    }



    private void SubscribeLightSystem()
    {
        LabLightSystem lightSystem = Object.FindFirstObjectByType<LabLightSystem>();
        if (lightSystem == _lightSystem) return;

        UnsubscribeLightSystem();

        _lightSystem = lightSystem;
        if (_lightSystem == null) return;

        _lightSystem.LightsOffEvent.AddListener(ShowMarker);
        _lightSystem.LightsOnEvent.AddListener(HideMarker);
    }



    private void UnsubscribeLightSystem()
    {
        if (_lightSystem == null) return;

        _lightSystem.LightsOffEvent.RemoveListener(ShowMarker);
        _lightSystem.LightsOnEvent.RemoveListener(HideMarker);
        _lightSystem = null;
    }



    private void RefreshVisibility()
    {
        if (_lightSystem != null && !_lightSystem.IsLightsOn)
            ShowMarker();
        else
            HideMarker();
    }



    private void ShowMarker()
    {
        _isTracking = true;
    }



    private void HideMarker()
    {
        _isTracking = false;
        SetMarkerVisible(false);
        SetDirectionArrowVisible(false);
    }



    private void SetMarkerVisible(bool visible)
    {
        if (_markerGroup == null)
            EnsureMarkerGroup();

        if (_markerGroup == null) return;

        _markerGroup.alpha = visible ? 1f : 0f;
        _markerGroup.interactable = false;
        _markerGroup.blocksRaycasts = false;
    }



    private bool HasRequiredReferences()
    {
        return _markerRoot != null
            && _boundsRect != null
            && TargetTransform != null
            && WorldCamera != null;
    }



    private Transform TargetTransform
    {
        get
        {
            if (_target != null) return _target;

            if (_fallbackTarget == null)
            {
                FuseBox fuseBox = Object.FindFirstObjectByType<FuseBox>();
                if (fuseBox != null)
                    _fallbackTarget = fuseBox.transform;
            }

            return _fallbackTarget;
        }
    }



    private Camera WorldCamera
    {
        get
        {
            if (_worldCamera != null) return _worldCamera;
            return Camera.main;
        }
    }



    private Camera UICamera
    {
        get
        {
            if (_canvas == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            if (_canvas.worldCamera != null)
                return _canvas.worldCamera;

            return WorldCamera;
        }
    }



    private void UpdateMarkerPosition()
    {
        Camera camera = WorldCamera;
        Vector3 targetPosition = TargetTransform.position + _targetOffset;
        Vector3 viewportPoint = camera.WorldToViewportPoint(targetPosition);

        Vector2 viewportPadding = GetViewportPadding();
        bool isOnScreen = viewportPoint.z > 0f
            && viewportPoint.x >= viewportPadding.x
            && viewportPoint.x <= 1f - viewportPadding.x
            && viewportPoint.y >= viewportPadding.y
            && viewportPoint.y <= 1f - viewportPadding.y;

        if (isOnScreen)
        {
            SetMarkerToScreenPoint(camera.WorldToScreenPoint(targetPosition));
            SetMarkerRotation(Quaternion.identity);
            SetDirectionArrowVisible(false);
            return;
        }

        Vector2 direction = GetOffscreenDirection(viewportPoint);
        _markerRoot.anchoredPosition = GetEdgePosition(direction);
        SetMarkerRotation(GetDirectionRotation(direction));
        SetDirectionArrowVisible(true);
    }



    private void SetMarkerToScreenPoint(Vector3 screenPoint)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_boundsRect, screenPoint, UICamera, out Vector2 localPoint))
            _markerRoot.anchoredPosition = localPoint;
    }



    private Vector2 GetOffscreenDirection(Vector3 viewportPoint)
    {
        Vector2 direction = new Vector2(viewportPoint.x - 0.5f, viewportPoint.y - 0.5f);

        if (viewportPoint.z < 0f)
            direction *= -1f;

        if (direction.sqrMagnitude < DirectionEpsilon)
            direction = Vector2.down;

        return direction.normalized;
    }



    private Vector2 GetEdgePosition(Vector2 direction)
    {
        Rect rect = _boundsRect.rect;
        Vector2 center = rect.center;

        float halfWidth = Mathf.Max(0f, rect.width * 0.5f - _edgePadding);
        float halfHeight = Mathf.Max(0f, rect.height * 0.5f - _edgePadding);

        float scale = float.PositiveInfinity;
        if (Mathf.Abs(direction.x) > DirectionEpsilon)
            scale = Mathf.Min(scale, halfWidth / Mathf.Abs(direction.x));

        if (Mathf.Abs(direction.y) > DirectionEpsilon)
            scale = Mathf.Min(scale, halfHeight / Mathf.Abs(direction.y));

        if (float.IsInfinity(scale))
            scale = 0f;

        return center + direction * scale;
    }



    private Vector2 GetViewportPadding()
    {
        Rect rect = _boundsRect.rect;
        float paddingX = rect.width > 0f ? _edgePadding / rect.width : 0f;
        float paddingY = rect.height > 0f ? _edgePadding / rect.height : 0f;

        return new Vector2(
            Mathf.Clamp(paddingX, 0f, 0.49f),
            Mathf.Clamp(paddingY, 0f, 0.49f));
    }



    private Quaternion GetDirectionRotation(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f + _rotationOffsetDegrees;
        return Quaternion.Euler(0f, 0f, angle);
    }



    private void SetMarkerRotation(Quaternion rotation)
    {
        if (_rotationRoot != null)
            _rotationRoot.localRotation = rotation;
    }



    private void SetDirectionArrowVisible(bool visible)
    {
        if (_directionArrowObject != null && _directionArrowObject.activeSelf != visible)
            _directionArrowObject.SetActive(visible);
    }
}
