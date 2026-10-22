using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Zdar_Monogame
{
    // Micek, ktery lita po hraci plose.
    class Micek
    {
        public float X;
        public float Y;

        // Smer je sipka o delce 1 - rika jen KAM micek leti.
        // Jak rychle leti, je ulozene zvlast v Rychlost.
        public float SmerX;
        public float SmerY;
        public float Rychlost = Nastaveni.StartovniRychlost;

        static Random nahoda = new Random();

        public Rectangle Obdelnik
        {
            get
            {
                return new Rectangle((int)X, (int)Y,
                                     Nastaveni.VelikostMicku, Nastaveni.VelikostMicku);
            }
        }

        public float StredX
        {
            get { return X + Nastaveni.VelikostMicku / 2f; }
        }

        // Micek sedi na palce a ceka, az ho hrac odpali.
        public void PolozNaPalku(Palka palka)
        {
            Rectangle tvar = palka.Obdelnik;

            X = tvar.X + tvar.Width / 2f - Nastaveni.VelikostMicku / 2f;
            Y = tvar.Y - Nastaveni.VelikostMicku - 2;

            Rychlost = Nastaveni.StartovniRychlost;
            SmerX = 0;
            SmerY = -1;
        }

        // Odpal sikmo nahoru, pokazde trochu jinam.
        public void Odpal()
        {
            double uhel = (nahoda.NextDouble() - 0.5) * 0.8;   // asi -23 az +23 stupnu
            NastavSmer(uhel);
        }

        // Uhel se pocita od svisle osy smerem nahoru.
        public void NastavSmer(double uhel)
        {
            SmerX = (float)Math.Sin(uhel);
            SmerY = -(float)Math.Cos(uhel);
        }

        public void Zrychli()
        {
            Rychlost += Nastaveni.ZrychleniZaCihlu;
            if (Rychlost > Nastaveni.MaxRychlost) Rychlost = Nastaveni.MaxRychlost;
        }

        public void Vykresli(SpriteBatch davka)
        {
            Rectangle tvar = Obdelnik;

            Kreslic.Obdelnik(davka, tvar, new Color(130, 210, 255));

            // mensi svetlejsi ctverecek uvnitr, aby micek vypadal jako kulicka
            Kreslic.Obdelnik(davka, tvar.X + 3, tvar.Y + 3,
                             tvar.Width - 7, tvar.Height - 7, Color.White);
        }
    }
}
