namespace Zdar_Monogame
{
    // Vsechna pevna cisla hry na jednom miste, aby se daly snadno menit.
    static class Nastaveni
    {
        // --- okno ---
        public const int SirkaOkna = 960;
        public const int VyskaOkna = 640;

        // --- cihly ---
        // Sirka zdi vyjde 10*80 + 9*8 = 872, zbyle misto je okraj po stranach.
        public const int Sloupcu = 10;
        public const int Radku = 6;
        public const int SirkaCihly = 80;
        public const int VyskaCihly = 28;
        public const int MezeraCihel = 8;
        public const int OkrajZdi = 44;
        public const int PrvniRadekY = 90;

        // --- palka ---
        public const int SirkaPalky = 130;
        public const int VyskaPalky = 16;
        public const int PalkaY = 570;
        public const float RychlostPalky = 640f;    // pixelu za sekundu

        // --- micek ---
        public const int VelikostMicku = 16;
        public const float StartovniRychlost = 380f;
        public const float MaxRychlost = 700f;
        public const float ZrychleniZaCihlu = 4f;   // o kolik se zrychli po kazde cihle

        // --- pravidla ---
        public const int PocetZivotu = 3;
        public const int BodyZaCihlu = 10;
    }
}
