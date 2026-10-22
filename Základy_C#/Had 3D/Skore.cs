using System.IO;

namespace Had3D
{
    // Ulozeni nejlepsiho skore do textoveho souboru vedle programu.
    static class Skore
    {
        static string Cesta()
        {
            return Path.Combine(AppContext.BaseDirectory, "nejlepsi-skore.txt");
        }

        public static int Nacti()
        {
            try
            {
                string cesta = Cesta();
                if (!File.Exists(cesta)) return 0;

                string text = File.ReadAllText(cesta).Trim();
                int hodnota;
                if (int.TryParse(text, out hodnota)) return hodnota;
                return 0;
            }
            catch
            {
                return 0;   // kdyz se soubor nepodari precist, hra bezi dal bez rekordu
            }
        }

        public static void Uloz(int skore)
        {
            try
            {
                File.WriteAllText(Cesta(), skore.ToString());
            }
            catch
            {
                // kdyz se nepodari zapsat, nevadi - rekord se jen neulozi
            }
        }
    }
}
