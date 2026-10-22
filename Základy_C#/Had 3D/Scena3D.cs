using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Had3D
{
    // Stara se o celou 3D scenu: podlahu, zdi, hada, jidlo a svetla.
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

        public Model3DGroup Koren { get; } = new Model3DGroup();

        // Jedna jedina kostka v pameti. Vsechny clanky hada ji sdileji a lisi se
        // jen svym posunem - diky tomu je prekreslovani rychle i pro dlouheho hada.
        MeshGeometry3D kostkaTela;
        MeshGeometry3D kostkaHlavy;
        MeshGeometry3D kostkaJidla;
        MeshGeometry3D plochaStinu;

        Material materialHlavy;
        Material materialTela1;
        Material materialTela2;
        Material materialJidla;
        Material materialBonusu;
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

        // bloky hada - model i jeho posun si drzime, abychom je mohli jen premistovat
        List<GeometryModel3D> segmenty = new List<GeometryModel3D>();
        List<TranslateTransform3D> posuny = new List<TranslateTransform3D>();

        GeometryModel3D jidloModel;
        GeometryModel3D jidloStin;
        TranslateTransform3D jidloPosun = new TranslateTransform3D();
        TranslateTransform3D jidloStinPosun = new TranslateTransform3D();
        AxisAngleRotation3D jidloRotace = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);

        GeometryModel3D bonusModel;
        GeometryModel3D bonusStin;
        TranslateTransform3D bonusPosun = new TranslateTransform3D();
        TranslateTransform3D bonusStinPosun = new TranslateTransform3D();
        AxisAngleRotation3D bonusRotace = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);

        public Scena3D()
        {
            kostkaTela = VytvorKostku(Nastaveni.VelikostBloku, Nastaveni.VelikostBloku);
            kostkaHlavy = VytvorKostku(Nastaveni.VelikostHlavy, Nastaveni.VelikostHlavy);
            kostkaJidla = VytvorKostku(0.60, 0.60);
            plochaStinu = VytvorPlochu(0.78);

            materialHlavy = Lesk(0xC9, 0xFF, 0x6B, 0.55);
            materialTela1 = Lesk(0x3E, 0xD1, 0x55, 0.30);
            materialTela2 = Lesk(0x2E, 0xA8, 0x44, 0.30);
            materialJidla = Lesk(0xFF, 0x53, 0x4F, 0.60);
            materialBonusu = Lesk(0xFF, 0xC4, 0x3D, 0.75);
            materialZdi = Lesk(0x4C, 0x58, 0x72, 0.12);
            materialPrekazky = Lesk(0x74, 0x80, 0x9B, 0.18);
            materialPodlahaA = Matny(0x2B, 0x37, 0x54);
            materialPodlahaB = Matny(0x1C, 0x25, 0x3B);
            materialStinu = Matny(0x11, 0x17, 0x25);

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

            jidloModel = VytvorOtocnyBlok(kostkaJidla, materialJidla, jidloRotace, jidloPosun);
            jidloStin = VytvorStin(jidloStinPosun);
            jidloSkupina.Children.Add(jidloStin);
            jidloSkupina.Children.Add(jidloModel);

            bonusModel = VytvorOtocnyBlok(kostkaJidla, materialBonusu, bonusRotace, bonusPosun);
            bonusStin = VytvorStin(bonusStinPosun);
        }

        // ---------- prepocet souradnic ----------

        // Deska je vycentrovana na pocatek, aby se kolem ni dala otacet kamera.
        // Bunka [0,0] je tedy vlevo vzadu, ne v pocatku.
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
            posuny.Clear();
            bonusSkupina.Children.Clear();
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
            PridejKvadr(zdi, -okrajX - t, -okrajX, 0, v, -okrajZ - t, okrajZ + t);   // vlevo
            PridejKvadr(zdi, okrajX, okrajX + t, 0, v, -okrajZ - t, okrajZ + t);     // vpravo
            PridejKvadr(zdi, -okrajX, okrajX, 0, v, -okrajZ - t, -okrajZ);           // vzadu
            PridejKvadr(zdi, -okrajX, okrajX, 0, v, okrajZ, okrajZ + t);             // vepredu

            // deska pod podlahou, aby hraci plocha vypadala jako masivni kvadr
            PridejKvadr(zdi, -okrajX - t, okrajX + t, -0.45, 0, -okrajZ - t, okrajZ + t);

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
                    PridejKvadr(mesh,
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
        //   cas    ... celkovy cas hry v sekundach (pro otaceni jidla)
        public void Aktualizuj(Hra hra, List<Bod> stary, double postup, double cas)
        {
            ZajistiSegmenty(hra.Had.Count);

            for (int i = 0; i < hra.Had.Count; i++)
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
                double t = postup;
                if (Math.Abs(zX - kX) > 1.5 || Math.Abs(zZ - kZ) > 1.5) t = 1;

                posuny[i].OffsetX = zX + (kX - zX) * t;
                posuny[i].OffsetZ = zZ + (kZ - zZ) * t;

                segmenty[i].Geometry = (i == 0) ? kostkaHlavy : kostkaTela;
                segmenty[i].Material = (i == 0)
                    ? materialHlavy
                    : (i % 2 == 0 ? materialTela1 : materialTela2);
            }

            // jidlo se pomalu otaci a pohupuje nad deskou
            jidloRotace.Angle = cas * 55;
            jidloPosun.OffsetX = StredX(hra.Jidlo.X);
            jidloPosun.OffsetZ = StredZ(hra.Jidlo.Y);
            jidloPosun.OffsetY = 0.26 + Math.Sin(cas * 3.0) * 0.10;
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
                bonusPosun.OffsetY = 0.32 + Math.Sin(cas * 4.5) * 0.14;
                bonusStinPosun.OffsetX = bonusPosun.OffsetX;
                bonusStinPosun.OffsetZ = bonusPosun.OffsetZ;
            }
            else if (bonusSkupina.Children.Count > 0)
            {
                bonusSkupina.Children.Clear();
            }
        }

        // Pripravi tolik bloku, kolik ma had clanku. Bloky se nevyhazuji a nevytvareji
        // porad znovu - jen se pridava, co chybi.
        void ZajistiSegmenty(int pocet)
        {
            while (segmenty.Count < pocet)
            {
                TranslateTransform3D posun = new TranslateTransform3D();
                GeometryModel3D model = new GeometryModel3D(kostkaTela, materialTela1);
                model.Transform = posun;

                segmenty.Add(model);
                posuny.Add(posun);
                hadSkupina.Children.Add(model);
            }

            while (segmenty.Count > pocet)
            {
                int posledni = segmenty.Count - 1;
                hadSkupina.Children.RemoveAt(posledni);
                segmenty.RemoveAt(posledni);
                posuny.RemoveAt(posledni);
            }
        }

        // ---------- pomocne staveni geometrie ----------

        GeometryModel3D VytvorOtocnyBlok(MeshGeometry3D mesh, Material material,
                                         AxisAngleRotation3D rotace, TranslateTransform3D posun)
        {
            // Nejdriv se blok otoci kolem sve osy (stoji v pocatku, takze se
            // otaci na miste) a teprve potom se posune na sve pole.
            Transform3DGroup skupina = new Transform3DGroup();
            skupina.Children.Add(new RotateTransform3D(rotace));
            skupina.Children.Add(posun);

            GeometryModel3D model = new GeometryModel3D(mesh, material);
            model.Transform = skupina;
            return model;
        }

        GeometryModel3D VytvorStin(TranslateTransform3D posun)
        {
            // Falesny stin - WPF stiny neumi, takze pod jidlo polozime tmavy ctverec.
            GeometryModel3D model = new GeometryModel3D(plochaStinu, materialStinu);
            model.Transform = posun;
            return model;
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

        // Kostka stojici na podlaze, vycentrovana na pocatek v ose X a Z.
        static MeshGeometry3D VytvorKostku(double sirka, double vyska)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            double p = sirka / 2;
            PridejKvadr(mesh, -p, p, 0, vyska, -p, p);
            mesh.Freeze();
            return mesh;
        }

        // Kvadr od (x0,y0,z0) do (x1,y1,z1) - sest sten, kazda s vlastni normalou.
        static void PridejKvadr(MeshGeometry3D mesh,
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

            mesh.TriangleIndices.Add(i);
            mesh.TriangleIndices.Add(i + 1);
            mesh.TriangleIndices.Add(i + 2);

            mesh.TriangleIndices.Add(i);
            mesh.TriangleIndices.Add(i + 2);
            mesh.TriangleIndices.Add(i + 3);
        }

        // ---------- materialy ----------

        // Matny material - jen barva, zadny odlesk.
        static Material Matny(byte r, byte g, byte b)
        {
            DiffuseMaterial material = new DiffuseMaterial(
                new SolidColorBrush(Color.FromRgb(r, g, b)));
            material.Freeze();
            return material;
        }

        // Lesklý material - barva plus bily odlesk, aby blok vypadal jako plast.
        static Material Lesk(byte r, byte g, byte b, double sila)
        {
            MaterialGroup skupina = new MaterialGroup();
            skupina.Children.Add(new DiffuseMaterial(
                new SolidColorBrush(Color.FromRgb(r, g, b))));

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
