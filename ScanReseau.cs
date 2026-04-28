using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;

namespace ScanReseau
{
    // Représente un équipement détecté sur le réseau
    class Equipement
    {
        public string AdresseIP { get; set; }
        public string NomMachine { get; set; }
        public string Statut { get; set; }     // "En ligne" ou "Hors ligne"
        public long TempsReponse { get; set; } // en millisecondes
    }

    class Program
    {
        static void Main(string[] args)
        {
            AfficherBandeau();

            Console.WriteLine("Entrez la plage IP a scanner (ex: 192.168.1)");
            Console.Write("> Base reseau : ");
            string baseReseau = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(baseReseau))
            {
                baseReseau = "192.168.1";
                Console.WriteLine("(Plage par defaut utilisee : 192.168.1)");
            }

            Console.Write("> Debut de plage (1-254) : ");
            int debut = LireEntier(1, 254, 1);

            Console.Write("> Fin de plage (1-254)   : ");
            int fin = LireEntier(1, 254, 20);

            Console.WriteLine();
            Console.WriteLine("Scan en cours...");
            Console.WriteLine(new string('-', 60));

            List<Equipement> resultats = new List<Equipement>();

            for (int i = debut; i <= fin; i++)
            {
                string ip = baseReseau + "." + i;
                Equipement eq = ScannerAdresse(ip);
                resultats.Add(eq);

                // Afficher uniquement les machines en ligne en temps réel
                if (eq.Statut == "En ligne")
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(
                        "[EN LIGNE]  {0,-18} {1,-25} {2} ms",
                        eq.AdresseIP,
                        eq.NomMachine,
                        eq.TempsReponse
                    );
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("[hors ligne] {0}", eq.AdresseIP);
                    Console.ResetColor();
                }
            }

            Console.WriteLine(new string('-', 60));
            AfficherResume(resultats);
            ExporterCSV(resultats);

            Console.WriteLine();
            Console.WriteLine("Appuyez sur une touche pour quitter...");
            Console.ReadKey();
        }

        // -------------------------------------------------------
        // Ping une adresse IP et retourne les infos de l'équipement
        // -------------------------------------------------------
        static Equipement ScannerAdresse(string ip)
        {
            Equipement eq = new Equipement();
            eq.AdresseIP = ip;
            eq.NomMachine = "Inconnu";
            eq.Statut = "Hors ligne";
            eq.TempsReponse = 0;

            try
            {
                Ping ping = new Ping();
                PingReply reponse = ping.Send(ip, 500); // timeout 500ms

                if (reponse.Status == IPStatus.Success)
                {
                    eq.Statut = "En ligne";
                    eq.TempsReponse = reponse.RoundtripTime;

                    // Essayer de résoudre le nom de la machine
                    try
                    {
                        IPHostEntry hote = Dns.GetHostEntry(ip);
                        eq.NomMachine = hote.HostName;
                    }
                    catch
                    {
                        eq.NomMachine = "Nom non resolu";
                    }
                }
            }
            catch
            {
                // En cas d'erreur réseau, la machine est considérée hors ligne
            }

            return eq;
        }

        // -------------------------------------------------------
        // Affiche un résumé du scan
        // -------------------------------------------------------
        static void AfficherResume(List<Equipement> resultats)
        {
            int enLigne = 0;
            int horsLigne = 0;

            foreach (Equipement eq in resultats)
            {
                if (eq.Statut == "En ligne")
                    enLigne++;
                else
                    horsLigne++;
            }

            Console.WriteLine();
            Console.WriteLine("=== RESUME DU SCAN ===");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  Machines en ligne   : " + enLigne);
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  Machines hors ligne : " + horsLigne);
            Console.ResetColor();
            Console.WriteLine("  Total scanne        : " + resultats.Count + " adresses");
            Console.WriteLine();

            // Afficher le tableau récapitulatif des machines en ligne
            if (enLigne > 0)
            {
                Console.WriteLine("--- Machines detectees ---");
                Console.WriteLine("{0,-20} {1,-30} {2,-12} {3}",
                    "Adresse IP", "Nom machine", "Statut", "Ping (ms)");
                Console.WriteLine(new string('-', 70));

                foreach (Equipement eq in resultats)
                {
                    if (eq.Statut == "En ligne")
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("{0,-20} {1,-30} {2,-12} {3}",
                            eq.AdresseIP,
                            eq.NomMachine,
                            eq.Statut,
                            eq.TempsReponse);
                        Console.ResetColor();
                    }
                }
            }
        }

        // -------------------------------------------------------
        // Exporte les résultats dans un fichier CSV
        // -------------------------------------------------------
        static void ExporterCSV(List<Equipement> resultats)
        {
            string nomFichier = "scan_reseau_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv";

            try
            {
                using (StreamWriter writer = new StreamWriter(nomFichier))
                {
                    // En-tête du CSV
                    writer.WriteLine("Adresse IP;Nom machine;Statut;Ping (ms);Date du scan");

                    foreach (Equipement eq in resultats)
                    {
                        writer.WriteLine(
                            eq.AdresseIP + ";" +
                            eq.NomMachine + ";" +
                            eq.Statut + ";" +
                            eq.TempsReponse + ";" +
                            DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                        );
                    }
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  Resultats exportes dans : " + nomFichier);
                Console.ResetColor();
            }
            catch
            {
                Console.WriteLine("  Impossible d'ecrire le fichier CSV.");
            }
        }

        // -------------------------------------------------------
        // Bandeau d'accueil
        // -------------------------------------------------------
        static void AfficherBandeau()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════╗");
            Console.WriteLine("║         OUTIL DE SCAN RESEAU v1.0            ║");
            Console.WriteLine("║   BTS SIO SISR - AQUAPROCESS - DUPONT Jean   ║");
            Console.WriteLine("╚══════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        // -------------------------------------------------------
        // Lit un entier dans la console avec une valeur par défaut
        // -------------------------------------------------------
        static int LireEntier(int min, int max, int defaut)
        {
            string saisie = Console.ReadLine();
            int valeur;

            if (int.TryParse(saisie, out valeur) && valeur >= min && valeur <= max)
                return valeur;

            Console.WriteLine("(Valeur par defaut utilisee : " + defaut + ")");
            return defaut;
        }
    }
}
