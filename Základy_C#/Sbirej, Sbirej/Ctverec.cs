using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ctverce
{
    internal class Ctverec
    {
        // Vlastnosti: co čtverec "má"
        public Vector2 Pozice;
        public int Velikost;
        public Color Barva;

        // Konstruktor: spustí se při new Ctverec(...)
        public Ctverec(Vector2 pozice, int velikost, Color barva)
        {
            Pozice = pozice;
            Velikost = velikost;
            Barva = barva;
        }

        // Obdélník čtverce (hodí se na kolize)
        public Rectangle Obdelnik
        {
            get { return new Rectangle((int)Pozice.X, (int)Pozice.Y, Velikost, Velikost); }
        }

        // Metoda: posunout se
        public void Posun(Vector2 posun)
        {
            Pozice += posun;
        }

        // Metoda: nezůstat mimo okno
        public void Omez(int sirka, int vyska)
        {
            Pozice.X = MathHelper.Clamp(Pozice.X, 0, sirka - Velikost);
            Pozice.Y = MathHelper.Clamp(Pozice.Y, 0, vyska - Velikost);
        }

        // Metoda: dotýká se jiného čtverce?
        public bool Koliduje(Ctverec jiny)
        {
            return Obdelnik.Intersects(jiny.Obdelnik);
        }

        // Metoda: nakreslit se
        public void Nakresli(SpriteBatch spriteBatch, Texture2D pixel)
        {
            spriteBatch.Draw(pixel, Obdelnik, Barva);
        }
    }
}