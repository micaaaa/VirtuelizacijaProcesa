using System;
using System.ServiceModel;
using Service; 

namespace DroneServer
{
    class Program
    {
        static void Main(string[] args)
        {
      
            ServiceHost host = new ServiceHost(typeof(DroneService));

            try
            {
                host.Open();
                Console.WriteLine("DroneService is running...");
                Console.WriteLine("Press any key to stop the service.");
                Console.ReadKey();

                host.Close();
                Console.WriteLine("DroneService is stopped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                host.Abort();
            }
        }
    }
}
