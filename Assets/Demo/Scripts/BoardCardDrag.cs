using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hakaton.Demo
{
    public sealed class BoardCardDrag : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public string EntryId { get; private set; }
        DetectiveBoardView view;
        RectTransform rect;
        Vector2 offset;
        bool dragging;
        Text title, excerpt, category, source;
        Outline outline;

        public void Initialize(DetectiveBoardView owner, DetectiveBoard.Entry entry, Text heading, Text body, Text type, Text sourceLabel, Outline border)
        {
            view = owner; EntryId = entry.id; rect = (RectTransform)transform;
            title = heading; excerpt = body; category = type; source = sourceLabel; outline = border;
            Refresh(entry, false);
        }

        public void Refresh(DetectiveBoard.Entry entry, bool selected)
        {
            title.text = entry.title;
            excerpt.text = entry.text.Length > 148 ? entry.text.Substring(0, 145) + "…" : entry.text;
            if (entry.isDeduction)
            {
                category.text = "ВЫВОД / " + entry.deductionId;
                source.text = "ЗАФИКСИРОВАН НА ДОСКЕ";
            }
            else
            {
                category.text = entry.isEvidence ? "УЛИКА / " + DemoGame.EvidenceNumber(entry.sourceId) : "ЗАПИСЬ ПО ДЕЛУ";
                source.text = entry.sourceId >= 0 ? "ОСНОВАНИЕ: ПРЕДМЕТ " + DemoGame.EvidenceNumber(entry.sourceId) : "ПОЛЕВЫЕ ЗАПИСИ";
            }
            outline.effectColor = selected ? new Color(0.84f, 0.54f, 0.22f) : new Color(0, 0, 0, 0.38f);
            outline.effectDistance = selected ? new Vector2(3, -3) : new Vector2(2, -3);
            rect.anchoredPosition = new Vector2(entry.position.x, -entry.position.y);
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left) return;
            view.Select(EntryId);
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left) return;
            view.Select(EntryId);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(view.Surface, data.position, data.pressEventCamera, out var point)) return;
            offset = point - rect.anchoredPosition;
            dragging = true;
        }

        public void OnDrag(PointerEventData data)
        {
            if (!dragging || data.button != PointerEventData.InputButton.Left) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(view.Surface, data.position, data.pressEventCamera, out var point)) return;
            Vector2 position = point - offset;
            view.Move(EntryId, new Vector2(position.x, -position.y));
        }

        public void OnEndDrag(PointerEventData data)
        {
            if (!dragging) return;
            OnDrag(data);
            dragging = false;
            view.Status("Положение карточки сохранено");
        }
    }
}
