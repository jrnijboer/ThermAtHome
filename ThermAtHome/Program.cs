using System;
using System.IO;
using System.Text.RegularExpressions;

using System.Collections.Generic;
using System.Net;
using System.Linq;

using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using RaspberryPiDotNet;

namespace ThermAtHome
{
    class Program
    {
        public static readonly decimal idleTemperature = 15;
        public static DateTime heatingStarted = DateTime.Now.AddMinutes(-1);
        public static DateTime heatingStopped = DateTime.Now;
        public static int retries = 0;

        //decimal static delta = 0; //delta with last minute
        //decimal static delta5 = 0; //delta with 5 minutes ago
        //decimal static delta10 = 0; //delta with 10 minutes ago

        static void Main(string[] args)
        {
            while (true)
            {
                // mysqluser: ThermAtHome
                // mysqlpass: secretthermathomepwd
                controlTemperature();
            }
        }

        static void controlTemperature()
        {
            List<decimal> temperatures = new List<decimal>();

            //string sensorPath = "/sys/bus/w1/devices/28-0000034d2861/w1_slave";
            string sensorPath = "ds18b20.sample";
            decimal currentTemperature;
            if (TryGetTemperatureFromSensor(sensorPath, out currentTemperature))
            {
                try
                {
                    decimal desiredTemperature = GetDesiredTemperatureFromDb();
                    decimal outdoorTemperature = GetOutdoorTemperature();

                    ControlHeater(currentTemperature, desiredTemperature, outdoorTemperature);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error controlling temperature: {0}", ex.Message);
                    // shut down heating 
                    heatingStopped = DateTime.Now;
                }
                retries = 0;
                System.Threading.Thread.Sleep(/*60 **/ 1000);
            }
            else
            {
                //no temperature from sensor, retry in 1s
                Console.WriteLine("{0}: failed to read from sensor", DateTime.Now);
                System.Threading.Thread.Sleep(1000);
                retries++;
                if (retries > 10)
                {
                    Console.WriteLine("Error reading current temperature, shutting down heating");
                    // shut down heating 
                    heatingStopped = DateTime.Now;
                }
            }
        }

        private static void ControlHeater(decimal currentTemperature, decimal desiredTemperature, decimal outdoorTemperature)
        {
            //too cold, check if we can heat
            if (desiredTemperature - currentTemperature > (decimal)0.3)
            {
                // already heating, should pause
                if (heatingStarted.Subtract(heatingStopped).Minutes > 3) //arbitrary max heating interval of 3 mins
                {
                    heatingStopped = DateTime.Now;
                }
                // not heating, should start heating 
                else if (heatingStopped > heatingStarted && DateTime.Now.Subtract(heatingStopped).Minutes > 8) //arbitrary pause interval of 8 mins
                {
                    heatingStarted = DateTime.Now;
                }
            }
            //warm enough
            if (desiredTemperature > currentTemperature)
            {
                //whatever happens, shut off heating
            }
        }

        private static decimal GetOutdoorTemperature()
        {
            try
            {
                var url = @"http://api.openweathermap.org/data/2.5/weather?q=Mantgum&units=metric&appid=" + Settings.Default.weatherapi_appid; 
                var webRequest = WebRequest.Create(url);

                string outdoorTempResponse;
                using (var response = webRequest.GetResponse())
                using (var content = response.GetResponseStream())
                using (var reader = new StreamReader(content))
                {
                    outdoorTempResponse = reader.ReadToEnd();
                }
                
                string pattern = ".*\"temp\":(.*),\"pressure";
                var outdoortempMatch = Regex.Matches(outdoorTempResponse, pattern)[0].Groups[1].ToString();
                return decimal.Parse(outdoortempMatch.Replace('.', ','));
            }
            catch
            {
                return 50;
            }
        }

        private static decimal GetDesiredTemperatureFromDb()
        {
            try
            {
                string mysqlConnectionstring = "Server=192.168.1.47;Database=ThermAtHome;Uid=thermathome;Pwd=secretthermathomepwd;";
                using (var mySqlConnection = new MySqlConnection(mysqlConnectionstring))
                {
                    mySqlConnection.Open();

                    var query = "select coalesce("
                                + " (select temperature From OverrideSchedule where overrideday = curdate() and starttime < now() and stoptime > now()),"
                                + " (select temperature From FixedSchedule where weekday = dayofweek(now()) - 1 and starttime < now() and stoptime > now()),"
                                + " 0) as temperature"; // nothing in schedule, return 0 degrees. Probably should work unless you're a penguin.
                    var command = new MySqlCommand(query, mySqlConnection);
                    var reader = command.ExecuteReader();
                    reader.Read();

                    return Convert.ToDecimal(reader["temperature"]);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting desired temperature: " + ex.Message);
            }
        }

        static void StartHeating()
        {
            SetHeatingPin(PinState.High);
            heatingStarted = DateTime.Now;            
        }
        static void StopHeating()
        {            
            SetHeatingPin(PinState.Low);
            heatingStopped = DateTime.Now;
        }

        static void SetHeatingPin(PinState state)
        {
            GPIOMem heatingPin = new GPIOMem(GPIOPins.GPIO_14);
            heatingPin.PinDirection = GPIODirection.Out;
            heatingPin.Write(state);
        }

        static bool TryGetTemperatureFromSensor(string sensorPath, out decimal temperature)
        {
            try
            {
                string sensorOutput = ReadSensor(sensorPath);
                decimal t;

                if (TryParseSample(sensorOutput, out t))
                {
                    temperature = t;
                    return true;
                }

                throw new Exception("invalid input from sensor");
            }

            catch (Exception ex)
            {
                Console.WriteLine("failed reading from sensor: {0}", ex.Message);
                temperature = 0;

                return false;
            }
        }

        static string ReadSensor(string sensorPath)
        {
            using (StreamReader sr = new StreamReader(sensorPath))
            {
                return sr.ReadToEnd();
            }
        }

        static bool TryParseSample(string sample, out decimal temperature)
        {
            /*  sample response:
                39 01 4b 46 7f ff 07 10 43 : crc=43 YES
                39 01 4b 46 7f ff 07 10 43 t=19562
            */
            string pattern = @"^.*crc=(\d{2}) (YES|NO).*t=(\d{5})$";
            Regex r = new Regex(pattern, RegexOptions.Multiline | RegexOptions.Singleline);

            if (r.IsMatch(sample))
            {
                var matches = Regex.Matches(sample, pattern, RegexOptions.Multiline | RegexOptions.Singleline);

                //Regex.Matches(sample, pattern);
                temperature = decimal.Parse(matches[0].Groups[3].ToString()) / 1000;
                return true;
            }
            else
            {
                temperature = 0;
                return false;
            }
        }
    }
}
