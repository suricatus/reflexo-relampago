using System.Collections.Generic;
using Suricatus.FastDrive;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suricatus.ReflexoRelampago
{
    /// <summary>
    /// As artes da partida: alvos (jogo/alvo*.png) e distrações (jogo/distracao*.png) do cliente.
    /// Sem alvo, vale o logo quadrado; sem distração, um X na cor de erro do tema.
    /// </summary>
    public sealed class GameArts
    {
        public const string TargetName = "alvo";
        public const string DistractionName = "distracao";

        public GameArts(ClientBundle bundle)
        {
            var targets = bundle != null ? bundle.ArtsNamed(TargetName) : new List<Texture2D>();
            if (targets.Count == 0 && bundle != null && bundle.logoQuadrado != null) targets.Add(bundle.logoQuadrado);
            Targets = targets;
            Distractions = bundle != null ? bundle.ArtsNamed(DistractionName) : new List<Texture2D>();
        }

        public IReadOnlyList<Texture2D> Targets { get; }
        public IReadOnlyList<Texture2D> Distractions { get; }

        public Texture2D Target(int variant) => Pick(Targets, variant);
        public Texture2D Distraction(int variant) => Pick(Distractions, variant);

        static Texture2D Pick(IReadOnlyList<Texture2D> list, int variant) => list.Count == 0 ? null : list[(variant % list.Count + list.Count) % list.Count];
    }

    /// <summary>O desenho de alvos e distrações, igual na área do jogo e na legenda.</summary>
    public static class PieceLook
    {
        const float HaloAlpha = 0.5f;

        /// <summary>Alvo: a arte sobre um halo na cor primária, que deixa claro o que tocar.</summary>
        public static VisualElement Target(UiFactory ui, Texture2D art, out VisualElement halo)
        {
            var root = Div("rr-look");
            halo = Div("rr-look__halo");
            halo.style.backgroundColor = WithAlpha(ui.Theme.Primary, HaloAlpha);
            root.Add(halo);
            root.Add(art != null ? Art(art) : Bullseye(ui.Theme));
            return root;
        }

        /// <summary>Distração: só a arte, sem halo; sem arte, um X na cor de erro.</summary>
        public static VisualElement Distraction(UiFactory ui, Texture2D art)
        {
            var root = Div("rr-look");
            root.Add(art != null ? Art(art) : Cross(ui.Theme));
            return root;
        }

        static VisualElement Art(Texture2D texture)
        {
            var art = Div("rr-look__art");
            art.style.backgroundImage = new StyleBackground(texture);
            return art;
        }

        static VisualElement Bullseye(ThemeData theme)
        {
            var outer = Div("rr-look__disc");
            outer.style.backgroundColor = theme.Primary;
            var middle = Div("rr-look__ring");
            middle.style.backgroundColor = theme.Card;
            var inner = Div("rr-look__dot");
            inner.style.backgroundColor = theme.Primary;
            middle.Add(inner);
            outer.Add(middle);
            return outer;
        }

        static VisualElement Cross(ThemeData theme)
        {
            var disc = Div("rr-look__disc");
            disc.style.backgroundColor = theme.Error;
            foreach (var angle in new[] { 45f, -45f })
            {
                var bar = Div("rr-look__bar");
                bar.style.backgroundColor = theme.Card;
                bar.style.rotate = new Rotate(angle);
                disc.Add(bar);
            }
            return disc;
        }

        static VisualElement Div(string className)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.AddToClassList(className);
            return element;
        }

        static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
    }
}
