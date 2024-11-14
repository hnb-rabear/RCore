/***
 * Author HNB-RaBear - 2019
 **/

#pragma warning disable 0649

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCore.UI
{
	public enum PointerAlignment
	{
		TopLeft,
		Top,
		TopRight,
		Left,
		Center,
		Right,
		BotLeft,
		Bot,
		BotRight,
	}

	/// <summary>
	/// Show a message or pointer to a recttransform target
	/// </summary>
	public class MessageWithPointer : MonoBehaviour
	{
		[FormerlySerializedAs("mRectMessage")]
		[SerializeField] private RectTransform m_rectMessage;
		[FormerlySerializedAs("mTxtMessage")]
		[SerializeField] private TextMeshProUGUI m_txtMessage;
		/// <summary>
		/// Pointer is an arrow which points down
		/// </summary>
		[FormerlySerializedAs("mRectPointer")]
		[SerializeField] private RectTransform m_rectPointer;

		public int id;
		public RectTransform RectMessage => m_rectMessage;
		public RectTransform RectPointer => m_rectPointer;

		public void PointToTarget(RectTransform pTarget, PointerAlignment pAlignment, float pOffset = 0, bool pPostValidate = true)
		{
			m_rectPointer.SetActive(true);

			m_rectPointer.position = pTarget.position;
			var targetPivot = pTarget.pivot;
			var x = m_rectPointer.anchoredPosition.x - pTarget.rect.width * targetPivot.x + pTarget.rect.width / 2f;
			var y = m_rectPointer.anchoredPosition.y - pTarget.rect.height * targetPivot.y + pTarget.rect.height / 2f;
			m_rectPointer.anchoredPosition = new Vector2(x, y);

			var targetBounds = pTarget.Bounds();
			var arrowBounds = m_rectPointer.Bounds();
			var arrowPos = m_rectPointer.anchoredPosition;

			switch (pAlignment)
			{
				case PointerAlignment.TopLeft:
					arrowPos.y = arrowPos.y + targetBounds.size.y / 2 + arrowBounds.size.y / 2 + pOffset;
					arrowPos.x = arrowPos.x - targetBounds.size.x / 2 - arrowBounds.size.x / 2 - pOffset;
					m_rectPointer.eulerAngles = new Vector3(0, 0, 45);
					break;
				case PointerAlignment.Top:
					arrowPos.y = arrowPos.y + targetBounds.size.y / 2 + arrowBounds.size.y / 2 + pOffset;
					m_rectPointer.eulerAngles = new Vector3(0, 0, 0);
					break;
				case PointerAlignment.TopRight:
					arrowPos.y = arrowPos.y + targetBounds.size.y / 2 + arrowBounds.size.y / 2 + pOffset;
					arrowPos.x = arrowPos.x + targetBounds.size.x / 2 + arrowBounds.size.x / 2 + pOffset;
					m_rectPointer.eulerAngles = new Vector3(0, 0, -45);
					break;
				case PointerAlignment.Left:
					arrowPos.x = arrowPos.x - targetBounds.size.x / 2 - arrowBounds.size.x / 2 + pOffset;
					m_rectPointer.eulerAngles = new Vector3(0, 0, 90);
					break;
				case PointerAlignment.Center:
					break;
				case PointerAlignment.Right:
					arrowPos.x = arrowPos.x + targetBounds.size.x / 2 + arrowBounds.size.x / 2 + pOffset;
					m_rectPointer.eulerAngles = new Vector3(0, 0, -90);
					break;
				case PointerAlignment.BotLeft:
					arrowPos.y = arrowPos.y - targetBounds.size.y / 2 - arrowBounds.size.y / 2 - pOffset;
					arrowPos.x = arrowPos.x - targetBounds.size.x / 2 - arrowBounds.size.x / 2 - pOffset;
					m_rectPointer.eulerAngles = new Vector3(0, 0, -235);
					break;
				case PointerAlignment.Bot:
					arrowPos.y = arrowPos.y - targetBounds.size.y / 2 - arrowBounds.size.y / 2 - pOffset;
					m_rectPointer.eulerAngles = new Vector3(0, 0, 180);
					break;
				case PointerAlignment.BotRight:
					arrowPos.y = arrowPos.y - targetBounds.size.y / 2 - arrowBounds.size.y / 2 - pOffset;
					arrowPos.x = arrowPos.x + targetBounds.size.x / 2 + arrowBounds.size.x / 2 + pOffset;
					m_rectPointer.eulerAngles = new Vector3(0, 0, 235);
					break;
			}

			m_rectPointer.anchoredPosition = arrowPos;
			enabled = true;

			if (pPostValidate)
				TimerEventsInScene.Instance.StartCoroutine(IEPostValidatingPointer(pTarget, pAlignment, pOffset));
		}

		public void MessageToTarget(RectTransform pTarget, string pMessage, PointerAlignment pAlignment, Vector2 pSize, float pOffset = 30, bool pPostValidate = true)
		{
			m_rectMessage.SetActive(true);
			m_txtMessage.text = pMessage;
			m_rectMessage.sizeDelta = pSize;

			if (pTarget == null)
				m_rectMessage.anchoredPosition = Vector2.zero;
			else
			{
				m_rectMessage.position = pTarget.position;
				var targetPivot = pTarget.pivot;
				var x = m_rectMessage.anchoredPosition.x - pTarget.rect.width * targetPivot.x + pTarget.rect.width / 2f;
				var y = m_rectMessage.anchoredPosition.y - pTarget.rect.height * targetPivot.y + pTarget.rect.height / 2f;
				m_rectMessage.anchoredPosition = new Vector2(x, y);

				var targetBounds = pTarget.Bounds();
				var boxBounds = m_rectMessage.Bounds();
				var messageBoxPos = m_rectMessage.anchoredPosition;

				switch (pAlignment)
				{
					case PointerAlignment.TopLeft:
						messageBoxPos.y = messageBoxPos.y + targetBounds.size.y / 2 + boxBounds.size.y / 2 + pOffset;
						messageBoxPos.x = messageBoxPos.x - targetBounds.size.x / 2 - boxBounds.size.x / 2 - pOffset;
						break;
					case PointerAlignment.Top:
						messageBoxPos.y = messageBoxPos.y + targetBounds.size.y / 2 + boxBounds.size.y / 2 + pOffset;
						break;
					case PointerAlignment.TopRight:
						messageBoxPos.y = messageBoxPos.y + targetBounds.size.y / 2 + boxBounds.size.y / 2 + pOffset;
						messageBoxPos.x = messageBoxPos.x + targetBounds.size.x / 2 + boxBounds.size.x / 2 + pOffset;
						break;
					case PointerAlignment.Left:
						messageBoxPos.x = messageBoxPos.x - targetBounds.size.x / 2 - boxBounds.size.x / 2 - pOffset;
						break;
					case PointerAlignment.Center:
						break;
					case PointerAlignment.Right:
						messageBoxPos.x = messageBoxPos.x + targetBounds.size.x / 2 + boxBounds.size.x / 2 + pOffset;
						break;
					case PointerAlignment.BotLeft:
						messageBoxPos.y = messageBoxPos.y - targetBounds.size.y / 2 - boxBounds.size.y / 2 - pOffset;
						messageBoxPos.x = messageBoxPos.x - targetBounds.size.x / 2 - boxBounds.size.x / 2 - pOffset;
						break;
					case PointerAlignment.Bot:
						messageBoxPos.y = messageBoxPos.y - targetBounds.size.y / 2 - boxBounds.size.y / 2 - pOffset;
						break;
					case PointerAlignment.BotRight:
						messageBoxPos.y = messageBoxPos.y - targetBounds.size.y / 2 - boxBounds.size.y / 2 - pOffset;
						messageBoxPos.x = messageBoxPos.x + targetBounds.size.x / 2 + boxBounds.size.x / 2 + pOffset;
						break;
				}
				m_rectMessage.anchoredPosition = messageBoxPos;
			}
			enabled = true;

			if (pPostValidate)
                TimerEventsInScene.Instance.StartCoroutine(IEPostValidatingMessage(pTarget, pMessage, pAlignment, pSize, pOffset));
		}

		private IEnumerator IEPostValidatingPointer(RectTransform pTarget, PointerAlignment pAlignment, float pOffset = 0)
		{
			for (int i = 0; i < 5; i++)
			{
				yield return null;
				PointToTarget(pTarget, pAlignment, pOffset, false);
			}
		}

		private IEnumerator IEPostValidatingMessage(RectTransform pTarget, string pMessage, PointerAlignment pAlignment, Vector2 pSize, float pOffset = 30)
		{
			for (int i = 0; i < 5; i++)
			{
				yield return null;
				try
				{
					MessageToTarget(pTarget, pMessage, pAlignment, pSize, pOffset, false);
				}
				catch { }
			}
		}
	}
}