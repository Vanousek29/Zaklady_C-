namespace Had3D
{
    // Jedna bunka hraciho pole. X je sloupec, Y je radek.
    // Pozor: Y je porad jen cislo radku na desce - vyska v 3D scene se resi jinde.
    struct Bod
    {
        public int X;
        public int Y;

        public Bod(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool JeStejny(Bod jiny)
        {
            return X == jiny.X && Y == jiny.Y;
        }
    }

    enum Smer { Nahoru, Dolu, Vlevo, Vpravo }
}
