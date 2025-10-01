using Common;
using System;

namespace Service
{
    public class EventSubscriber
    {
        private DroneService droneService; 

        public EventSubscriber(DroneService droneService)
        {
            this.droneService = droneService;
            droneService.LoadConfig();

            // Pretplata na događaje
            droneService.OnTransferStarted += HandleTransferStarted;
            droneService.OnSampleReceived += HandleSampleReceived;
            droneService.OnWarningRaised += HandleWarningRaised;
            droneService.OnTransferCompleted += HandleTransferCompleted;
        }

        // Handler metode
        private void HandleTransferStarted(object sender, TransferEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[START] {e.Message}");
            Console.ResetColor();
            Console.WriteLine();
        }

        private void HandleSampleReceived(object sender, SampleEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[SAMPLE] LinearAccelerationX={e.LinearAccelerationX}, LinearAccelerationY={e.LinearAccelerationY}, LinearAccelerationZ={e.LinearAccelerationZ}, WindSpeed={e.WindSpeed}, WindAngle={e.WindAngle}, Time={e.Time}");
            Console.ResetColor();
            Console.WriteLine();
        }

        private void HandleWarningRaised(object sender, WarningEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"⚠️ WARNING:\n {e.Warning}");
            Console.ResetColor();
            Console.WriteLine();
        }

        private void HandleTransferCompleted(object sender, TransferEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[END] {e.Message}");
            Console.ResetColor();
            Console.WriteLine();
        }

        public void CloseEvents(DroneService droneService)
        {
            if (droneService != null)
            {
                // Otkaži pretplate na događaje
                droneService.OnTransferStarted -= HandleTransferStarted;
                droneService.OnSampleReceived -= HandleSampleReceived;
                droneService.OnWarningRaised -= HandleWarningRaised;
                droneService.OnTransferCompleted -= HandleTransferCompleted;
                droneService.Dispose();
                droneService = null;
            }
        }
    }
}
