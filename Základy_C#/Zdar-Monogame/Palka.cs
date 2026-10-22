using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Zdar_Monogame
{
    // Palka dole, kterou hrac posouva do stran.
    class Palka
    {
        // Pozici si drzime jako desetinne cislo. Kdybychom pocitali rovnou
        // v celych pixelech, pomaly pohyb by se zaokrouhloval na nulu
        // a palka by se vubec nehnula.
        float x;

        public Rectangle Obdelnik
        {
            get
            {
                return new Rectangle((int)x, Nastaveni.PalkaY,
                                     Nastaveni.SirkaPalky, Nastaveni.VyskaPalky);
            }
        }

        public void Vycentruj()
        {
            x = (Nastaveni.SirkaOkna - Nastaveni.SirkaPalky) / 2f;
        }

        public void Posun(KeyboardState klavesy, float dt)
        {
            float posun = Nastaveni.RychlostPalky * dt;

            if (klavesy.IsKeyDown(Keys.Left) || klavesy.IsKeyDown(Keys.A)) x -= posun;
            if (klavesy.IsKeyDown(Keys.Right) || klavesy.IsKeyDown(Keys.D)) x += posun;

            // palka nesmi vyjet z okna
            if (x < 0) x = 0;
            if (x + Nastaveni.SirkaPalky > Nastaveni.SirkaOkna)
            {
                x = Nastaveni.SirkaOkna - Nastaveni.SirkaPalky;
            }
        }

        public void Vykresli(SpriteBatch davka)
        {
            Rectangle tvar = Obdelnik;
            Color zaklad = new Color(225, 235, 250);

            Kreslic.Obdelnik(davka, tvar, zaklad);

            // svetly prouzek nahore a tmavy dole - palka pak nevypada placata
            Kreslic.Obdelnik(davka, tvar.X, tvar.Y, tvar.Width, 4,
                             Kreslic.Zesvetli(zaklad, 0.6f));
            Kreslic.Obdelnik(davka, tvar.X, tvar.Bottom - 4, tvar.Width, 4,
                             Kreslic.Ztmav(zaklad, 0.35f));
        }
    }
}
