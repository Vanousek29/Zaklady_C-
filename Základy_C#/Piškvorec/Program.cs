namespace Piškvorec
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string[] pole = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            string hrac = "X";
            bool konecHry = false;

            while (!konecHry)
            {
                VypisPole(pole);

                Console.WriteLine("Hráč " + hrac + ", vyber pole (1-9):");
                string vstup = Console.ReadLine();
                int cislo = int.Parse(vstup) - 1;

                if (pole[cislo] == "X" || pole[cislo] == "O")
                {
                    Console.WriteLine("Tam už je vyplněno, zkus jiné pole!");
                    continue;
                }

                pole[cislo] = hrac;

                if (JeVyhra(pole, hrac))
                {
                    VypisPole(pole);
                    Console.WriteLine("Hráč " + hrac + " vyhrál!");
                    konecHry = true;
                }
                else if (JePlno(pole))
                {
                    VypisPole(pole);
                    Console.WriteLine("Remíza!");
                    konecHry = true;
                }
                else
                {
                    if (hrac == "X")
                    {
                        hrac = "O";
                    }
                    else
                    {
                        hrac = "X";
                    }
                }
            }

            Console.ReadKey();
        }

        static void VypisPole(string[] pole)
        {
            Console.WriteLine();
            Console.WriteLine(pole[0] + " | " + pole[1] + " | " + pole[2]);
            Console.WriteLine("---------");
            Console.WriteLine(pole[3] + " | " + pole[4] + " | " + pole[5]);
            Console.WriteLine("---------");
            Console.WriteLine(pole[6] + " | " + pole[7] + " | " + pole[8]);
            Console.WriteLine();
        }

        static bool JeVyhra(string[] p, string hrac)
        {
            if (p[0] == hrac && p[1] == hrac && p[2] == hrac) return true;
            if (p[3] == hrac && p[4] == hrac && p[5] == hrac) return true;
            if (p[6] == hrac && p[7] == hrac && p[8] == hrac) return true;

            if (p[0] == hrac && p[3] == hrac && p[6] == hrac) return true;
            if (p[1] == hrac && p[4] == hrac && p[7] == hrac) return true;
            if (p[2] == hrac && p[5] == hrac && p[8] == hrac) return true;

            if (p[0] == hrac && p[4] == hrac && p[8] == hrac) return true;
            if (p[2] == hrac && p[4] == hrac && p[6] == hrac) return true;

            return false;
        }

        static bool JePlno(string[] p)
        {
            foreach (string pole in p)
            {
                if (pole != "X" && pole != "O")
                {
                    return false;
                }
            }
            return true;
        }
    }
}
