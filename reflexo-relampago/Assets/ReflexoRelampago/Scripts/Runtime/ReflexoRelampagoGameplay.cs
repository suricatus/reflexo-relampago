using System.Collections.Generic;
using Suricatus.FastDrive;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suricatus.ReflexoRelampago
{
    /// <summary>
    /// A mecânica do Reflexo Relâmpago. Tudo o mais (telas, placar, tempo, pacotes) vem da Base do Fast-Drive.
    /// O ritmo, a pontuação e as faixas vêm do conteudo/reflexo.json do cliente; as artes, da pasta jogo/.
    /// </summary>
    public sealed class ReflexoRelampagoGameplay : GameplayProvider
    {
        const string DefaultTitle = "Reflexo Relâmpago";
        const string DefaultSubtitle = "Toque nos alvos o mais rápido que puder!";
        const string DefaultResult = "Acertos: {acertos} de {alvos} · Precisão: {precisao}%";

        readonly System.Random random = new System.Random();

        GameContext context;
        ReflexoContent content = new ReflexoContent();
        GameArts arts;
        MatchContext match;
        ReflexMatch state;
        ArenaView arena;
        MatchPanelView panel;
        int lastPrecision;

        public override string Title => context != null ? context.Texts.Get("reflexo.titulo", DefaultTitle) : DefaultTitle;

        public override string Subtitle => context?.Texts.Get("reflexo.subtitulo", DefaultSubtitle);

        public override IReadOnlyList<string> HowToPlay => content.HowToPlay;

        public override void Configure(GameContext gameContext)
        {
            context = gameContext;
            var asset = context.Bundle.FindContent(ReflexoContent.FileName);
            if (asset == null)
            {
                Debug.LogWarning($"[Reflexo Relâmpago] O cliente \"{context.Bundle.clientId}\" não tem conteudo/{ReflexoContent.FileName}.json: valem os padrões do jogo.");
            }
            else
            {
                try
                {
                    content = ReflexoContent.Parse(asset.text);
                }
                catch (System.FormatException e)
                {
                    Debug.LogError($"[Reflexo Relâmpago] conteudo/{ReflexoContent.FileName}.json: {e.Message}");
                }
            }

            arts = new GameArts(context.Bundle);
            if (arts.Targets.Count == 0)
                Debug.LogWarning($"[Reflexo Relâmpago] O cliente \"{context.Bundle.clientId}\" não tem jogo/alvo.png nem marca/logo-quadrado.png: o alvo vai ser desenhado com as cores do tema.");

            var styles = Resources.Load<StyleSheet>("ReflexoRelampago");
            if (styles != null) context.Root.styleSheets.Add(styles);
        }

        public override void BeginMatch(MatchContext matchContext, VisualElement gameArea, VisualElement supportArea)
        {
            match = matchContext;
            state = new ReflexMatch(content, match.TotalSeconds, random, arts.Targets.Count, arts.Distractions.Count);

            arena = new ArenaView(context.Ui, arts, TapPiece, TapEmpty);
            gameArea.Add(arena);
            panel = new MatchPanelView(context.Ui, context.Texts, arts, content.HasDistractions);
            supportArea.Add(panel);

            state.Shown += arena.Show;
            state.Hidden += arena.Hide;
            panel.SetPhase(state.Phase);
            panel.SetStats(0, 0);
        }

        public override void TickMatch(float deltaTime)
        {
            if (state == null || match.Finished) return;
            state.Tick(deltaTime);
            panel.SetPhase(state.Phase);
        }

        public override void TimeUp()
        {
            if (state == null) return;
            state.Stop();
            arena.Lock();
            lastPrecision = state.PrecisionPercent;
            var tier = content.TierFor(match.Score);
            match.Finish(tier != null && tier.Win, state.Hits, state.Goal);
        }

        public override void EndMatch()
        {
            if (state != null)
            {
                state.Shown -= arena.Show;
                state.Hidden -= arena.Hide;
            }
            arena?.RemoveFromHierarchy();
            panel?.RemoveFromHierarchy();
            arena = null;
            panel = null;
            state = null;
            match = null;
        }

        public override string ResultDetail(GameResult result)
        {
            var line = context.Texts.Get("reflexo.resultado", DefaultResult)
                .Replace("{acertos}", result.Hits.ToString())
                .Replace("{alvos}", result.Goal.ToString())
                .Replace("{precisao}", lastPrecision.ToString());
            var tier = content.TierFor(result.Score);
            return tier == null ? line : "<b>" + tier.Name + "</b>\n" + line;
        }

        void TapPiece(int id)
        {
            if (state == null || match.Finished) return;
            var tap = state.TapPiece(id);
            switch (tap.Outcome)
            {
                case TapOutcome.Hit:
                    match.Hit(tap.Points);
                    break;
                case TapOutcome.Distraction:
                    int taken = MatchContext.PenaltyFor(match.Score, tap.Points);
                    match.Miss(tap.Points);
                    arena.ShowPenalty(tap.Piece, taken);
                    break;
                default:
                    return;
            }
            panel.SetStats(state.Hits, state.PrecisionPercent);
        }

        void TapEmpty()
        {
            if (state == null || match.Finished) return;
            state.TapEmpty();
            panel.SetStats(state.Hits, state.PrecisionPercent);
        }
    }
}
