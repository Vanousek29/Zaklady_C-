namespace Šibenice
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string[] slova = { "programator", "pocitac", "karakoram", "monitor", "goldenhill" };
            Random rnd = new Random();
            string slovo = slova[rnd.Next(0, slova.Length)];

            char[] uhadnuto = new char[slovo.Length];
            for (int i = 0; i < slovo.Length; i++)
            {
                uhadnuto[i] = '_';
            }

            int pocetZivotu = 6;
            bool vyhra = false;

            while (pocetZivotu > 0 && !vyhra)
            {
                Console.WriteLine();
                Console.WriteLine("Zbývající pokusy: " + pocetZivotu);
                Console.WriteLine(string.Join(" ", uhadnuto));

                Console.WriteLine("Napiš písmeno:");
                string vstup = Console.ReadLine();
                char pismeno = vstup[0];

                if (slovo.Contains(pismeno))
                {
                    for (int i = 0; i < slovo.Length; i++)
                    {
                        if (slovo[i] == pismeno)
                        {
                            uhadnuto[i] = pismeno;
                        }
                    }
                }
                else
                {
                    pocetZivotu--;
                    Console.WriteLine("Špatně!");
                }

                if (new string(uhadnuto) == slovo)
                {
                    vyhra = true;
                }
            }

            Console.WriteLine();
            if (vyhra)
            {
                Console.WriteLine("Vyhrál jsi! Slovo bylo: " + slovo);
            }
            else
            {
                Console.WriteLine("Prohrál jsi! Slovo bylo: " + slovo);
            }

            Console.ReadKey();
        }
    }
}
