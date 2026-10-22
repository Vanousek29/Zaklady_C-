namespace Had3D
{
    // Veskera logika hry. Tato trida o 3D vubec nic nevi - jen pocita,
    // co se na desce deje. Vykreslovani resi Scena3D.
    class Hra
    {
        public List<Bod> Had = new List<Bod>();     // Had[0] je hlava, posledni prvek je konec ocasu
        public bool[,] Prekazky;                    // pevne zdi uvnitr pole

        public Bod Jidlo;
        public Bod Bonus;
        public bool BonusAktivni;
        public int BonusZbyva;                      // kolik tahu bonus jeste vydrzi

        public int Skore;
        public int SnedenoJidla;
        public bool Konec;
        public bool Vyhra;                          // had zaplnil cele pole

        Smer smer;
        Queue<Smer> dalsiSmery = new Queue<Smer>();  // fronta stisknutych smeru (max 2)
        Random rnd = new Random();
        Konfigurace konfigurace;

        public Hra(Konfigurace konfigurace)
        {
            this.konfigurace = konfigurace;
            Prekazky = new bool[Nastaveni.SirkaPole, Nastaveni.VyskaPole];

            if (konfigurace.Uroven == Obtiznost.Tezka)
            {
                PostavPrekazky();
            }

            // had zacina uprostred vlevo a leze doprava
            smer = Smer.Vpravo;
            int stred = Nastaveni.VyskaPole / 2;
            Had.Add(new Bod(6, stred));
            Had.Add(new Bod(5, stred));
            Had.Add(new Bod(4, stred));

            VytvorJidlo();
        }

        // Ctyri kratke zdi uvnitr pole, symetricky rozmistene.
        void PostavPrekazky()
        {
            int vlevo = 6;
            int vpravo = Nastaveni.SirkaPole - 7;

            for (int i = 0; i < 4; i++)
            {
                Prekazky[vlevo, 5 + i] = true;
                Prekazky[vpravo, 5 + i] = true;
                Prekazky[vlevo, Nastaveni.VyskaPole - 6 - i] = true;
                Prekazky[vpravo, Nastaveni.VyskaPole - 6 - i] = true;
            }
        }

        // Hrac stisknul klavesu. Smer si ulozime do fronty, aby se nezratil
        // ani rychly dvojity tah (napr. doleva a hned nahoru).
        public void ZmenSmer(Smer novy)
        {
            if (dalsiSmery.Count >= 2) return;

            // novy smer porovnavame proti poslednimu, ktery uz je ve fronte
            Smer predchozi = smer;
            foreach (Smer s in dalsiSmery)
            {
                predchozi = s;
            }

            if (novy == predchozi) return;
            if (JeOpacny(novy, predchozi)) return;   // otocka o 180 stupnu neni dovolena

            dalsiSmery.Enqueue(novy);
        }

        static bool JeOpacny(Smer a, Smer b)
        {
            if (a == Smer.Nahoru && b == Smer.Dolu) return true;
            if (a == Smer.Dolu && b == Smer.Nahoru) return true;
            if (a == Smer.Vlevo && b == Smer.Vpravo) return true;
            if (a == Smer.Vpravo && b == Smer.Vlevo) return true;
            return false;
        }

        // Jeden tah hry. Vraci true, kdyz had neco snedl.
        public bool Tik()
        {
            if (Konec) return false;

            if (dalsiSmery.Count > 0)
            {
                smer = dalsiSmery.Dequeue();
            }

            // spocitame, kam se hlava posune
            Bod nova = Had[0];
            switch (smer)
            {
                case Smer.Nahoru: nova.Y--; break;
                case Smer.Dolu: nova.Y++; break;
                case Smer.Vlevo: nova.X--; break;
                case Smer.Vpravo: nova.X++; break;
            }

            // naraz do okraje desky (nebo proleznuti na druhou stranu)
            if (konfigurace.ProchazetZdi)
            {
                if (nova.X < 0) nova.X = Nastaveni.SirkaPole - 1;
                if (nova.X >= Nastaveni.SirkaPole) nova.X = 0;
                if (nova.Y < 0) nova.Y = Nastaveni.VyskaPole - 1;
                if (nova.Y >= Nastaveni.VyskaPole) nova.Y = 0;
            }
            else if (nova.X < 0 || nova.X >= Nastaveni.SirkaPole ||
                     nova.Y < 0 || nova.Y >= Nastaveni.VyskaPole)
            {
                Konec = true;
                return false;
            }

            if (Prekazky[nova.X, nova.Y])
            {
                Konec = true;
                return false;
            }

            // kdyz had roste, ocas se neposune - takze i do neho se da narazit
            bool roste = nova.JeStejny(Jidlo) || (BonusAktivni && nova.JeStejny(Bonus));
            int kontrolovat = roste ? Had.Count : Had.Count - 1;

            for (int i = 0; i < kontrolovat; i++)
            {
                if (Had[i].JeStejny(nova))
                {
                    Konec = true;
                    return false;
                }
            }

            Had.Insert(0, nova);

            bool snedl = false;
            if (nova.JeStejny(Jidlo))
            {
                Skore += Nastaveni.BodyZaJidlo;
                SnedenoJidla++;
                snedl = true;

                if (SnedenoJidla % Nastaveni.BonusKazde == 0 && !BonusAktivni)
                {
                    VytvorBonus();
                }
                VytvorJidlo();
            }
            else if (BonusAktivni && nova.JeStejny(Bonus))
            {
                Skore += Nastaveni.BodyZaBonus;
                BonusAktivni = false;
                snedl = true;
            }

            if (!roste)
            {
                Had.RemoveAt(Had.Count - 1);
            }

            if (BonusAktivni)
            {
                BonusZbyva--;
                if (BonusZbyva <= 0) BonusAktivni = false;
            }

            return snedl;
        }

        // Seznam vsech bunek, kde nic nestoji - z nich pak vybereme misto pro jidlo.
        List<Bod> VolneBunky()
        {
            List<Bod> volne = new List<Bod>();

            for (int y = 0; y < Nastaveni.VyskaPole; y++)
            {
                for (int x = 0; x < Nastaveni.SirkaPole; x++)
                {
                    if (Prekazky[x, y]) continue;

                    Bod bunka = new Bod(x, y);
                    bool obsazeno = false;
                    foreach (Bod cast in Had)
                    {
                        if (cast.JeStejny(bunka))
                        {
                            obsazeno = true;
                            break;
                        }
                    }

                    if (!obsazeno) volne.Add(bunka);
                }
            }

            return volne;
        }

        void VytvorJidlo()
        {
            List<Bod> volne = VolneBunky();

            // na bonusu jidlo lezet nemuze
            if (BonusAktivni)
            {
                for (int i = volne.Count - 1; i >= 0; i--)
                {
                    if (volne[i].JeStejny(Bonus)) volne.RemoveAt(i);
                }
            }

            if (volne.Count == 0)
            {
                Vyhra = true;    // uz neni kam dat jidlo - had zaplnil celou desku
                Konec = true;
                return;
            }

            Jidlo = volne[rnd.Next(volne.Count)];
        }

        void VytvorBonus()
        {
            List<Bod> volne = VolneBunky();

            for (int i = volne.Count - 1; i >= 0; i--)
            {
                if (volne[i].JeStejny(Jidlo)) volne.RemoveAt(i);
            }

            if (volne.Count == 0) return;

            Bonus = volne[rnd.Next(volne.Count)];
            BonusAktivni = true;
            BonusZbyva = Nastaveni.BonusTrvani;
        }

        // Cas mezi tahy v milisekundach - s kazdym jidlem se hra zrychli.
        public int Rychlost()
        {
            int cas = konfigurace.StartovniRychlost() - SnedenoJidla * Nastaveni.ZrychleniZaJidlo;
            if (cas < Nastaveni.MinRychlost) cas = Nastaveni.MinRychlost;
            return cas;
        }

        public int UrovenRychlosti()
        {
            return SnedenoJidla / 5 + 1;
        }
    }
}
