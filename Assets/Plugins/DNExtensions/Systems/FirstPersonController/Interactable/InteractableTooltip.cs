using DNExtensions.Utilities;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DNExtensions.Systems.FirstPersonController.Interactable
{
	[DisallowMultipleComponent]
	public class InteractableTooltip : MonoBehaviour
	{
		[Header("Settings")]
		[SerializeField] private string actionPrompt = "Action";
		[SerializeField, Multiline(4)] private string descriptionPrompt = "Description";
		[SerializeField, Range(0f, 1f)] private float maxAlpha = 0.5f;
		[SerializeField] private float animationDuration = 0.5f;

		[Header("References")]
		[SerializeField] private Interactable interactable;
		[SerializeField] private CanvasGroup tooltipCanvas;
		[SerializeField] private Image canvasBackground;
		[SerializeField] private TextMeshProUGUI actionText;
		[SerializeField] private TextMeshProUGUI descriptionText;

		private Color _defaultBackgroundColor;
		private Camera _camera;
		private Sequence _visibilitySequence;
		private Sequence _punchSequence;
		private Vector3 _tooltipCanvasDefaultSize;
		private bool _isVisible = true;

		private void OnValidate()
		{
			if (!interactable) interactable = GetComponentInParent<Interactable>();
			if (!tooltipCanvas) tooltipCanvas = GetComponentInChildren<CanvasGroup>();
			if (tooltipCanvas) tooltipCanvas.alpha = maxAlpha;

			actionText.text = actionPrompt;
			descriptionText.text = descriptionPrompt;
		}

		private void Awake()
		{
			_defaultBackgroundColor = canvasBackground.color;
			_tooltipCanvasDefaultSize = tooltipCanvas.transform.localScale;
			_camera = Camera.main;
			ToggleTooltip(false, false);
		}

		private void OnEnable()
		{
			interactable.OnHighlight += InteractableOnOnHighlight;
			interactable.OnUnHighlight += InteractableOnOnUnHighlight;
			interactable.OnInteract += InteractableOnOnInteract;
		}

		private void OnDisable()
		{
			interactable.OnHighlight -= InteractableOnOnHighlight;
			interactable.OnUnHighlight -= InteractableOnOnUnHighlight;
			interactable.OnInteract -= InteractableOnOnInteract;
		}

		private void Update()
		{
			if (_isVisible)
			{
				var direction = _camera.transform.position - transform.position;

				if (direction.sqrMagnitude > 0.001f)
				{
					var lookRotation = Quaternion.LookRotation(direction);
					var eulerAngles = lookRotation.eulerAngles.AddY(180);
					eulerAngles.x = -eulerAngles.x;
					eulerAngles.z = 0;
					transform.rotation = Quaternion.Euler(eulerAngles);
				}
			}
		}

		private void InteractableOnOnInteract(FPCInteraction interactor)
		{

		}

		private void InteractableOnOnUnHighlight()
		{
			ToggleTooltip(false);
		}

		private void InteractableOnOnHighlight()
		{
			ToggleTooltip(true);
		}

		private void ToggleTooltip(bool isVisible, bool animate = true)
		{
			if (_visibilitySequence.isAlive) _visibilitySequence.Stop();

			if (animate)
			{
				if (isVisible) _isVisible = true;

				_visibilitySequence = Sequence.Create()
					.Group(Tween.Alpha(tooltipCanvas, isVisible ? maxAlpha : 0, animationDuration))
					.OnComplete((() => { if (!isVisible) _isVisible = false; }));
			}
			else
			{
				tooltipCanvas.alpha = isVisible ? maxAlpha : 0;
				_isVisible = isVisible;
			}
		}

		public void SetText(string action, string description)
		{
			actionText.text = action;
			descriptionText.text = $"{description}";
		}

		public void Punch(Color punchColor = default)
		{
			if (_punchSequence.isAlive) _punchSequence.Stop();

			tooltipCanvas.transform.localScale = _tooltipCanvasDefaultSize;
			_punchSequence = Sequence.Create()
				.Group(Tween.PunchScale(tooltipCanvas.transform, Vector3.one * 0.02f, 0.2f, frequency: 1));

			if (punchColor != default)
			{
				_punchSequence.Group(Tween.Color(canvasBackground, punchColor, 0.2f));
				_punchSequence.Chain(Tween.Color(canvasBackground, _defaultBackgroundColor, 0.2f));
			}
		}
	}
}