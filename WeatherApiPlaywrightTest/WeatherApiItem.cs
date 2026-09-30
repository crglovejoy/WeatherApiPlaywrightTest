
namespace WeatherApiPlaywrightTest
{
    public class WeatherApiItem
    {
        public readonly string Latitude;
        public readonly string Longitude;
        public readonly string City;
        public readonly string State;

        internal WeatherApiItem(string inputLine)
        {
            string[] inputItems = inputLine.Split(',');

            if (inputItems.Length != 4
                || !decimal.TryParse(inputItems[0].Trim(), out decimal decVal1) 
                || !decimal.TryParse(inputItems[1].Trim(), out decimal decVal2)
                )
            {
                throw new ArgumentException($"input line is invalid: {inputLine}");
            }

            Latitude = inputItems[0].Trim();
            Longitude = inputItems[1].Trim();
            City = inputItems[2].Trim();
            State = inputItems[3].Trim();
        }
    }
}
