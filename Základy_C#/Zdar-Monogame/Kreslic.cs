using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Zdar_Monogame
{
    // Cela hra je jen z obdelniku. Staci na to jedina textura velka jeden bily
    // pixel - pri kresleni se roztahne na jakoukoliv velikost a obarvi se.
    // Diky tomu hra nepotrebuje zadny obrazek ani Content pipeline.
    static class Kreslic
    {
        static Texture2D pixel;

        public static void Priprav(GraphicsDevice zarizeni)
        {
            pixel = new Texture2D(zarizeni, 1, 1);
            pixel.SetData(new[] { Color.White });
        }

        public static void Obdelnik(SpriteBatch davka, Rectangle kam, Color barva)
        {
            davka.Draw(pixel, kam, barva);
        }

        public static void Obdelnik(SpriteBatch davka, int x, int y, int sirka, int vyska, Color barva)
        {
            davka.Draw(pixel, new Rectangle(x, y, sirka, vyska), barva);
        }

        // ---------- cisla slozena ze sedmi obdelniku ----------
        // Protoze nemame zadny font, kreslime cisla jako na digitalkach.
        // Poradi segmentu: A horni, B pravy horni, C pravy dolni, D spodni,
        //                  E levy dolni, F levy horni, G prostredni.
        static readonly bool[,] Segmenty =
        {
            { true,  true,  true,  true,  true,  true,  false },  // 0
            { false, true,  true,  false, false, false, false },  // 1
            { true,  true,  false, true,  true,  false, true  },  // 2
            { true,  true,  true,  true,  false, false, true  },  // 3
            { false, true,  true,  false, false, true,  true  },  // 4
            { true,  false, true,  true,  false, true,  true  },  // 5
            { true,  false, true,  true,  true,  true,  true  },  // 6
            { true,  true,  true,  false, false, false, false },  // 7
            { true,  true,  true,  true,  true,  true,  true  },  // 8
            { true,  true,  true,  true,  false, true,  true  },  // 9
        };

        public static void Cislice(SpriteBatch davka, int cislice, int x, int y,
                                   int sirka, int vyska, int tloustka, Color barva)
        {
            int pulka = vyska / 2;

            if (Segmenty[cislice, 0]) Obdelnik(davka, x, y, sirka, tloustka, barva);
            if (Segmenty[cislice, 1]) Obdelnik(davka, x + sirka - tloustka, y, tloustka, pulka, barva);
            if (Segmenty[cislice, 2]) Obdelnik(davka, x + sirka - tloustka, y + pulka, tloustka, pulka, barva);
            if (Segmenty[cislice, 3]) Obdelnik(davka, x, y + vyska - tloustka, sirka, tloustka, barva);
            if (Segmenty[cislice, 4]) Obdelnik(davka, x, y + pulka, tloustka, pulka, barva);
            if (Segmenty[cislice, 5]) Obdelnik(davka, x, y, tloustka, pulka, barva);
            if (Segmenty[cislice, 6]) Obdelnik(davka, x, y + pulka - tloustka / 2, sirka, tloustka, barva);
        }

        public static void Cislo(SpriteBatch davka, int cislo, int x, int y,
                                 int sirka, int vyska, int tloustka, Color barva)
        {
            string text = cislo.ToString();

            for (int i = 0; i < text.Length; i++)
            {
                int hodnota = text[i] - '0';
                Cislice(davka, hodnota, x + i * Rozestup(sirka, tloustka),
                        y, sirka, vyska, tloustka, barva);
            }
        }

        // Jak siroke bude cele cislo - potrebujeme to, kdyz ho chceme vycentrovat.
        public static int SirkaCisla(int cislo, int sirka, int tloustka)
        {
            int pocet = cislo.ToString().Length;
            return pocet * Rozestup(sirka, tloustka) - tloustka * 2;
        }

        static int Rozestup(int sirka, int tloustka)
        {
            return sirka + tloustka * 2;
        }

        // ---------- pomocne barvy ----------

        public static Color Zesvetli(Color barva, float kolik)
        {
            return new Color(
                (int)(barva.R + (255 - barva.R) * kolik),
                (int)(barva.G + (255 - barva.G) * kolik),
                (int)(barva.B + (255 - barva.B) * kolik));
        }

        public static Color Ztmav(Color barva, float kolik)
        {
            return new Color(
                (int)(barva.R * (1 - kolik)),
                (int)(barva.G * (1 - kolik)),
                (int)(barva.B * (1 - kolik)));
        }
    }
}
