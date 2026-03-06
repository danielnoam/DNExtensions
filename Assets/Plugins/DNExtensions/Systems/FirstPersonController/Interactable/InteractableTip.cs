using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DNExtensions.Systems.FirstPersonController.Interactable
{
	/// <summary>
	/// Manages tooltip display for an interactable object, showing action and description prompts with animated transitions.
	/// </summary>
	[DisallowMultipleComponent]
	public class InteractableTip : MonoBehaviour
	{
		[Header("Settings")]
		[SerializeField] private string actionPrompt = "Action";
		[SerializeField, Multiline(4)] private string descriptionPrompt = "Description";
		[SerializeField, Range(0f, 1f)] private float maxAlpha = 0.5f;
		[SerializeField] private float animationDuration = 0.5f;

		[Header("References")]
		[SerializeField] private CanvasGroup tooltipCanvas;
		[SerializeField] private Image canvasBackground;
		[SerializeField] private TextMeshProUGUI actionText;
		[SerializeField] private TextMeshProUGUI descriptionText;

		private Color _defaultBackgroundColor;
		private Sequence _visibilitySequence;
		private Sequence _punchSequence;
		private Vector3 _tooltipCanvasDefaultSize;
		
		private void Awake()
		{
			if (tooltipCanvas)
			{
				tooltipCanvas.alpha = maxAlpha;
				_tooltipCanvasDefaultSize = tooltipCanvas.transform.localScale;
			}
			if (canvasBackground)_defaultBackgroundColor = canvasBackground.color;
			if (actionText) actionText.text = actionPrompt;
			if (descriptionText) descriptionText.text = descriptionPrompt;
			ToggleTooltip(false, false);
		}
		

		public void ToggleTooltip(bool visible, bool animate = true)
		{
			if (!tooltipCanvas) return;
			
			if (_visibilitySequence.isAlive) _visibilitySequence.Stop();

			if (animate)
			{
				_visibilitySequence = Sequence.Create()
					.Group(Tween.Alpha(tooltipCanvas, visible ? maxAlpha : 0, animationDuration));
			}
			else
			{
				tooltipCanvas.alpha = visible ? maxAlpha : 0;
			}
		}


		public void SetText(string action, string description)
		{
			if (!actionText || !descriptionText) return;
			
			actionText.text = action;
			descriptionText.text = description;
		}
		
		public void Punch(Color punchColor = default)
		{
			if (!tooltipCanvas) return;
			
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