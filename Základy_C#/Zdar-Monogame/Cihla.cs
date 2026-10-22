using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Zdar_Monogame
{
    // Jedna cihla ve zdi.
    class Cihla
    {
        public Rectangle Obdelnik;
        public Color Barva;
        public bool Ziva = true;

        public Cihla(Rectangle obdelnik, Color barva)
        {
            Obdelnik = obdelnik;
            Barva = barva;
        }

        public void Vykresli(SpriteBatch davka)
        {
            if (!Ziva) return;

            Kreslic.Obdelnik(davka, Obdelnik, Barva);

            // svetly prouzek nahore a tmavy dole -> cihla vypada vypukle
            Kreslic.Obdelnik(davka, Obdelnik.X, Obdelnik.Y, Obdelnik.Width, 4,
                             Kreslic.Zesvetli(Barva, 0.45f));
            Kreslic.Obdelnik(davka, Obdelnik.X, Obdelnik.Bottom - 4, Obdelnik.Width, 4,
                             Kreslic.Ztmav(Barva, 0.35f));
        }
    }
}
