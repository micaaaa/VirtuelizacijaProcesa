using Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.ServiceModel;
using System.Threading;

namespace Client
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Inicijalizacija WCF kanala
                ChannelFactory<IDroneService> factory = new ChannelFactory<IDroneService>("DroneService");
                IDroneService proxy = factory.CreateChannel();

                // Čitanje konfiguracije
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string fileName = ConfigurationManager.AppSettings["path"];
                var filePath = Path.Combine(exeDir, fileName);
                var maxRows = Int32.Parse(ConfigurationManager.AppSettings["maxRows"]);

                Console.WriteLine($"Putanja do fajla: {filePath}");
                Console.WriteLine($"Maksimalno redova za učitavanje: {maxRows}");

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Fajl nije pronađen: {filePath}");
                    WaitForExit();
                    return;
                }

                // Čitanje uzoraka iz CSV fajla pomoću nove klase
                List<DroneSample> samples;
                using (var reader = new DroneReaderFromCsv(filePath))
                {
                    samples = reader.ReadSamples(maxRows);
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Učitano je {samples.Count} validnih redova iz CSV fajla.");
                Console.ResetColor();

                var meta = new SessionMeta();
                Console.WriteLine("[START] Pokrenuta drone sesija!");

                try
                {

                    var startResponse = proxy.StartSession(meta);
                    if (startResponse.ServiceType == ServiceType.NACK)
                    {
                        Console.WriteLine($"NACK: {startResponse.Message}");
                        WaitForExit();
                        return;
                    }
                }
                catch (FaultException<ValidationFault> ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Validacijska greška pri pokretanju: {ex.Detail.Message}");
                    Console.ResetColor();
                    WaitForExit();
                    return;
                }
                catch (FaultException<DataFormatFault> ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Format greška pri pokretanju: {ex.Detail.Message}");
                    Console.WriteLine($"Detalji: {ex.Detail.Details}");
                    Console.ResetColor();
                    WaitForExit();
                    return;
                }

                // Slanje uzoraka serveru
                int i = 0;
                foreach (var sample in samples)
                {
                    try
                    {
                        var response = proxy.PushSample(sample);

                        if (response.ServiceType == ServiceType.ACK)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"{++i} -> Uzorak uspešno obrađen: {response.Message} (Time: {sample.Time})");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"{++i} -> Uzorak odbijen: {response.Message} (Time: {sample.Time})");
                        }
                        Console.ResetColor();
                    }
                    catch (FaultException ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[NEOČEKIVANA WCF GREŠKA] {i}: {ex.Message}");
                        Console.ResetColor();
                    }

                    Thread.Sleep(100); // simulacija vremenskog razmaka
                }

                // Zatvaranje sesije
                var endResponse = proxy.EndSession();

                if (endResponse.ServiceType == ServiceType.ACK)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[KRAJ] Sesija je zatvorena");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[GREŠKA END SESSION] {endResponse.Message}");
                }
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Nepredviđena greška: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }
            finally
            {
                WaitForExit();
            }
        }

        private static void WaitForExit()
        {
            Console.WriteLine("Pritisni ENTER za izlaz...");
            Console.ReadLine();
        }
    }
}
