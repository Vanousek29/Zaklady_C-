using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Had3D
{
    // Stara se o celou 3D scenu: podlahu, zdi, hada, jidlo, portaly a svetla.
    //
    // Jak to funguje:
    //   V 3D se vsechno sklada z trojuhelniku. Jedna stena kvadru je ctverec,
    //   ktery rozdelime na dva trojuhelniky. Kvadr ma 6 sten, takze 12 trojuhelniku.
    //   Ke kazdemu vrcholu navic ulozime "normalu" - sipku, kam stena kouka.
    //   Podle ni pak WPF pocita, jak moc na stenu dopada svetlo. Proto ma horni
    //   plocha bloku jinou barvu nez bok, i kdyz jsou ze stejneho materialu.
    class Scena3D
    {
        // Vysky jednotlivych vrstev nad deskou. Nesmi byt stejne, jinak se
        // plochy vzajemne prekryvaji a blikaji.
        const double VyskaPodlahy = 0.02;
        const double VyskaStinu = 0.05;

        // --- vlneni hada ---
        // Had se neposouva jako tuha tyc. Kazdy clanek navic vybocuje do stran
        // podle sinusovky, ktera bezi od hlavy k ocasu - diky tomu to vypada,
        // ze se plazi. Vybnuti je male, aby clanek zustal na svem policku.
        const double AmplitudaVlny = 0.09;    // jak daleko do stran clanek vybocí
        const double HustotaVlny = 0.85;      // kolik vln je videt na delce hada
        const double RychlostVlny = 5.5;      // jak rychle vlna bezi telem
        const double AmplitudaZdvihu = 0.05;  // jak moc se clanek zvedá a klesá
        const double ZuzeniOcasu = 0.38;      // o kolik je ocas tencí nez hlava

        // --- portal ---
        const double TrvaniPortalu = 0.8;     // jak dlouho je portal videt (v sekundach)

        // O kolik portal odsadime od zdi smerem do desky. Kdyby stal presne
        // v rovine zdi, pralo by se o kazdy bod, ktera z obou ploch je vepredu,
        // a portal by nebyl videt.
        const double OdsazeniPortalu = 0.12;

        public Model3DGroup Koren { get; } = new Model3DGroup();

        // Jedna jedina kostka v pameti. Vsechny clanky hada ji sdileji a lisi se
        // jen svymi transformacemi - diky tomu je prekreslovani rychle i pro
        // dlouheho hada.
        MeshGeometry3D kostkaTela;
        MeshGeometry3D kostkaHlavy;
        MeshGeometry3D meshOci;
        MeshGeometry3D kouleJablka;
        MeshGeometry3D meshStopky;
        MeshGeometry3D meshListu;
        MeshGeometry3D plochaStinu;
        MeshGeometry3D plochaPortalu;

        Material materialHlavy;
        Material materialTela1;
        Material materialTela2;
        Material materialOci;
        Material materialJablka;
        Material materialZlatehoJablka;
        Material materialStopky;
        Material materialListu;
        Material materialStinu;
        Material materialZdi;
        Material materialPrekazky;
        Material materialPodlahaA;
        Material materialPodlahaB;

        // scena je rozdelena na skupiny, aby se dalo menit jen to, co je potreba
        Model3DGroup statickeVeci = new Model3DGroup();   // podlaha, zdi, prekazky
        Model3DGroup hadSkupina = new Model3DGroup();
        Model3DGroup jidloSkupina = new Model3DGroup();
        Model3DGroup bonusSkupina = new Model3DGroup();
        Model3DGroup portalySkupina = new Model3DGroup();

        // Bloky hada. Kazdy ma trojici transformaci: zmenseni (ocas je tencí),
        // otoceni (kvuli vlneni) a posun na sve misto.
        List<GeometryModel3D> segmenty = new List<GeometryModel3D>();
        List<ScaleTransform3D> meritka = new List<ScaleTransform3D>();
        List<AxisAngleRotation3D> otoceni = new List<AxisAngleRotation3D>();
        List<TranslateTransform3D> posuny = new List<TranslateTransform3D>();
        List<Transform3DGroup> skupinyTransformaci = new List<Transform3DGroup>();

        GeometryModel3D ociModel;

        Model3DGroup jidloModel;
        GeometryModel3D jidloStin;
        TranslateTransform3D jidloPosun = new TranslateTransform3D();
        TranslateTransform3D jidloStinPosun = new TranslateTransform3D();
        AxisAngleRotation3D jidloRotace = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);

        Model3DGroup bonusModel;
        GeometryModel3D bonusStin;
        TranslateTransform3D bonusPosun = new TranslateTransform3D();
        TranslateTransform3D bonusStinPosun = new TranslateTransform3D();
        AxisAngleRotation3D bonusRotace = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);

        // dva portaly - jeden kde had zmizel, druhy kde se objevil
        GeometryModel3D portalVen;
        GeometryModel3D portalDovnitr;
        TranslateTransform3D portalVenPosun = new TranslateTransform3D();
        TranslateTransform3D portalDovnitrPosun = new TranslateTransform3D();
        AxisAngleRotation3D portalVenRotace = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
        AxisAngleRotation3D portalDovnitrRotace = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
        ScaleTransform3D portalVenMeritko = new ScaleTransform3D(1, 1, 1);
        ScaleTransform3D portalDovnitrMeritko = new ScaleTransform3D(1, 1, 1);
        RadialGradientBrush portalStetec;
        double portalCas;   // odpocet, jak dlouho je portal jeste videt

        public Scena3D()
        {
            kostkaTela = VytvorKvadr(Nastaveni.SirkaClanku,
                                     Nastaveni.VyskaClanku,
                                     Nastaveni.DelkaClanku);

            kostkaHlavy = VytvorKvadr(Nastaveni.SirkaHlavy,
                                      Nastaveni.VyskaHlavy,
                                      Nastaveni.DelkaHlavy);

            meshOci = VytvorOci(Nastaveni.SirkaHlavy,
                                Nastaveni.VyskaHlavy,
                                Nastaveni.DelkaHlavy);

            // jablko = koule (trochu zmacknuta) plus stopka a listek
            kouleJablka = VytvorKouli(0.30, 0.27, 0.30, 22, 14);
            meshStopky = VytvorKvadrMesh(0.055, 0.20, 0.055, 0.20);
            meshListu = VytvorListek();
            plochaStinu = VytvorPlochu(0.78);
            plochaPortalu = VytvorSvislouPlochu(2.1, 2.1);

            // Telo hada neni jednolita barva, ale vzorek supin. Vzorek se kresli
            // jako obrazek, ktery se pak dlazdicove opakuje po kazde stene kostky.
            materialTela1 = LeskStetcem(Supiny(Color.FromRgb(0x1C, 0x6B, 0x2F),
                                               Color.FromRgb(0x46, 0xD8, 0x5E)), 0.30);
            materialTela2 = LeskStetcem(Supiny(Color.FromRgb(0x17, 0x5A, 0x28),
                                               Color.FromRgb(0x38, 0xBA, 0x4E)), 0.30);
            materialHlavy = LeskStetcem(Supiny(Color.FromRgb(0x50, 0xA0, 0x35),
                                               Color.FromRgb(0xC6, 0xFF, 0x70)), 0.55);

            materialOci = Lesk(0x0D, 0x11, 0x17, 0.95);
            materialJablka = LeskStetcem(Jablko(Color.FromRgb(0xFF, 0x6B, 0x5A),
                                                Color.FromRgb(0x9E, 0x15, 0x1C),
                                                Color.FromRgb(0xFF, 0xB0, 0x72)), 0.80);
            materialZlatehoJablka = LeskStetcem(Jablko(Color.FromRgb(0xFF, 0xE2, 0x7A),
                                                       Color.FromRgb(0xC2, 0x7E, 0x0A),
                                                       Color.FromRgb(0xFF, 0xF6, 0xC8)), 0.95);
            materialStopky = Lesk(0x6B, 0x49, 0x2B, 0.20);
            materialListu = Lesk(0x4F, 0xB8, 0x55, 0.45);
            materialZdi = Lesk(0x4C, 0x58, 0x72, 0.12);
            materialPrekazky = Lesk(0x74, 0x80, 0x9B, 0.18);
            materialPodlahaA = Matny(0x2B, 0x37, 0x54);
            materialPodlahaB = Matny(0x1C, 0x25, 0x3B);
            materialStinu = Matny(0x11, 0x17, 0x25);

            portalStetec = VytvorStetecPortalu();

            // Svetla. Hlavni svetlo sviti zeshora zleva, takze horni plochy bloku
            // jsou svetle a prave boky zustavaji ve stinu. Druhe slabe svetlo z druhe
            // strany jen zjemnuje stiny, aby nebyly uplne cerne.
            Koren.Children.Add(new AmbientLight(Color.FromRgb(0x3A, 0x40, 0x4F)));
            Koren.Children.Add(new DirectionalLight(Color.FromRgb(0xFF, 0xFA, 0xEE),
                                                    Smer(-0.45, -1.0, -0.38)));
            Koren.Children.Add(new DirectionalLight(Color.FromRgb(0x2B, 0x36, 0x52),
                                                    Smer(0.65, -0.28, 0.6)));

            Koren.Children.Add(statickeVeci);
            Koren.Children.Add(hadSkupina);
            Koren.Children.Add(jidloSkupina);
            Koren.Children.Add(bonusSkupina);
            Koren.Children.Add(portalySkupina);

            ociModel = new GeometryModel3D(meshOci, materialOci);

            jidloModel = VytvorJablko(materialJablka, jidloRotace, jidloPosun);
            jidloStin = VytvorStin(jidloStinPosun);
            jidloSkupina.Children.Add(jidloStin);
            jidloSkupina.Children.Add(jidloModel);

            bonusModel = VytvorJablko(materialZlatehoJablka, bonusRotace, bonusPosun);
            bonusStin = VytvorStin(bonusStinPosun);

            portalVen = VytvorPortal(portalVenRotace, portalVenPosun, portalVenMeritko);
            portalDovnitr = VytvorPortal(portalDovnitrRotace, portalDovnitrPosun,
                                         portalDovnitrMeritko);
        }

        // ---------- prepocet souradnic ----------

        // Deska je vycentrovana na pocatek, aby se kolem ni dala otacet kamera.
        static double StredX(int x)
        {
            return x - Nastaveni.SirkaPole / 2.0 + 0.5;
        }

        static double StredZ(int y)
        {
            return y - Nastaveni.VyskaPole / 2.0 + 0.5;
        }

        // ---------- staveni scény ----------

        // Zavola se pri kazde nove hre - postavi podlahu, zdi a prekazky.
        public void NovaHra(Hra hra)
        {
            statickeVeci.Children.Clear();
            PostavPodlahu();
            PostavZdi();
            PostavPrekazky(hra);

            // had zacina od zacatku, takze zahodime vsechny jeho stare bloky
            hadSkupina.Children.Clear();
            segmenty.Clear();
            meritka.Clear();
            otoceni.Clear();
            posuny.Clear();
            skupinyTransformaci.Clear();

            bonusSkupina.Children.Clear();
            portalySkupina.Children.Clear();
            portalCas = 0;
        }

        // Sachovnicova podlaha - svetla a tmava pole jsou dve samostatne site
        // trojuhelniku, protoze kazda ma jinou barvu.
        void PostavPodlahu()
        {
            MeshGeometry3D svetla = new MeshGeometry3D();
            MeshGeometry3D tmava = new MeshGeometry3D();

            for (int y = 0; y < Nastaveni.VyskaPole; y++)
            {
                for (int x = 0; x < Nastaveni.SirkaPole; x++)
                {
                    MeshGeometry3D cil = ((x + y) % 2 == 0) ? svetla : tmava;

                    double x0 = StredX(x) - 0.5;
                    double x1 = x0 + 1;
                    double z0 = StredZ(y) - 0.5;
                    double z1 = z0 + 1;

                    // Podlaha lezi o chlup vys nez deska pod ni. Kdyby byly obe
                    // presne ve stejne vysce, pralo by se o kazdy bod, ktera z nich
                    // je vepredu, a podlaha by zmizela.
                    PridejStenu(cil,
                        new Point3D(x0, VyskaPodlahy, z1), new Point3D(x1, VyskaPodlahy, z1),
                        new Point3D(x1, VyskaPodlahy, z0), new Point3D(x0, VyskaPodlahy, z0),
                        new Vector3D(0, 1, 0));
                }
            }

            svetla.Freeze();
            tmava.Freeze();
            statickeVeci.Children.Add(new GeometryModel3D(svetla, materialPodlahaA));
            statickeVeci.Children.Add(new GeometryModel3D(tmava, materialPodlahaB));
        }

        void PostavZdi()
        {
            double okrajX = Nastaveni.SirkaPole / 2.0;
            double okrajZ = Nastaveni.VyskaPole / 2.0;
            double t = Nastaveni.TloustkaZdi;
            double v = Nastaveni.VyskaZdi;

            MeshGeometry3D zdi = new MeshGeometry3D();

            // ctyri zdi dokola
            PridejKvadrDo(zdi, -okrajX - t, -okrajX, 0, v, -okrajZ - t, okrajZ + t);   // vlevo
            PridejKvadrDo(zdi, okrajX, okrajX + t, 0, v, -okrajZ - t, okrajZ + t);     // vpravo
            PridejKvadrDo(zdi, -okrajX, okrajX, 0, v, -okrajZ - t, -okrajZ);           // vzadu
            PridejKvadrDo(zdi, -okrajX, okrajX, 0, v, okrajZ, okrajZ + t);             // vepredu

            // deska pod podlahou, aby hraci plocha vypadala jako masivni kvadr
            PridejKvadrDo(zdi, -okrajX - t, okrajX + t, -0.45, 0, -okrajZ - t, okrajZ + t);

            zdi.Freeze();
            statickeVeci.Children.Add(new GeometryModel3D(zdi, materialZdi));
        }

        void PostavPrekazky(Hra hra)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            bool naslo = false;

            for (int y = 0; y < Nastaveni.VyskaPole; y++)
            {
                for (int x = 0; x < Nastaveni.SirkaPole; x++)
                {
                    if (!hra.Prekazky[x, y]) continue;

                    naslo = true;
                    double p = 0.48;
                    PridejKvadrDo(mesh,
                        StredX(x) - p, StredX(x) + p,
                        0, 1.0,
                        StredZ(y) - p, StredZ(y) + p);
                }
            }

            if (!naslo) return;

            mesh.Freeze();
            statickeVeci.Children.Add(new GeometryModel3D(mesh, materialPrekazky));
        }

        // ---------- prekreslovani ----------

        // Zavola se pri kazdem snimku.
        //   postup ... 0 az 1, jak daleko je had mezi predchozim a novym polem
        //   cas    ... celkovy cas hry v sekundach (pro vlneni a otaceni jidla)
        //   dt     ... jak dlouho trval posledni snimek v sekundach (pro portal)
        public void Aktualizuj(Hra hra, List<Bod> stary, double postup, double cas, double dt)
        {
            ZajistiSegmenty(hra.Had.Count);
            PlazeniHada(hra, stary, postup, cas);
            Pochoutky(hra, cas);
            Portaly(dt);
        }

        void PlazeniHada(Hra hra, List<Bod> stary, double postup, double cas)
        {
            int pocet = hra.Had.Count;

            for (int i = 0; i < pocet; i++)
            {
                Bod cil = hra.Had[i];

                // Clanek i byl pred tahem tam, kde je ted clanek i-1. Takze staci
                // plynule prejet z jeho stare pozice na novou a cely had se posouva.
                Bod odkud = (i < stary.Count) ? stary[i] : cil;

                double zX = StredX(odkud.X);
                double zZ = StredZ(odkud.Y);
                double kX = StredX(cil.X);
                double kZ = StredZ(cil.Y);

                // Kdyz had proleze zdi, skoci z jedne strany desky na druhou.
                // To se animovat nesmi, jinak by blok preletel pres celou desku.
                bool proleznuti = Math.Abs(zX - kX) > 1.5 || Math.Abs(zZ - kZ) > 1.5;
                double t = proleznuti ? 1 : postup;

                double x = zX + (kX - zX) * t;
                double z = zZ + (kZ - zZ) * t;

                // Kterym smerem clanek lezi. Bereme to z tvaru tela, ne z pohybu:
                // kazdy clanek miri k tomu, ktery je pred nim. Kdybychom to brali
                // z pohybu, clanek ktery zrovna stoji (cerstve narosteny ocas nebo
                // cely had na zacatku hry) by se otocil nahodne.
                double smerX, smerZ;

                if (i > 0)
                {
                    smerX = StredX(hra.Had[i - 1].X) - StredX(hra.Had[i].X);
                    smerZ = StredZ(hra.Had[i - 1].Y) - StredZ(hra.Had[i].Y);
                }
                else if (pocet > 1)
                {
                    // hlava miri pryc od prvniho clanku tela
                    smerX = StredX(hra.Had[0].X) - StredX(hra.Had[1].X);
                    smerZ = StredZ(hra.Had[0].Y) - StredZ(hra.Had[1].Y);
                }
                else
                {
                    smerX = 0;
                    smerZ = -1;
                }

                // Kdyz jsou sousedni clanky na opacnych koncich desky (prave se
                // proleza zdi), vyjde nesmyslny smer - vezmeme radeji smer pohybu.
                if (Math.Abs(smerX) > 1.5 || Math.Abs(smerZ) > 1.5)
                {
                    smerX = proleznuti ? 0 : kX - zX;
                    smerZ = proleznuti ? 0 : kZ - zZ;
                }

                double delka = Math.Sqrt(smerX * smerX + smerZ * smerZ);
                if (delka > 0.001)
                {
                    smerX /= delka;
                    smerZ /= delka;
                }
                else
                {
                    smerX = 0;
                    smerZ = -1;
                }

                // Kolmice na smer lezeni - do teto strany clanek vybocuje.
                double kolmoX = smerZ;
                double kolmoZ = -smerX;

                // Sinusovka, ktera bezi od hlavy k ocasu. Hlava se vlni min nez telo,
                // aby had mířil tam, kam ho hrac posila.
                double faze = i * HustotaVlny - cas * RychlostVlny;
                double sila = (i == 0) ? 0.45 : 1.0;
                double vlna = Math.Sin(faze) * sila;

                posuny[i].OffsetX = x + kolmoX * AmplitudaVlny * vlna;
                posuny[i].OffsetZ = z + kolmoZ * AmplitudaVlny * vlna;
                posuny[i].OffsetY = AmplitudaZdvihu * (Math.Sin(faze * 2 + 1.0) * 0.5 + 0.5);

                // Otoceni clanku: zaklad je smer lezeni, k nemu se prida naklon
                // podle toho, jak strme vlna zrovna stoupa.
                double uhelSmeru = Math.Atan2(-smerX, -smerZ);
                double sklon = Math.Atan(AmplitudaVlny * HustotaVlny * Math.Cos(faze) * sila);
                otoceni[i].Angle = (uhelSmeru + sklon) * 180.0 / Math.PI;

                // Ocas je tencí nez hlava. Zuzuje se ale jen napric a na vysku -
                // po delce ne, jinak by mezi clanky u ocasu vznikly mezery.
                double podil = (pocet > 1) ? (double)i / (pocet - 1) : 0;
                double zuzeni = 1.0 - ZuzeniOcasu * podil;
                meritka[i].ScaleX = zuzeni;
                meritka[i].ScaleY = zuzeni;
                meritka[i].ScaleZ = 1;

                segmenty[i].Geometry = (i == 0) ? kostkaHlavy : kostkaTela;
                segmenty[i].Material = (i == 0)
                    ? materialHlavy
                    : (i % 2 == 0 ? materialTela1 : materialTela2);
            }

            // Ocka jedou s hlavou - dostanou uplne stejnou transformaci
            // jako prvni clanek, takze se o ne uz nemusime starat.
            if (pocet > 0)
            {
                ociModel.Transform = skupinyTransformaci[0];
                if (!hadSkupina.Children.Contains(ociModel))
                {
                    hadSkupina.Children.Add(ociModel);
                }
            }
        }

        void Pochoutky(Hra hra, double cas)
        {
            // jidlo se pomalu otaci a pohupuje nad deskou
            jidloRotace.Angle = cas * 55;
            jidloPosun.OffsetX = StredX(hra.Jidlo.X);
            jidloPosun.OffsetZ = StredZ(hra.Jidlo.Y);
            // Koule ma stred uprostred sebe, takze posun musi byt aspon o polomer
            // vys, jinak by spodek jablka zajel pod podlahu.
            jidloPosun.OffsetY = 0.40 + Math.Sin(cas * 3.0) * 0.08;
            jidloStinPosun.OffsetX = jidloPosun.OffsetX;
            jidloStinPosun.OffsetZ = jidloPosun.OffsetZ;

            // bonus je ve scene jen kdyz zrovna existuje
            if (hra.BonusAktivni)
            {
                if (bonusSkupina.Children.Count == 0)
                {
                    bonusSkupina.Children.Add(bonusStin);
                    bonusSkupina.Children.Add(bonusModel);
                }

                bonusRotace.Angle = -cas * 100;
                bonusPosun.OffsetX = StredX(hra.Bonus.X);
                bonusPosun.OffsetZ = StredZ(hra.Bonus.Y);
                bonusPosun.OffsetY = 0.46 + Math.Sin(cas * 4.5) * 0.10;
                bonusStinPosun.OffsetX = bonusPosun.OffsetX;
                bonusStinPosun.OffsetZ = bonusPosun.OffsetZ;
            }
            else if (bonusSkupina.Children.Count > 0)
            {
                bonusSkupina.Children.Clear();
            }
        }

        // ---------- portal ----------

        // Zavola se, kdyz had proleze zdi. Postavi dva zarici kruhy - jeden tam,
        // kde had zmizel, druhy tam, kde se zase objevil.
        public void SpustPortal(Bod odkud, Bod kam)
        {
            double okrajX = Nastaveni.SirkaPole / 2.0;
            double okrajZ = Nastaveni.VyskaPole / 2.0;

            if (Math.Abs(odkud.X - kam.X) > 1)
            {
                // Had proletel levou nebo pravou zdi. Kdyz lezl doprava, zmizel
                // u prave zdi (vetsi X) a objevil se u leve.
                double strana = (odkud.X > kam.X) ? 1 : -1;
                double x = okrajX - OdsazeniPortalu;

                portalVenPosun.OffsetX = x * strana;
                portalVenPosun.OffsetZ = StredZ(odkud.Y);
                portalVenRotace.Angle = 90;

                portalDovnitrPosun.OffsetX = -x * strana;
                portalDovnitrPosun.OffsetZ = StredZ(kam.Y);
                portalDovnitrRotace.Angle = 90;
            }
            else
            {
                // had proletel predni nebo zadni zdi
                double strana = (odkud.Y > kam.Y) ? 1 : -1;
                double z = okrajZ - OdsazeniPortalu;

                portalVenPosun.OffsetX = StredX(odkud.X);
                portalVenPosun.OffsetZ = z * strana;
                portalVenRotace.Angle = 0;

                portalDovnitrPosun.OffsetX = StredX(kam.X);
                portalDovnitrPosun.OffsetZ = -z * strana;
                portalDovnitrRotace.Angle = 0;
            }

            portalySkupina.Children.Clear();
            portalySkupina.Children.Add(portalVen);
            portalySkupina.Children.Add(portalDovnitr);
            portalCas = TrvaniPortalu;
        }

        // Portal se rozevre, vybledne a zmizi.
        void Portaly(double dt)
        {
            if (portalCas <= 0) return;

            portalCas -= dt;

            if (portalCas <= 0)
            {
                portalySkupina.Children.Clear();
                portalCas = 0;
                return;
            }

            double podil = portalCas / TrvaniPortalu;   // 1 na zacatku, 0 na konci

            // Mocnina drzi portal dele jasny a vybledne az ke konci -
            // kdyby slabl rovnomerne, byl by uz v pulce skoro neviditelny.
            portalStetec.Opacity = Math.Pow(podil, 0.55);

            // na zacatku je portal maly a rychle se rozevre
            double meritko = 0.52 + 0.55 * (1 - podil);
            portalVenMeritko.ScaleX = meritko;
            portalVenMeritko.ScaleY = meritko;
            portalDovnitrMeritko.ScaleX = meritko;
            portalDovnitrMeritko.ScaleY = meritko;
        }

        // Pripravi tolik bloku, kolik ma had clanku. Bloky se nevyhazuji a nevytvareji
        // porad znovu - jen se pridava, co chybi.
        void ZajistiSegmenty(int pocet)
        {
            while (segmenty.Count < pocet)
            {
                ScaleTransform3D meritko = new ScaleTransform3D(1, 1, 1);
                AxisAngleRotation3D rotace = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
                TranslateTransform3D posun = new TranslateTransform3D();

                // Poradi je dulezite: nejdriv zmensit, pak otocit a az nakonec
                // posunout na misto. Kdyby se posouvalo driv, otacel by se clanek
                // kolem stredu desky misto kolem sebe.
                Transform3DGroup skupina = new Transform3DGroup();
                skupina.Children.Add(meritko);
                skupina.Children.Add(new RotateTransform3D(rotace));
                skupina.Children.Add(posun);

                GeometryModel3D model = new GeometryModel3D(kostkaTela, materialTela1);
                model.Transform = skupina;

                segmenty.Add(model);
                meritka.Add(meritko);
                otoceni.Add(rotace);
                posuny.Add(posun);
                skupinyTransformaci.Add(skupina);
                hadSkupina.Children.Add(model);
            }

            while (segmenty.Count > pocet)
            {
                int posledni = segmenty.Count - 1;
                hadSkupina.Children.Remove(segmenty[posledni]);
                segmenty.RemoveAt(posledni);
                meritka.RemoveAt(posledni);
                otoceni.RemoveAt(posledni);
                posuny.RemoveAt(posledni);
                skupinyTransformaci.RemoveAt(posledni);
            }
        }

        // ---------- pomocne staveni geometrie ----------

        // Jablko se sklada ze tri casti - koule, stopky a listku. Vsechny tri
        // dame do jedne skupiny a transformaci nastavime az te skupine, takze
        // se otaci a pohupuji spolecne jako jeden kus.
        Model3DGroup VytvorJablko(Material materialTela, AxisAngleRotation3D rotace,
                                  TranslateTransform3D posun)
        {
            Model3DGroup jablko = new Model3DGroup();
            jablko.Children.Add(new GeometryModel3D(kouleJablka, materialTela));
            jablko.Children.Add(new GeometryModel3D(meshStopky, materialStopky));
            jablko.Children.Add(new GeometryModel3D(meshListu, materialListu));

            // Nejdriv se jablko otoci kolem sve osy (stoji v pocatku, takze se
            // otaci na miste) a teprve potom se posune na sve pole.
            Transform3DGroup transformace = new Transform3DGroup();
            transformace.Children.Add(new RotateTransform3D(rotace));
            transformace.Children.Add(posun);
            jablko.Transform = transformace;

            return jablko;
        }

        // Koule slozena z trojuhelniku. Delime ji na poledniky (kolem dokola)
        // a rovnobezky (od polu k polu) - jako sit na globusu. Kazdy ctverecek
        // site pak rozpulime na dva trojuhelniky.
        // Polomery mohou byt ruzne, takze z toho jde udelat i zmacknuta koule.
        static MeshGeometry3D VytvorKouli(double rx, double ry, double rz,
                                          int poledniky, int rovnobezky)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            for (int i = 0; i <= rovnobezky; i++)
            {
                double v = (double)i / rovnobezky;
                double fi = v * Math.PI;              // 0 = severni pol, PI = jizni

                for (int j = 0; j <= poledniky; j++)
                {
                    double u = (double)j / poledniky;
                    double theta = u * 2 * Math.PI;   // kolem dokola

                    double sx = Math.Sin(fi) * Math.Cos(theta);
                    double sy = Math.Cos(fi);
                    double sz = Math.Sin(fi) * Math.Sin(theta);

                    mesh.Positions.Add(new Point3D(sx * rx, sy * ry, sz * rz));

                    // U zmacknute koule normala nemiri ze stredu ven - musi se
                    // vydelit druhou mocninou polomeru, jinak by svetlo dopadalo
                    // spatne a koule by vypadala placata.
                    Vector3D normala = new Vector3D(sx / (rx * rx),
                                                    sy / (ry * ry),
                                                    sz / (rz * rz));
                    normala.Normalize();
                    mesh.Normals.Add(normala);
                    mesh.TextureCoordinates.Add(new Point(u, v));
                }
            }

            for (int i = 0; i < rovnobezky; i++)
            {
                for (int j = 0; j < poledniky; j++)
                {
                    int a = i * (poledniky + 1) + j;   // tenhle bod
                    int b = a + poledniky + 1;         // bod o rovnobezku niz

                    mesh.TriangleIndices.Add(a);
                    mesh.TriangleIndices.Add(a + 1);
                    mesh.TriangleIndices.Add(b);

                    mesh.TriangleIndices.Add(a + 1);
                    mesh.TriangleIndices.Add(b + 1);
                    mesh.TriangleIndices.Add(b);
                }
            }

            mesh.Freeze();
            return mesh;
        }

        // Listek u stopky - placaty kvadr kousek stranou.
        static MeshGeometry3D VytvorListek()
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            PridejKvadrDo(mesh, 0.04, 0.24, 0.30, 0.335, -0.065, 0.065);
            mesh.Freeze();
            return mesh;
        }

        // Kvadr o dane velikosti, ktery zacina v urcite vysce (pro stopku).
        static MeshGeometry3D VytvorKvadrMesh(double sirka, double vyska,
                                              double hloubka, double odVysky)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            PridejKvadrDo(mesh, -sirka / 2, sirka / 2,
                          odVysky, odVysky + vyska,
                          -hloubka / 2, hloubka / 2);
            mesh.Freeze();
            return mesh;
        }

        GeometryModel3D VytvorStin(TranslateTransform3D posun)
        {
            // Falesny stin - WPF stiny neumi, takze pod jidlo polozime tmavy ctverec.
            GeometryModel3D model = new GeometryModel3D(plochaStinu, materialStinu);
            model.Transform = posun;
            return model;
        }

        GeometryModel3D VytvorPortal(AxisAngleRotation3D rotace, TranslateTransform3D posun,
                                     ScaleTransform3D meritko)
        {
            Transform3DGroup skupina = new Transform3DGroup();
            skupina.Children.Add(meritko);
            skupina.Children.Add(new RotateTransform3D(rotace));
            skupina.Children.Add(posun);

            // Zarici material - nezavisi na svetlech, sviti sam o sobe.
            EmissiveMaterial material = new EmissiveMaterial(portalStetec);

            GeometryModel3D model = new GeometryModel3D(plochaPortalu, material);
            model.BackMaterial = material;   // portal je videt z obou stran
            model.Transform = skupina;
            return model;
        }

        // Stetec portalu: uprostred skoro pruhledny, na kraji zarici kruh.
        static RadialGradientBrush VytvorStetecPortalu()
        {
            RadialGradientBrush stetec = new RadialGradientBrush();
            stetec.GradientStops.Add(new GradientStop(Color.FromArgb(0x28, 0x3F, 0xC4, 0xFF), 0.00));
            stetec.GradientStops.Add(new GradientStop(Color.FromArgb(0x6E, 0x5A, 0xDC, 0xFF), 0.44));
            stetec.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x8F, 0xEC, 0xFF), 0.66));
            stetec.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xF2, 0xFE, 0xFF), 0.76));
            stetec.GradientStops.Add(new GradientStop(Color.FromArgb(0xAA, 0x5A, 0xDC, 0xFF), 0.88));
            stetec.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x3F, 0xC4, 0xFF), 1.00));
            return stetec;   // nemrazime - za behu se meni pruhlednost
        }

        // Rovny ctverec lezici na podlaze (kousek nad ni, aby neproblikavala).
        static MeshGeometry3D VytvorPlochu(double velikost)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            double p = velikost / 2;

            PridejStenu(mesh,
                new Point3D(-p, VyskaStinu, p), new Point3D(p, VyskaStinu, p),
                new Point3D(p, VyskaStinu, -p), new Point3D(-p, VyskaStinu, -p),
                new Vector3D(0, 1, 0));

            mesh.Freeze();
            return mesh;
        }

        // Svisly ctverec stojici na podlaze - pouziva se na portal.
        static MeshGeometry3D VytvorSvislouPlochu(double sirka, double vyska)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            double p = sirka / 2;

            PridejStenu(mesh,
                new Point3D(-p, 0, 0), new Point3D(p, 0, 0),
                new Point3D(p, vyska, 0), new Point3D(-p, vyska, 0),
                new Vector3D(0, 0, 1));

            mesh.Freeze();
            return mesh;
        }

        // Kvadr stojici na podlaze, vycentrovany na pocatek v ose X a Z.
        static MeshGeometry3D VytvorKvadr(double sirka, double vyska, double hloubka)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            PridejKvadrDo(mesh, -sirka / 2, sirka / 2, 0, vyska, -hloubka / 2, hloubka / 2);
            mesh.Freeze();
            return mesh;
        }

        // Dve ocka na predku hlavy. Predek hlavy miri na -Z.
        static MeshGeometry3D VytvorOci(double sirka, double vyska, double delka)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            double r = 0.075;          // polovina velikosti ocka
            double y = vyska * 0.70;   // jak vysoko na hlave
            double x = sirka * 0.28;   // jak daleko od stredu
            double z = -delka * 0.46;  // predek hlavy

            PridejKvadrDo(mesh, -x - r, -x + r, y - r, y + r, z - r, z + r);
            PridejKvadrDo(mesh, x - r, x + r, y - r, y + r, z - r, z + r);

            mesh.Freeze();
            return mesh;
        }

        // Kvadr od (x0,y0,z0) do (x1,y1,z1) - sest sten, kazda s vlastni normalou.
        static void PridejKvadrDo(MeshGeometry3D mesh,
                                  double x0, double x1, double y0, double y1, double z0, double z1)
        {
            // horni plocha - na ni dopada nejvic svetla
            PridejStenu(mesh,
                new Point3D(x0, y1, z1), new Point3D(x1, y1, z1),
                new Point3D(x1, y1, z0), new Point3D(x0, y1, z0),
                new Vector3D(0, 1, 0));

            // predni stena (k divakovi)
            PridejStenu(mesh,
                new Point3D(x0, y0, z1), new Point3D(x1, y0, z1),
                new Point3D(x1, y1, z1), new Point3D(x0, y1, z1),
                new Vector3D(0, 0, 1));

            // zadni stena
            PridejStenu(mesh,
                new Point3D(x1, y0, z0), new Point3D(x0, y0, z0),
                new Point3D(x0, y1, z0), new Point3D(x1, y1, z0),
                new Vector3D(0, 0, -1));

            // pravy bok
            PridejStenu(mesh,
                new Point3D(x1, y0, z1), new Point3D(x1, y0, z0),
                new Point3D(x1, y1, z0), new Point3D(x1, y1, z1),
                new Vector3D(1, 0, 0));

            // levy bok
            PridejStenu(mesh,
                new Point3D(x0, y0, z0), new Point3D(x0, y0, z1),
                new Point3D(x0, y1, z1), new Point3D(x0, y1, z0),
                new Vector3D(-1, 0, 0));

            // spodni plocha
            PridejStenu(mesh,
                new Point3D(x0, y0, z0), new Point3D(x1, y0, z0),
                new Point3D(x1, y0, z1), new Point3D(x0, y0, z1),
                new Vector3D(0, -1, 0));
        }

        // Jedna ctvercova stena = dva trojuhelniky (a-b-c a a-c-d).
        // Vrcholy musi jit proti smeru hodinovych rucicek, kdyz se na stenu
        // koukame zvenku - jinak by WPF stenu povazoval za odvracenou a zahodil ji.
        static void PridejStenu(MeshGeometry3D mesh,
                                Point3D a, Point3D b, Point3D c, Point3D d, Vector3D normala)
        {
            int i = mesh.Positions.Count;

            mesh.Positions.Add(a);
            mesh.Positions.Add(b);
            mesh.Positions.Add(c);
            mesh.Positions.Add(d);

            // kazdy vrchol dostane stejnou normalu, takze stena bude plocha a ostra
            for (int j = 0; j < 4; j++) mesh.Normals.Add(normala);

            // Souradnice v obrazku s texturou - rikaji, ktery roh obrazku patri
            // ke kteremu rohu steny. Bez nich by se vzorek supin nemel kam natahnout.
            mesh.TextureCoordinates.Add(new Point(0, 1));
            mesh.TextureCoordinates.Add(new Point(1, 1));
            mesh.TextureCoordinates.Add(new Point(1, 0));
            mesh.TextureCoordinates.Add(new Point(0, 0));

            mesh.TriangleIndices.Add(i);
            mesh.TriangleIndices.Add(i + 1);
            mesh.TriangleIndices.Add(i + 2);

            mesh.TriangleIndices.Add(i);
            mesh.TriangleIndices.Add(i + 2);
            mesh.TriangleIndices.Add(i + 3);
        }

        // ---------- materialy a vzorky ----------

        // Vyrobi vzorek hadich supin. Je to obycejna kresba: podklad a na nem
        // tri rady prekryvajicich se kolecek, kazda druha posunuta o pulku -
        // jako tasky na strese. Stetec se pak dlazdicove opakuje po cele stene.
        static Brush Supiny(Color podklad, Color supina)
        {
            DrawingGroup kresba = new DrawingGroup();

            // podklad = tmava kuze, ktera mezi supinami prosvita
            kresba.Children.Add(new GeometryDrawing(
                new SolidColorBrush(podklad), null,
                new RectangleGeometry(new Rect(0, 0, 1, 1))));

            // Supiny kreslime ve dvou vrstvach: nejdriv cely tvar s tmavym
            // obrysem, pak o kousek vys mensi svetlejsi tvar, ktery dela
            // odlesk na hornim okraji. Diky tomu supina vypada vypukle.
            GeometryGroup telo = new GeometryGroup();
            GeometryGroup odlesk = new GeometryGroup();

            for (int rada = 0; rada < 3; rada++)
            {
                double y = rada * 0.42;
                double posun = (rada % 2 == 0) ? 0.0 : 0.25;

                for (int i = -1; i < 3; i++)
                {
                    double x = i * 0.5 + posun;
                    telo.Children.Add(new EllipseGeometry(new Point(x, y), 0.26, 0.30));
                    odlesk.Children.Add(new EllipseGeometry(new Point(x, y - 0.06), 0.16, 0.17));
                }
            }

            Pen obrys = new Pen(new SolidColorBrush(Ztmav(podklad, 0.5)), 0.05);
            obrys.Freeze();

            kresba.Children.Add(new GeometryDrawing(new SolidColorBrush(supina), obrys, telo));
            kresba.Children.Add(new GeometryDrawing(
                new SolidColorBrush(Zesvetli(supina, 0.30)), null, odlesk));

            DrawingBrush stetec = new DrawingBrush(kresba);
            stetec.Viewbox = new Rect(0, 0, 1, 1);
            stetec.ViewboxUnits = BrushMappingMode.Absolute;
            stetec.Viewport = new Rect(0, 0, 0.5, 0.5);   // dve dlazdice na sirku i na vysku
            stetec.ViewportUnits = BrushMappingMode.RelativeToBoundingBox;
            stetec.TileMode = TileMode.Tile;
            stetec.Freeze();

            return stetec;
        }

        // Vzorek jablka. Na rozdil od supin se neopakuje - natahne se na kouli
        // jen jednou. Svisly prechod dela tmavsi spodek, tenke svisle pruhy
        // pak to zluté zihani, ktere maji jablka na slupce.
        static Brush Jablko(Color svrchu, Color zespodu, Color pruh)
        {
            DrawingGroup kresba = new DrawingGroup();

            LinearGradientBrush prechod = new LinearGradientBrush(svrchu, zespodu, 90);
            kresba.Children.Add(new GeometryDrawing(prechod, null,
                new RectangleGeometry(new Rect(0, 0, 1, 1))));

            GeometryGroup pruhy = new GeometryGroup();
            for (int i = 0; i < 9; i++)
            {
                double x = i / 9.0;
                pruhy.Children.Add(new RectangleGeometry(new Rect(x, 0, 0.022, 1)));
                pruhy.Children.Add(new RectangleGeometry(new Rect(x + 0.045, 0.12, 0.013, 0.76)));
            }

            kresba.Children.Add(new GeometryDrawing(
                new SolidColorBrush(Color.FromArgb(0x4A, pruh.R, pruh.G, pruh.B)), null, pruhy));

            DrawingBrush stetec = new DrawingBrush(kresba);
            stetec.Viewbox = new Rect(0, 0, 1, 1);
            stetec.ViewboxUnits = BrushMappingMode.Absolute;
            stetec.Viewport = new Rect(0, 0, 1, 1);
            stetec.ViewportUnits = BrushMappingMode.RelativeToBoundingBox;
            stetec.TileMode = TileMode.None;   // jedna "dlazdice" na cele jablko
            stetec.Freeze();

            return stetec;
        }

        static Color Ztmav(Color barva, double kolik)
        {
            return Color.FromRgb((byte)(barva.R * (1 - kolik)),
                                 (byte)(barva.G * (1 - kolik)),
                                 (byte)(barva.B * (1 - kolik)));
        }

        static Color Zesvetli(Color barva, double kolik)
        {
            return Color.FromRgb((byte)(barva.R + (255 - barva.R) * kolik),
                                 (byte)(barva.G + (255 - barva.G) * kolik),
                                 (byte)(barva.B + (255 - barva.B) * kolik));
        }

        // Matny material - jen barva, zadny odlesk.
        static Material Matny(byte r, byte g, byte b)
        {
            DiffuseMaterial material = new DiffuseMaterial(
                new SolidColorBrush(Color.FromRgb(r, g, b)));
            material.Freeze();
            return material;
        }

        // Lesklý material z jedne barvy.
        static Material Lesk(byte r, byte g, byte b, double sila)
        {
            return LeskStetcem(new SolidColorBrush(Color.FromRgb(r, g, b)), sila);
        }

        // Lesklý material z libovolneho stetce (treba z vzorku supin) -
        // barva plus bily odlesk, aby blok vypadal jako plast.
        static Material LeskStetcem(Brush stetec, double sila)
        {
            MaterialGroup skupina = new MaterialGroup();
            skupina.Children.Add(new DiffuseMaterial(stetec));

            byte odlesk = (byte)(255 * sila);
            skupina.Children.Add(new SpecularMaterial(
                new SolidColorBrush(Color.FromRgb(odlesk, odlesk, odlesk)), 28));

            skupina.Freeze();
            return skupina;
        }

        static Vector3D Smer(double x, double y, double z)
        {
            Vector3D v = new Vector3D(x, y, z);
            v.Normalize();
            return v;
        }
    }
}
