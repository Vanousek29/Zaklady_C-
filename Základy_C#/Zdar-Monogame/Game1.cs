using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Zdar_Monogame
{
   
    enum Stav { Ceka, Hraje, Konec }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        Palka palka = new Palka();
        Micek micek = new Micek();
        Zed zed = new Zed();

        Stav stav = Stav.Ceka;
        int skore;
        int zivoty;
        bool vyhra;

        KeyboardState minuleKlavesy;
        double cas;   

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = Nastaveni.SirkaOkna;
            _graphics.PreferredBackBufferHeight = Nastaveni.VyskaOkna;
        }

        protected override void Initialize()
        {
            NovaHra();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            Kreslic.Priprav(GraphicsDevice);
        }

        

        protected override void Update(GameTime gameTime)
        {
            KeyboardState klavesy = Keyboard.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            cas += dt;

            if (klavesy.IsKeyDown(Keys.Escape)) Exit();

            if (stav == Stav.Konec)
            {
                if (Stisknuto(klavesy, Keys.Enter)) NovaHra();
            }
            else
            {
                palka.Posun(klavesy, dt);

                if (stav == Stav.Ceka)
                {
                    micek.PolozNaPalku(palka);   

                    if (Stisknuto(klavesy, Keys.Space))
                    {
                        micek.Odpal();
                        stav = Stav.Hraje;
                    }
                }
                else
                {
                    PosunMicek(dt);
                }
            }

            AktualizujTitulek();
            minuleKlavesy = klavesy;

            base.Update(gameTime);
        }

        
        bool Stisknuto(KeyboardState ted, Keys klavesa)
        {
            return ted.IsKeyDown(klavesa) && minuleKlavesy.IsKeyUp(klavesa);
        }

        void PosunMicek(float dt)
        {
           
            float posunZaSnimek = micek.Rychlost * dt;
            int kroku = (int)(posunZaSnimek / 4f) + 1;
            float castDt = dt / kroku;

            for (int k = 0; k < kroku; k++)
            {
                micek.X += micek.SmerX * micek.Rychlost * castDt;
                micek.Y += micek.SmerY * micek.Rychlost * castDt;

                OdrazOdSten();
                OdrazOdPalky();
                RozbijCihlu();

                if (micek.Y > Nastaveni.VyskaOkna)
                {
                    PrisloOZivot();
                    return;
                }

                if (zed.PocetZivych() == 0)
                {
                    vyhra = true;
                    stav = Stav.Konec;
                    return;
                }
            }
        }

        void OdrazOdSten()
        {
            if (micek.X < 0)
            {
                micek.X = 0;
                micek.SmerX = -micek.SmerX;
            }

            if (micek.X + Nastaveni.VelikostMicku > Nastaveni.SirkaOkna)
            {
                micek.X = Nastaveni.SirkaOkna - Nastaveni.VelikostMicku;
                micek.SmerX = -micek.SmerX;
            }

            if (micek.Y < 0)
            {
                micek.Y = 0;
                micek.SmerY = -micek.SmerY;
            }
        }

        void OdrazOdPalky()
        {
            
            if (micek.SmerY < 0) return;
            if (!micek.Obdelnik.Intersects(palka.Obdelnik)) return;

            Rectangle tvar = palka.Obdelnik;
            float stredPalky = tvar.X + tvar.Width / 2f;

            
            float zasah = (micek.StredX - stredPalky) / (tvar.Width / 2f);
            if (zasah < -1) zasah = -1;
            if (zasah > 1) zasah = 1;

            micek.NastavSmer(zasah * 1.05);   
            micek.Y = tvar.Y - Nastaveni.VelikostMicku - 1;
        }

        void RozbijCihlu()
        {
            foreach (Cihla cihla in zed.Cihly)
            {
                if (!cihla.Ziva) continue;
                if (!micek.Obdelnik.Intersects(cihla.Obdelnik)) continue;

                cihla.Ziva = false;
                skore += Nastaveni.BodyZaCihlu;
                micek.Zrychli();

               
                Rectangle prekryv = Rectangle.Intersect(micek.Obdelnik, cihla.Obdelnik);

                if (prekryv.Width < prekryv.Height) micek.SmerX = -micek.SmerX;
                else micek.SmerY = -micek.SmerY;

                return;   
            }
        }

        void PrisloOZivot()
        {
            zivoty--;

            if (zivoty <= 0)
            {
                vyhra = false;
                stav = Stav.Konec;
                return;
            }

            palka.Vycentruj();
            micek.PolozNaPalku(palka);
            stav = Stav.Ceka;
        }

        void NovaHra()
        {
            skore = 0;
            zivoty = Nastaveni.PocetZivotu;
            vyhra = false;

            zed.Postav();
            palka.Vycentruj();
            micek.PolozNaPalku(palka);

            stav = Stav.Ceka;
        }

        
        void AktualizujTitulek()
        {
            string text = "ARKANOID     skore " + skore + "     zivoty " + zivoty;

            if (stav == Stav.Konec)
            {
                text += vyhra ? "     >>> VYHRAL JSI <<<     Enter = hrat znovu"
                              : "     >>> KONEC HRY <<<     Enter = hrat znovu";
            }
            else if (stav == Stav.Ceka)
            {
                text += "     mezernik = odpal micek,  sipky nebo A/D = palka";
            }

            if (Window.Title != text) Window.Title = text;
        }

        

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(12, 16, 28));

            _spriteBatch.Begin();

            VykresliPozadi();
            zed.Vykresli(_spriteBatch);
            palka.Vykresli(_spriteBatch);

            if (stav != Stav.Konec) micek.Vykresli(_spriteBatch);

            VykresliHud();

            if (stav == Stav.Ceka) VykresliSipku();
            if (stav == Stav.Konec) VykresliKonec();

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        void VykresliPozadi()
        {
            
            Color ram = new Color(30, 38, 58);

            Kreslic.Obdelnik(_spriteBatch, 0, 0, Nastaveni.SirkaOkna, 6, ram);
            Kreslic.Obdelnik(_spriteBatch, 0, 0, 6, Nastaveni.VyskaOkna, ram);
            Kreslic.Obdelnik(_spriteBatch, Nastaveni.SirkaOkna - 6, 0, 6, Nastaveni.VyskaOkna, ram);
        }

        void VykresliHud()
        {
            Kreslic.Cislo(_spriteBatch, skore, 28, 26, 20, 32, 5, new Color(120, 200, 255));

            
            for (int i = 0; i < zivoty; i++)
            {
                Kreslic.Obdelnik(_spriteBatch,
                                 Nastaveni.SirkaOkna - 28 - (i + 1) * 28, 32, 20, 20,
                                 new Color(255, 90, 90));
            }
        }

        
        void VykresliSipku()
        {
            if ((int)(cas * 2) % 2 == 0) return;

            Rectangle tvar = palka.Obdelnik;
            int stred = tvar.X + tvar.Width / 2;
            Color barva = Color.White * 0.55f;

            for (int i = 0; i < 5; i++)
            {
                int sirka = 8 + i * 8;
                Kreslic.Obdelnik(_spriteBatch, stred - sirka / 2,
                                 tvar.Y - 66 + i * 7, sirka, 6, barva);
            }
        }

        void VykresliKonec()
        {
            
            Kreslic.Obdelnik(_spriteBatch, 0, 0, Nastaveni.SirkaOkna, Nastaveni.VyskaOkna,
                             Color.Black * 0.78f);

            Color barva = vyhra ? new Color(90, 230, 130) : new Color(255, 90, 90);

            Kreslic.Obdelnik(_spriteBatch, 0, 244, Nastaveni.SirkaOkna, 8, barva);
            Kreslic.Obdelnik(_spriteBatch, 0, 400, Nastaveni.SirkaOkna, 8, barva);

            
            int sirkaCislice = 44;
            int tloustka = 10;
            int sirkaCelkem = Kreslic.SirkaCisla(skore, sirkaCislice, tloustka);

            Kreslic.Cislo(_spriteBatch, skore,
                          (Nastaveni.SirkaOkna - sirkaCelkem) / 2, 290,
                          sirkaCislice, 72, tloustka, barva);
        }
    }
}
