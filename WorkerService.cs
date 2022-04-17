using System.Text;
using System.Net.Http;
using System.Globalization;
using CsvHelper;

namespace TesteWiProWorker
{
    public class WorkerService
    {
        static HttpClient client = new HttpClient();
        public async void GetItemQueue()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Consulta API realizada:");
            sb.Append(DateTime.Now.ToString());

            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            var response = client.GetAsync("http://localhost:5228/api/GetItemFila").Result;
            var contents = await response.Content.ReadAsStringAsync();

            if (contents == "\"Não há objetos a serem retornados\"")
                return;

            var resultAllCoins = GetCoinArchive(ListCoinValue(contents));

            GenerateQuoteFile(resultAllCoins);

            File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "log.txt", sb.ToString());
            sb.Clear();

        }

        public List<string> ListCoinValue(string value)
        {
            string[] coinList = value.Split(",");
            List<string> coinListResult = new List<string>();

            foreach (var item in coinList)
            {
                string[] valueList = item.Split(":");
                coinListResult.Add(valueList[1].Trim(new char[] { '"', '}' }));
            }

            return coinListResult;
        }

        public List<string> GetCoinArchive(List<string> coin)
        {

            List<string> coins = new List<string>();

            string[] csvlines = File.ReadAllLines(@"Files/DadosMoeda.csv");
            csvlines = csvlines.Skip(1).ToArray();

            foreach (string item in csvlines)
            {

                string[] itemValue = item.Split(";");
                if ((Convert.ToDateTime(itemValue[1]) >= Convert.ToDateTime(coin[1])) && Convert.ToDateTime(itemValue[1]) <= Convert.ToDateTime(coin[2]))
                    coins.Add(item);

            }

            return coins;

        }

        public void GenerateQuoteFile(List<string> allcoins)
        {
            var file = @"Resultado_" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + "_" + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second + ".csv";
            string delimiter = ", ";

            string[] csvlines = File.ReadAllLines(@"Files/DadosCotacao.csv");
            csvlines = csvlines.Skip(1).ToArray();
            using (StreamWriter writer = new StreamWriter(file))
            {
                foreach (string itemCoin in allcoins)
                {
                    foreach (string item in csvlines)
                    {
                        string[] splitedItem = item.Split(";");

                        string[] splitedCoin = itemCoin.Split(";");
                        int codQuote = (int)System.Enum.Parse(typeof(QuoteEnum), splitedCoin[0]);

                        if (splitedItem[1] == codQuote.ToString() && Convert.ToDateTime(splitedItem[2]) == Convert.ToDateTime(splitedCoin[1]))
                        {
                            string[] splitedItemFinal = splitedItem[0].Split(',');
                            string createText = splitedCoin[0] + delimiter + splitedCoin[1] + delimiter + splitedItemFinal[1] + "\r\n";
                            writer.Write(createText);
                        }

                    }
                }
            }
        }

    }
}