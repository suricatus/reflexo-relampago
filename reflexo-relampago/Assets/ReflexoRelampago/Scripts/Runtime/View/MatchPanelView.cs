using Suricatus.FastDrive;
using UnityEngine.UIElements;

namespace Suricatus.ReflexoRelampago
{
    /// <summary>
    /// A área de apoio: a fase da partida, acertos e precisão, e a legenda do que tocar e do que evitar.
    /// </summary>
    public sealed class MatchPanelView : VisualElement
    {
        const int PopMs = 220;

        readonly Texts texts;
        readonly Label phaseLabel;
        readonly Label hitsLabel;
        readonly Label precisionLabel;
        Phase? phase;

        public MatchPanelView(UiFactory ui, Texts texts, GameArts arts, bool showDistraction)
        {
            this.texts = texts;
            AddToClassList("rr-panel");
            ui.StyleCard(this);

            phaseLabel = ui.Title("", onCard: true);
            phaseLabel.AddToClassList("rr-panel__phase");
            Add(phaseLabel);

            var stats = Div("rr-stats");
            hitsLabel = Stat(ui, stats, texts.Get("reflexo.acertos", "Acertos"));
            precisionLabel = Stat(ui, stats, texts.Get("reflexo.precisao", "Precisão"));
            Add(stats);

            var legend = Div("rr-legend");
            legend.Add(LegendItem(ui, PieceLook.Target(ui, arts.Target(0), out _), texts.Get("reflexo.toque", "Toque")));
            if (showDistraction)
                legend.Add(LegendItem(ui, PieceLook.Distraction(ui, arts.Distraction(0)), texts.Get("reflexo.evite", "Evite")));
            Add(legend);
        }

        public void SetPhase(Phase value)
        {
            if (phase == value) return;
            bool first = phase == null;
            phase = value;
            phaseLabel.text = texts.Get(PhaseKey(value), PhaseDefault(value));
            if (first) return;
            phaseLabel.AddToClassList("rr-panel__phase--pop");
            phaseLabel.schedule.Execute(() => phaseLabel.RemoveFromClassList("rr-panel__phase--pop")).StartingIn(PopMs);
        }

        public void SetStats(int hits, int precisionPercent)
        {
            hitsLabel.text = hits.ToString();
            precisionLabel.text = precisionPercent + "%";
        }

        static string PhaseKey(Phase phase)
        {
            switch (phase)
            {
                case Phase.Aquecimento: return "reflexo.fase.aquecimento";
                case Phase.Sprint: return "reflexo.fase.sprint";
                default: return "reflexo.fase.desafio";
            }
        }

        static string PhaseDefault(Phase phase)
        {
            switch (phase)
            {
                case Phase.Aquecimento: return "Aquecimento";
                case Phase.Sprint: return "Sprint final!";
                default: return "Desafio";
            }
        }

        static Label Stat(UiFactory ui, VisualElement parent, string caption)
        {
            var block = Div("rr-stat");
            var value = ui.Title("0", onCard: true);
            value.AddToClassList("rr-stat__value");
            block.Add(value);
            var label = ui.Text(caption, soft: true);
            label.AddToClassList("rr-stat__label");
            block.Add(label);
            parent.Add(block);
            return value;
        }

        static VisualElement LegendItem(UiFactory ui, VisualElement look, string caption)
        {
            var item = Div("rr-legend__item");
            var icon = Div("rr-legend__icon");
            icon.Add(look);
            item.Add(icon);
            var label = ui.Text(caption);
            label.AddToClassList("rr-legend__label");
            item.Add(label);
            return item;
        }

        static VisualElement Div(string className)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.AddToClassList(className);
            return element;
        }
    }
}
