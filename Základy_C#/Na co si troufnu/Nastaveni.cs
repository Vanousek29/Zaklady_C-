namespace Had3D
{
    // Vsechna pevna nastaveni hry na jednom miste.
    static class Nastaveni
    {
        // --- hraci deska ---
        public const int SirkaPole = 20;        // pocet bunek na sirku
        public const int VyskaPole = 20;        // pocet bunek do hloubky

        // --- pravidla ---
        public const int MinRychlost = 60;      // nejkratsi mozny cas mezi tahy (ms)
        public const int ZrychleniZaJidlo = 3;  // o kolik ms se hra zrychli po kazdem jidle

        public const int BonusKazde = 5;        // kazde pate jidlo privola bonus
        public const int BonusTrvani = 50;      // jak dlouho bonus vydrzi (v tazich)

        public const int BodyZaJidlo = 10;
        public const int BodyZaBonus = 50;

        // --- rozmery v 3D scene ---
        // Jedna bunka desky je 1 x 1. Clanek hada je napric uzsi nez policko,
        // aby mel misto vybocit do stran pri vlneni. Po smeru lezeni je naopak
        // dlouhy pres cele policko, takze clanky na sebe navazuji a had vypada
        // jako souvisle telo, ne jako rozsypane kostky.
        public const double SirkaClanku = 0.72;    // napric telem
        public const double VyskaClanku = 0.66;
        public const double DelkaClanku = 1.02;    // po smeru lezeni

        public const double SirkaHlavy = 0.82;
        public const double VyskaHlavy = 0.76;
        public const double DelkaHlavy = 1.06;
        public const double VyskaZdi = 0.8;
        public const double TloustkaZdi = 0.7;
    }

    enum Obtiznost { Snadna, Normalni, Tezka }

    // Nastaveni, ktere si hrac prepina v menu.
    class Konfigurace
    {
        public Obtiznost Uroven = Obtiznost.Normalni;
        public bool ProchazetZdi = false;

        // Jak dlouho se ceka mezi tahy na zacatku hry.
        public int StartovniRychlost()
        {
            switch (Uroven)
            {
                case Obtiznost.Snadna: return 190;
                case Obtiznost.Tezka: return 110;
                default: return 150;
            }
        }

        public string NazevObtiznosti()
        {
            switch (Uroven)
            {
                case Obtiznost.Snadna: return "Snadná";
                case Obtiznost.Tezka: return "Těžká";
                default: return "Normální";
            }
        }
    }
}
