using System.ComponentModel.Design;

namespace Kamen__Nuzky__Papir
{
    internal class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("Napiš Kamen, Nuzky nebo Papir (nebo 'konec')");
                string hrac = Console.ReadLine();

                if (hrac == "konec")
                {
                    break;
                }

                Random hod = new Random();
                int cislo = hod.Next(1, 4);

                string pc = "";
                if (cislo == 1) pc = "Kamen";
                if (cislo == 2) pc = "Nuzky";
                if (cislo == 3) pc = "Papir";

                Console.WriteLine("Počítač si vybral: " + pc);

                if (hrac == pc)
                {
                    Console.WriteLine("Ty kráso remíza!");
                }
                else if (hrac == "Kamen" && pc == "Nuzky")
                {
                    Console.WriteLine("Vyhráváte!");
                }
                else if (hrac == "Nuzky" && pc == "Papir")
                {
                    Console.WriteLine("Vyhráváte!");
                }
                else if (hrac == "Papir" && pc == "Kamen")
                {
                    Console.WriteLine("Vyhráváte!");
                }
                else
                {
                    Console.WriteLine("Prohráváte! Nevadí, třeba příště!");
                }

                Console.WriteLine();
            }

            Console.WriteLine("Diky za hru, zdar!");
            Console.ReadKey();
        }
    }
}
