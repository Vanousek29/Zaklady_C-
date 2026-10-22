using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Zdar_Monogame
{
    // Cela zed cihel nahore.
    class Zed
    {
        public List<Cihla> Cihly = new List<Cihla>();

        // kazdy radek ma svou barvu
        static readonly Color[] BarvyRadku =
        {
            new Color(239, 83, 80),     // cervena
            new Color(255, 152, 0),     // oranzova
            new Color(255, 213, 79),    // zluta
            new Color(102, 205, 120),   // zelena
            new Color(79, 195, 247),    // modra
            new Color(171, 130, 255),   // fialova
        };

        public void Postav()
        {
            Cihly.Clear();

            for (int radek = 0; radek < Nastaveni.Radku; radek++)
            {
                for (int sloupec = 0; sloupec < Nastaveni.Sloupcu; sloupec++)
                {
                    int x = Nastaveni.OkrajZdi
                          + sloupec * (Nastaveni.SirkaCihly + Nastaveni.MezeraCihel);

                    int y = Nastaveni.PrvniRadekY
                          + radek * (Nastaveni.VyskaCihly + Nastaveni.MezeraCihel);

                    Rectangle tvar = new Rectangle(x, y,
                                                   Nastaveni.SirkaCihly,
                                                   Nastaveni.VyskaCihly);

                    Cihly.Add(new Cihla(tvar, BarvyRadku[radek % BarvyRadku.Length]));
                }
            }
        }

        public int PocetZivych()
        {
            int pocet = 0;
            foreach (Cihla cihla in Cihly)
            {
                if (cihla.Ziva) pocet++;
            }
            return pocet;
        }

        public void Vykresli(SpriteBatch davka)
        {
            foreach (Cihla cihla in Cihly)
            {
                cihla.Vykresli(davka);
            }
        }
    }
}
