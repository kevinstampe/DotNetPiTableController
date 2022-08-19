using System.Device.I2c;
using Iot.Device.Vl53L0X;
using System.Device.Gpio;
using Microsoft.AspNetCore.Mvc;

namespace PiTableController.Web.Controllers;

public class HomeController : Controller
{
    private const int PinUp = 17;
    private const int PinDown = 27;

    [HttpGet]
    public string Index(int height)
    {
        try
        {
            if (height == 0)
                return "height is empty";
            
            Vl53L0X laserMeasurement = new(I2cDevice.Create(new I2cConnectionSettings(1, Vl53L0X.DefaultI2cAddress)));
            laserMeasurement.Precision = Precision.LongRange;
            laserMeasurement.StartContinuousMeasurement(10);

            using GpioController controller = new();
            controller.OpenPin(PinUp, PinMode.Output);
            controller.OpenPin(PinDown, PinMode.Output);

            int distance = laserMeasurement.Distance;
            int prevDistance = 0;
            int targetDistance = height * 10;
            int difference = targetDistance;
            int equalReadings = 0;

            DateTime stamp = DateTime.Now;

            while (difference != 0)
            {
                TimeSpan timeElapsed = stamp - DateTime.Now;
                distance = laserMeasurement.Distance;
                difference = targetDistance - prevDistance;

                if (prevDistance == distance)
                    equalReadings += 1;

                if (equalReadings > 5)
                    break;

                if (timeElapsed.Seconds > 15)
                    break;

                if (difference > 0)
                {
                    controller.Write(PinUp, false);
                    controller.Write(PinDown, true);
                }
                else if (difference < 0)
                {
                    controller.Write(PinUp, true);
                    controller.Write(PinDown, false);
                }
                else
                {
                    controller.Write(PinUp, false);
                    controller.Write(PinDown, false);
                }

                prevDistance = distance;
                Thread.Sleep(100);
            }

            return $"height: {distance}";
        }
        catch (Exception ex)
        {
            return $"message:{ex.Message}, stacktrace: {ex.StackTrace}";
        }
    }
}