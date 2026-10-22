using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Had3D
{
    public partial class MainWindow : Window
    {
        // V jakem stavu hra prave je
        enum Stav { Menu, Hraje, Pauza, Konec }

        Konfigurace konfigurace = new Konfigurace();
        Scena3D scena = new Scena3D();
        Hra hra;
        List<Bod> staryHad = new List<Bod>();    // kde byl had pred poslednim tahem
        Stav stav = Stav.Menu;

        Stopwatch hodiny = Stopwatch.StartNew();
        double posledniSnimek;      // cas predchoziho snimku v ms
        double casOdTahu;           // kolik ms uplynulo od posledniho tahu hada
        double celkovyCas;          // celkovy cas v sekundach (pro otaceni jidla)

        int nejlepsi;
        int rekordPredHrou;

        // Kamera obiha kolem stredu desky: uhel je azimut, elevace je vyska nad deskou.
        double uhel = 0;
        double elevace = 44;
        double vzdalenost = 34;

        bool tahneMysi;
        Point predchoziMys;

        public MainWindow()
        {
            InitializeComponent();

            nejlepsi = Skore.Nacti();
            scenaVisual.Content = scena.Koren;

            hra = new Hra(konfigurace);
            NastavKameru();
            ZobrazMenu();

            // kdyz hrac prepne na jine okno, hra se sama zastavi
            Deactivated += Okno_Deaktivovano;

            // Tato udalost se spusti pred kazdym prekreslenim okna (asi 60x za sekundu).
            // Tady je cela hlavni smycka hry.
            CompositionTarget.Rendering += Snimek;
        }

        // ===================== hlavni smycka =====================

        void Snimek(object? sender, EventArgs e)
        {
            double nyni = hodiny.Elapsed.TotalMilliseconds;
            double rozdil = nyni - posledniSnimek;
            posledniSnimek = nyni;

            // kdyz bylo okno chvili minimalizovane, neudelame najednou deset tahu
            if (rozdil < 0) rozdil = 0;
            if (rozdil > 200) rozdil = 200;

            celkovyCas += rozdil / 1000.0;

            // v menu se kamera sama pomalu otaci, aby bylo na co koukat
            if (stav == Stav.Menu)
            {
                uhel += rozdil * 0.004;
                NastavKameru();
            }

            if (stav == Stav.Hraje)
            {
                casOdTahu += rozdil;
                int rychlost = hra.Rychlost();

                if (casOdTahu >= rychlost)
                {
                    casOdTahu -= rychlost;
                    if (casOdTahu >= rychlost) casOdTahu = 0;

                    staryHad = new List<Bod>(hra.Had);   // kde byl had pred tahem
                    hra.Tik();

                    if (hra.Skore > nejlepsi) nejlepsi = hra.Skore;
                    if (hra.Konec) UkonciHru();
                }
            }

            // Jak daleko je had mezi starym a novym polem (0 az 1). Diky tomu
            // bloky plynule kloužou a hra necuka po polickach.
            double postup = 1;
            if (stav == Stav.Hraje)
            {
                postup = casOdTahu / hra.Rychlost();
                if (postup > 1) postup = 1;
            }

            scena.Aktualizuj(hra, staryHad, postup, celkovyCas);
            AktualizujHud();
        }

        void AktualizujHud()
        {
            textSkore.Text = hra.Skore.ToString();
            textDelka.Text = hra.Had.Count.ToString();
            textUroven.Text = hra.UrovenRychlosti().ToString();
            textRekord.Text = nejlepsi.ToString();

            if (hra.BonusAktivni && stav != Stav.Menu)
            {
                panelBonusu.Visibility = Visibility.Visible;
                pruhBonusu.Width = 150.0 * hra.BonusZbyva / Nastaveni.BonusTrvani;
            }
            else
            {
                panelBonusu.Visibility = Visibility.Collapsed;
            }
        }

        // ===================== stavy hry =====================

        void PripravNovouHru()
        {
            hra = new Hra(konfigurace);
            scena.NovaHra(hra);
            staryHad = new List<Bod>(hra.Had);
            casOdTahu = 0;
        }

        void ZobrazMenu()
        {
            stav = Stav.Menu;
            PripravNovouHru();
            AktualizujMenu();

            panelMenu.Visibility = Visibility.Visible;
            panelPauza.Visibility = Visibility.Collapsed;
            panelKonec.Visibility = Visibility.Collapsed;
            panelBonusu.Visibility = Visibility.Collapsed;
            hud.Visibility = Visibility.Collapsed;
            panelNapovedy.Visibility = Visibility.Collapsed;
        }

        void ZacniHru()
        {
            PripravNovouHru();
            rekordPredHrou = nejlepsi;
            stav = Stav.Hraje;

            panelMenu.Visibility = Visibility.Collapsed;
            panelPauza.Visibility = Visibility.Collapsed;
            panelKonec.Visibility = Visibility.Collapsed;
            hud.Visibility = Visibility.Visible;
            panelNapovedy.Visibility = Visibility.Visible;
        }

        void UkonciHru()
        {
            stav = Stav.Konec;
            Skore.Uloz(nejlepsi);

            if (hra.Vyhra)
            {
                textKonecNadpis.Text = "VYHRÁL JSI!";
                textKonecNadpis.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xC4, 0x3D));
                textKonecPodtitul.Text = "zaplnil jsi celou desku";
            }
            else
            {
                textKonecNadpis.Text = "KONEC HRY";
                textKonecNadpis.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B));
                textKonecPodtitul.Text = "had narazil";
            }

            textKonecSkore.Text = hra.Skore.ToString();
            textKonecDelka.Text = hra.Had.Count.ToString();
            textKonecRekord.Text = nejlepsi.ToString();

            textNovyRekord.Visibility = (hra.Skore > rekordPredHrou && hra.Skore > 0)
                ? Visibility.Visible
                : Visibility.Collapsed;

            panelKonec.Visibility = Visibility.Visible;
        }

        void PrepniPauzu()
        {
            if (stav == Stav.Hraje)
            {
                stav = Stav.Pauza;
                panelPauza.Visibility = Visibility.Visible;
            }
            else if (stav == Stav.Pauza)
            {
                stav = Stav.Hraje;
                panelPauza.Visibility = Visibility.Collapsed;
            }
        }

        void AktualizujMenu()
        {
            tlacitkoObtiznost.Content = "Obtížnost:     " + konfigurace.NazevObtiznosti();
            tlacitkoZdi.Content = "Průchod zdí:     " + (konfigurace.ProchazetZdi ? "ano" : "ne");
        }

        // ===================== kamera =====================

        // Z uhlu a vzdalenosti spocitame, kde kamera stoji. Vzdycky kouka do stredu desky.
        void NastavKameru()
        {
            double a = uhel * Math.PI / 180.0;
            double e = elevace * Math.PI / 180.0;

            double x = vzdalenost * Math.Cos(e) * Math.Sin(a);
            double y = vzdalenost * Math.Sin(e);
            double z = vzdalenost * Math.Cos(e) * Math.Cos(a);

            kamera.Position = new Point3D(x, y, z);
            kamera.LookDirection = new Vector3D(-x, -y, -z);
            kamera.UpDirection = new Vector3D(0, 1, 0);
        }

        void Priblizeni(double o)
        {
            vzdalenost += o;
            if (vzdalenost < 13) vzdalenost = 13;
            if (vzdalenost > 55) vzdalenost = 55;
            NastavKameru();
        }

        // ===================== ovladani =====================

        void Okno_KlavesaDolu(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                // pohyb hada - podle toho, kam se prave kouka kamera
                case Key.Up:
                case Key.W:
                    Zatoc(KameraVpred());
                    e.Handled = true;
                    break;

                case Key.Down:
                case Key.S:
                    Zatoc(-KameraVpred());
                    e.Handled = true;
                    break;

                case Key.Left:
                case Key.A:
                    Zatoc(-KameraVpravo());
                    e.Handled = true;
                    break;

                case Key.Right:
                case Key.D:
                    Zatoc(KameraVpravo());
                    e.Handled = true;
                    break;

                // ovladani hry
                case Key.Enter:
                case Key.Space:
                    if (stav == Stav.Menu || stav == Stav.Konec) ZacniHru();
                    else if (stav == Stav.Pauza) PrepniPauzu();
                    e.Handled = true;
                    break;

                case Key.P:
                    PrepniPauzu();
                    break;

                case Key.Escape:
                    if (stav != Stav.Menu) ZobrazMenu();
                    break;

                // kamera
                case Key.Q: uhel -= 6; NastavKameru(); break;
                case Key.E: uhel += 6; NastavKameru(); break;

                case Key.R:
                    elevace = Math.Min(85, elevace + 4);
                    NastavKameru();
                    break;

                case Key.F:
                    elevace = Math.Max(14, elevace - 4);
                    NastavKameru();
                    break;

                case Key.Add:
                case Key.OemPlus:
                    Priblizeni(-2);
                    break;

                case Key.Subtract:
                case Key.OemMinus:
                    Priblizeni(2);
                    break;
            }
        }

        void Zatoc(Vector3D kam)
        {
            if (stav != Stav.Hraje) return;
            hra.ZmenSmer(NejblizsiSmer(kam));
        }

        // Vybere ten tah po desce, ktery nejvic odpovida pozadovanemu smeru ve scene.
        // Diky tomu ovladani funguje i po otoceni kamery - "doprava" je porad doprava
        // po obrazovce, ne doprava po desce.
        static Smer NejblizsiSmer(Vector3D kam)
        {
            Smer[] moznosti = { Smer.Nahoru, Smer.Dolu, Smer.Vlevo, Smer.Vpravo };

            Smer nejlepsiSmer = Smer.Nahoru;
            double nejlepsiShoda = double.MinValue;

            foreach (Smer s in moznosti)
            {
                // skalarni soucin rekne, jak moc dva smery miri na stejnou stranu
                double shoda = Vector3D.DotProduct(SvetovySmer(s), kam);
                if (shoda > nejlepsiShoda)
                {
                    nejlepsiShoda = shoda;
                    nejlepsiSmer = s;
                }
            }

            return nejlepsiSmer;
        }

        // Kterym smerem ve 3D scene vede dany tah po desce.
        static Vector3D SvetovySmer(Smer s)
        {
            switch (s)
            {
                case Smer.Nahoru: return new Vector3D(0, 0, -1);   // mensi radek = dal od divaka
                case Smer.Dolu: return new Vector3D(0, 0, 1);
                case Smer.Vlevo: return new Vector3D(-1, 0, 0);
                default: return new Vector3D(1, 0, 0);
            }
        }

        // Kam kamera kouka (srazeno na desku) a co je od ni doprava.
        Vector3D KameraVpred()
        {
            double a = uhel * Math.PI / 180.0;
            return new Vector3D(-Math.Sin(a), 0, -Math.Cos(a));
        }

        Vector3D KameraVpravo()
        {
            double a = uhel * Math.PI / 180.0;
            return new Vector3D(Math.Cos(a), 0, -Math.Sin(a));
        }

        // ===================== mys =====================

        void Okno_MysDolu(object sender, MouseButtonEventArgs e)
        {
            tahneMysi = true;
            predchoziMys = e.GetPosition(this);
            Mouse.Capture(this);
        }

        void Okno_MysPohyb(object sender, MouseEventArgs e)
        {
            if (!tahneMysi) return;

            Point nyni = e.GetPosition(this);
            uhel += (nyni.X - predchoziMys.X) * 0.35;
            elevace -= (nyni.Y - predchoziMys.Y) * 0.25;

            if (elevace < 14) elevace = 14;
            if (elevace > 85) elevace = 85;

            predchoziMys = nyni;
            NastavKameru();
        }

        void Okno_MysNahoru(object sender, MouseButtonEventArgs e)
        {
            tahneMysi = false;
            Mouse.Capture(null);
        }

        void Okno_Kolecko(object sender, MouseWheelEventArgs e)
        {
            Priblizeni(-e.Delta / 120.0 * 1.8);
        }

        void Okno_Deaktivovano(object? sender, EventArgs e)
        {
            if (stav == Stav.Hraje) PrepniPauzu();
        }

        // ===================== tlacitka =====================

        void TlacitkoHrat_Klik(object sender, RoutedEventArgs e)
        {
            ZacniHru();
        }

        void TlacitkoObtiznost_Klik(object sender, RoutedEventArgs e)
        {
            // obtiznost se prepina dokolecka
            if (konfigurace.Uroven == Obtiznost.Snadna) konfigurace.Uroven = Obtiznost.Normalni;
            else if (konfigurace.Uroven == Obtiznost.Normalni) konfigurace.Uroven = Obtiznost.Tezka;
            else konfigurace.Uroven = Obtiznost.Snadna;

            AktualizujMenu();
            PripravNovouHru();   // aby se hned videly prekazky
        }

        void TlacitkoZdi_Klik(object sender, RoutedEventArgs e)
        {
            konfigurace.ProchazetZdi = !konfigurace.ProchazetZdi;
            AktualizujMenu();
        }

        void TlacitkoDoMenu_Klik(object sender, RoutedEventArgs e)
        {
            ZobrazMenu();
        }

        void TlacitkoKonec_Klik(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
