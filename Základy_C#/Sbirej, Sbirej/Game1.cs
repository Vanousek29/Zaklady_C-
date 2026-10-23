using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ctverce
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Texture2D pixel;   // bílý pixel, ze kterého kreslíme všechny čtverce
        private Ctverec hrac;
        private Ctverec mince;
        private Random rnd = new Random();
        private int skore = 0;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 480;
            _graphics.ApplyChanges();

            // Vytvoření objektů ze třídy Ctverec
            hrac = new Ctverec(new Vector2(100, 100), 40, Color.CornflowerBlue);
            mince = new Ctverec(new Vector2(500, 300), 20, Color.Gold);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Texturu 1x1 bílý pixel vyrobíme v kódu, žádné obrázky nejsou potřeba
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
        }

        private void PresunMinci()
        {
            int x = rnd.Next(0, _graphics.PreferredBackBufferWidth - mince.Velikost);
            int y = rnd.Next(0, _graphics.PreferredBackBufferHeight - mince.Velikost);
            mince.Pozice = new Vector2(x, y);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState klavesy = Keyboard.GetState();

            if (klavesy.IsKeyDown(Keys.Escape))
                Exit();

            // Rychlost 300 pixelů za sekundu, násobíme časem od minulého snímku
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float rychlost = 300 * dt;
            Vector2 posun = Vector2.Zero;

            if (klavesy.IsKeyDown(Keys.Left) || klavesy.IsKeyDown(Keys.A)) posun.X -= rychlost;
            if (klavesy.IsKeyDown(Keys.Right) || klavesy.IsKeyDown(Keys.D)) posun.X += rychlost;
            if (klavesy.IsKeyDown(Keys.Up) || klavesy.IsKeyDown(Keys.W)) posun.Y -= rychlost;
            if (klavesy.IsKeyDown(Keys.Down) || klavesy.IsKeyDown(Keys.S)) posun.Y += rychlost;

            hrac.Posun(posun);
            hrac.Omez(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);

            if (hrac.Koliduje(mince))
            {
                skore++;
                PresunMinci();
                Window.Title = "Čtverce - skóre: " + skore;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();
            hrac.Nakresli(_spriteBatch, pixel);
            mince.Nakresli(_spriteBatch, pixel);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}