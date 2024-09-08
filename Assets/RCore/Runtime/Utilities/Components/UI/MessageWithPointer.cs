/***
 * Author RadBear - nbhung71711@gmail.com - 2019
 **/
#pragma warning disable 0649

using System.Collections;
using TMPro;
using UnityEngine;
using RCore.Common;

namespace RCore.Components
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
        [SerializeField] private RectTransform mRectMessage;
        [SerializeField] private TextMeshProUGUI mTxtMessage;
        /// <summary>
        /// Pointer is an arrow which points down
        /// </summary>
        [SerializeField] private RectTransform mRectPointer;

        public int id;
        public RectTransform RectMessage => mRectMessage;
        public RectTransform RectPointer => mRectPointer;

        public void PointToTarget(RectTransform pTarget, PointerAlignment pAlignment, float pOffset = 0, bool pPostValidate = true)
        {
            mRectPointer.SetActive(true);

            mRectPointer.position = pTarget.position;
            var targetPivot = pTarget.pivot;
            var x = mRectPointer.anchoredPosition.x - pTarget.rect.width * targetPivot.x + pTarget.rect.width / 2f;
            var y = mRectPointer.anchoredPosition.y - pTarget.rect.height * targetPivot.y + pTarget.rect.height / 2f;
            mRectPointer.anchoredPosition = new Vector2(x, y);

            var targetBounds = pTarget.Bounds();
            var arrowBounds = mRectPointer.Bounds();
            var arrowPos = mRectPointer.anchoredPosition;

            switch (pAlignment)
            {
                case PointerAlignment.TopLeft:
                    arrowPos.y = arrowPos.y + targetBounds.size.y / 2 + arrowBounds.size.y / 2 + pOffset;
                    arrowPos.x = arrowPos.x - targetBounds.size.x / 2 - arrowBounds.size.x / 2 - pOffset;
                    mRectPointer.eulerAngles = new Vector3(0, 0, 45);
                    break;
                case PointerAlignment.Top:
                    arrowPos.y = arrowPos.y + targetBounds.size.y / 2 + arrowBounds.size.y / 2 + pOffset;
                    mRectPointer.eulerAngles = new Vector3(0, 0, 0);
                    break;
                case PointerAlignment.TopRight:
                    arrowPos.y = arrowPos.y + targetBounds.size.y / 2 + arrowBounds.size.y / 2 + pOffset;
                    arrowPos.x = arrowPos.x + targetBounds.size.x / 2 + arrowBounds.size.x / 2 + pOffset;
                    mRectPointer.eulerAngles = new Vector3(0, 0, -45);
                    break;
                case PointerAlignment.Left:
                    arrowPos.x = arrowPos.x - targetBounds.size.x / 2 - arrowBounds.size.x / 2 + pOffset;
                    mRectPointer.eulerAngles = new Vector3(0, 0, 90);
                    break;
                case PointerAlignment.Center:
                    break;
                case PointerAlignment.Right:
                    arrowPos.x = arrowPos.x + targetBounds.size.x / 2 + arrowBounds.size.x / 2 + pOffset;
                    mRectPointer.eulerAngles = new Vector3(0, 0, -90);
                    break;
                case PointerAlignment.BotLeft:
                    arrowPos.y = arrowPos.y - targetBounds.size.y / 2 - arrowBounds.size.y / 2 - pOffset;
                    arrowPos.x = arrowPos.x - targetBounds.size.x / 2 - arrowBounds.size.x / 2 - pOffset;
                    mRectPointer.eulerAngles = new Vector3(0, 0, -235);
                    break;
                case PointerAlignment.Bot:
                    arrowPos.y = arrowPos.y - targetBounds.size.y / 2 - arrowBounds.size.y / 2 - pOffset;
                    mRectPointer.eulerAngles = new Vector3(0, 0, 180);
                    break;
                case PointerAlignment.BotRight:
                    arrowPos.y = arrowPos.y - targetBounds.size.y / 2 - arrowBounds.size.y / 2 - pOffset;
                    arrowPos.x = arrowPos.x + targetBounds.size.x / 2 + arrowBounds.size.x / 2 + pOffset;
                    mRectPointer.eulerAngles = new Vector3(0, 0, 235);
                    break;
            }

            mRectPointer.anchoredPosition = arrowPos;
            enabled = true;

            if (pPostValidate)
                CoroutineUtil.StartCoroutine(IEPostValidatingPointer(pTarget, pAlignment, pOffset));
        }

        public void MessageToTarget(RectTransform pTarget, string pMessage, PointerAlignment pAlignment, Vector2 pSize, float pOffset = 30, bool pPostValidate = true)
        {
            mRectMessage.SetActive(true);
            mTxtMessage.text = pMessage;
            mRectMessage.sizeDelta = pSize;

            if (pTarget == null)
                mRectMessage.anchoredPosition = Vector2.zero;
            else
            {
                mRectMessage.position = pTarget.position;
                var targetPivot = pTarget.pivot;
                var x = mRectMessage.anchoredPosition.x - pTarget.rect.width * targetPivot.x + pTarget.rect.width / 2f;
                var y = mRectMessage.anchoredPosition.y - pTarget.rect.height * targetPivot.y + pTarget.rect.height / 2f;
                mRectMessage.anchoredPosition = new Vector2(x, y);

                var targetBounds = pTarget.Bounds();
                var boxBounds = mRectMessage.Bounds();
                var messageBoxPos = mRectMessage.anchoredPosition;

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
                mRectMessage.anchoredPosition = messageBoxPos;
            }
            enabled = true;

            if (pPostValidate)
                CoroutineUtil.StartCoroutine(IEPostValidatingMessage(pTarget, pMessage, pAlignment, pSize, pOffset));
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