using System.Text;
using TesteWiProWorker.Model;
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
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var path = @"log.txt";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(path))
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("Consulta API realizada:");
                sb.Append(DateTime.Now.ToString() + "\r\n");

                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

                var response = client.GetAsync("http://localhost:5228/api/GetItemFila").Result;
                var contents = await response.Content.ReadAsStringAsync();

                if (contents == "\"Não há objetos a serem retornados\"")
                    return;

                var resultAllCoins = GetCoinArchive(ListCoinValue(contents));

                GenerateQuoteFile(resultAllCoins);
                watch.Stop();
                sb.Append("Time elapsed:");
                sb.Append(watch.ElapsedMilliseconds.ToString() + "ms");

                if (!File.Exists(path))
                    file.Write(sb.ToString());
                else
                    File.WriteAllText(path, sb.ToString());

                sb.Clear();
            }

        }

        private List<CoinModel> ListCoinValue(string value)
        {
            string[] coinList = value.Split(",");
            List<string> coinListResult = new List<string>();

            foreach (var item in coinList)
            {
                string[] valueList = item.Split(":");
                coinListResult.Add(valueList[1].Trim(new char[] { '"', '}' }));
            }

            return ConvertCSVCoinToModel(coinListResult);
        }

        private List<CoinQuotaModel> GetCoinArchive(List<CoinModel> coin)
        {

            List<CoinQuotaModel> coinList = new List<CoinQuotaModel>();

            string[] csvlines = File.ReadAllLines(@"Files/DadosMoeda.csv");
            csvlines = csvlines.Skip(1).ToArray();

            foreach (string item in csvlines)
            {
                string[] itemValue = item.Split(";");
                if ((Convert.ToDateTime(itemValue[1]) >= Convert.ToDateTime(coin[0].DateStart)) && (Convert.ToDateTime(itemValue[1]) <= Convert.ToDateTime(coin[0].DateEnd)))
                    coinList.Add(
                        new CoinQuotaModel
                        {
                            Coin = itemValue[0],
                            Date = itemValue[1]
                        }
                    );

            }
            return coinList;
        }

        private List<CoinModel> ConvertCSVCoinToModel(List<string> coinValue)
        {
            List<CoinModel> listCoins = new List<CoinModel>();
            listCoins.Add(new CoinModel
            {
                Coin = coinValue[0],
                DateStart = coinValue[1],
                DateEnd = coinValue[2]

            });
            return listCoins;
        }

        private List<QuotaModel> ConvertCSVQuotaToModel(string[] quotaValue)
        {
            List<QuotaModel> listQuota = new List<QuotaModel>();

            foreach (string quota in quotaValue)
            {
                string[] splitedQuota = quota.Split(";");

                listQuota.Add(new QuotaModel
                {
                    Value = splitedQuota[0],
                    Code = splitedQuota[1],
                    Date = splitedQuota[2]
                });
            }

            return listQuota;
        }

        private void GenerateQuoteFile(List<CoinQuotaModel> coinList)
        {
            var file = @"Resultado_" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + "_" + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second + ".csv";
            string delimiter = ";";

            string[] csvlines = File.ReadAllLines(@"Files/DadosCotacao.csv");
            csvlines = csvlines.Skip(1).ToArray();

            List<QuotaModel> quotaList = ConvertCSVQuotaToModel(csvlines);

            using (StreamWriter writer = new StreamWriter(file))
            {
                 StringBuilder sb = new StringBuilder();

                foreach (var coin in coinList)
                {
                    int codQuote = (int)System.Enum.Parse(typeof(QuoteEnum), coin.Coin);

                    quotaList
                    .Where(
                        x => (x.Code == codQuote.ToString())
                    )
                    .Where(
                        x => Convert.ToDateTime(x.Date) == Convert.ToDateTime(coin.Date)
                    ).ToList().ForEach(x => sb.AppendLine(coin.Coin + delimiter + coin.Date + delimiter + x.Value + "\r\n"));
                }
                File.WriteAllText(file,sb.ToString());
            }
        }
    }
}