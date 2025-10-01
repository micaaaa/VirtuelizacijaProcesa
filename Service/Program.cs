using System;
using System.ServiceModel;
using Service;

namespace DroneServer
{
    public class DroneServiceProgram
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

     
            DroneService droneService = new DroneService();

       
            EventSubscriber eventSubscriber = new EventSubscriber(droneService);

          
            ServiceHost host = new ServiceHost(droneService);
      
            try
            {
                host.Open();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("DroneService je pokrenut na net.tcp://localhost:5000/DroneService");
                Console.WriteLine("Pritisnite neki taster za zatvaranje servisa...");
                Console.ReadKey();
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri pokretanju servisa: {ex.Message}");
            }
            finally
            {
                // Zatvori servis i odjavi evente
                if (host.State == CommunicationState.Opened)
                    host.Close();

                eventSubscriber.CloseEvents(droneService);
            }
        }
    }
}
