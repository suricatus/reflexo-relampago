using System;
using System.Collections.Generic;
using Suricatus.FastDrive;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suricatus.ReflexoRelampago
{
    /// <summary>
    /// A área do jogo: os alvos e as distrações aparecem e somem aqui. Toque em peça vai para
    /// <c>onPieceTap</c>; toque no vazio, para <c>onEmptyTap</c> (conta na precisão).
    /// </summary>
    public sealed class ArenaView : VisualElement
    {
        const int NextFrameMs = 16;
        const int RemoveAfterMs = 280;
        const int FloatMs = 700;
        const int RippleMs = 340;

        readonly UiFactory ui;
        readonly GameArts arts;
        readonly VisualElement field;
        readonly Action<int> onPieceTap;
        readonly Action onEmptyTap;
        readonly Dictionary<int, VisualElement> pieces = new Dictionary<int, VisualElement>();
        bool locked;

        public ArenaView(UiFactory ui, GameArts arts, Action<int> onPieceTap, Action onEmptyTap)
        {
            this.ui = ui;
            this.arts = arts;
            this.onPieceTap = onPieceTap;
            this.onEmptyTap = onEmptyTap;

            AddToClassList("rr-arena");
            ui.StyleCard(this);
            field = new VisualElement();
            field.AddToClassList("rr-field");
            Add(field);
            field.RegisterCallback<PointerDownEvent>(OnEmptyDown);
        }

        /// <summary>Trava a área (fim da partida).</summary>
        public void Lock() => locked = true;

        public void Show(Piece piece)
        {
            var element = new VisualElement();
            element.AddToClassList("rr-piece");
            element.AddToClassList("rr-piece--enter");
            element.style.left = Length.Percent((piece.X - piece.Size / 2) * 100);
            element.style.top = Length.Percent((piece.Y - piece.Size / 2) * 100);
            element.style.width = Length.Percent(piece.Size * 100);
            element.style.height = Length.Percent(piece.Size * 100);

            if (piece.Kind == PieceKind.Target)
            {
                element.Add(PieceLook.Target(ui, arts.Target(piece.Variant), out var halo));
                // O halo encolhe enquanto o alvo está na tela: mostra quanto tempo ainda falta.
                halo.style.transitionDuration = new List<TimeValue> { new TimeValue(piece.VisibleSeconds, TimeUnit.Second) };
                halo.schedule.Execute(() => halo.AddToClassList("rr-look__halo--closing")).StartingIn(NextFrameMs);
            }
            else
            {
                element.Add(PieceLook.Distraction(ui, arts.Distraction(piece.Variant)));
            }

            int id = piece.Id;
            // Peça saindo continua recebendo o toque, sem contar: um toque duplo rápido não vira erro.
            element.RegisterCallback<PointerDownEvent>(evt =>
            {
                evt.StopPropagation();
                if (!locked) onPieceTap?.Invoke(id);
            });

            field.Add(element);
            pieces[id] = element;
            element.schedule.Execute(() => element.RemoveFromClassList("rr-piece--enter")).StartingIn(NextFrameMs);
        }

        public void Hide(Piece piece, PieceEnd end)
        {
            if (!pieces.TryGetValue(piece.Id, out var element)) return;
            pieces.Remove(piece.Id);
            element.AddToClassList(EndClass(end));
            element.schedule.Execute(element.RemoveFromHierarchy).StartingIn(RemoveAfterMs);
        }

        /// <summary>"-50" subindo de onde estava a distração tocada.</summary>
        public void ShowPenalty(Piece piece, int points)
        {
            if (points <= 0) return;
            var label = new Label("-" + points) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("rr-float");
            ui.TitleFont(label);
            label.style.color = ui.Theme.Error;
            label.style.left = Length.Percent(piece.X * 100);
            label.style.top = Length.Percent(piece.Y * 100);
            field.Add(label);
            label.schedule.Execute(() => label.AddToClassList("rr-float--up")).StartingIn(NextFrameMs);
            label.schedule.Execute(label.RemoveFromHierarchy).StartingIn(FloatMs);
        }

        void OnEmptyDown(PointerDownEvent evt)
        {
            if (locked) return;
            onEmptyTap?.Invoke();

            var ring = new VisualElement { pickingMode = PickingMode.Ignore };
            ring.AddToClassList("rr-ripple");
            ring.style.left = evt.localPosition.x;
            ring.style.top = evt.localPosition.y;
            var color = ui.Theme.SoftTextOnCard;
            ring.style.borderTopColor = color;
            ring.style.borderRightColor = color;
            ring.style.borderBottomColor = color;
            ring.style.borderLeftColor = color;
            field.Add(ring);
            ring.schedule.Execute(() => ring.AddToClassList("rr-ripple--out")).StartingIn(NextFrameMs);
            ring.schedule.Execute(ring.RemoveFromHierarchy).StartingIn(RippleMs);
        }

        static string EndClass(PieceEnd end)
        {
            switch (end)
            {
                case PieceEnd.Hit: return "rr-piece--hit";
                case PieceEnd.Expired: return "rr-piece--expired";
                case PieceEnd.Tapped: return "rr-piece--wrong";
                default: return "rr-piece--gone";
            }
        }
    }
}
