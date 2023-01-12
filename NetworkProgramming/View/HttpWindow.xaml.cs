using NetworkProgramming.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;

namespace NetworkProgramming.View
{
    /// <summary>
    /// Логика взаимодействия для HttpWindow.xaml
    /// </summary>
    public partial class HttpWindow : Window
    {
        public HttpWindow()
        {
            InitializeComponent();
        }

        private void HtmlRequestButon_Click(object sender, RoutedEventArgs e)
        {
            HttpClient httpClient = new HttpClient() {       // Создаем клиент для отправки запроса на
                BaseAddress = new Uri(textBoxUrl.Text)        // сайт из textBoxUrl (https://itstep.org)
            };
            /*
            var responce = await httpClient.GetStringAsync("/"); // ближе к синхронному
            textBlockResponse.Text = responce;
            */
            httpClient.GetStringAsync("/")                    // Отправляем запрос на домашнюю страницу (/)
                .ContinueWith(t => Dispatcher.Invoke(         // Добавляем "нить" - задачу, запускаемую после получения результата
                    () =>                                     // Поскольку выполнение в отдельном потоке -
                    textBlockResponse.Text = t.Result));      // вызываем Dispatcher с задачей вывода полученного 
        }                                                     // текста в textBlockResponse

        private void XmlRequestButon_Click(object sender, RoutedEventArgs e)
        {
            HttpClient httpClient = new HttpClient();         // Вариант: при создании клиента
            httpClient.GetStringAsync(textBoxXmlUrl.Text)     // мы не указали базовый адрес,
                .ContinueWith(t => Dispatcher.Invoke(() =>    // поэтому в Get-метод передаем
                {                                             // полный Url, а не только */*
                    textBlockResponse.Text = t.Result;        // Также дополнительно
                    httpClient.Dispose();                     // освободим ресурс httpClient
                   {
                        ParseXmlRates();                          // запускаем разбор XML данных
                    }
                  
                }));
        }
               
        private void ParseXmlRates()                           // XmlDocument - основа для работы с XML
        {                                                      // загружаем текст в документ для 
            XmlDocument rates = new XmlDocument();             // дальнейшей обработки 
            rates.LoadXml(                                     // Селектор - запрос на выборку узлов,
                textBlockResponse.Text);                       // соответствующих определенным критериям.
            XmlNodeList currencies =                           // Сам контент (без строки заголовка <?xml ...>  
                rates                                          // Отбор по имени тегов (<currency>)
                .DocumentElement                               // 
                .SelectNodes("currency");                      // 

            //TextBox textBox = new TextBox();
            //TextWriter textWriter = new StringWriter();
            //textWriter.Write(EnterDate.Text);
           
                if (currencies is null) return;                    // Проверка на успешный разбор документа
                          
                
            foreach (XmlNode node                              // Итерирование коллекции - элементы XmlNode
                in                                             // 
               currencies)                                     // node.InnerText  - текст узла (без тегов)
            {                                                  // если у узла есть внутренние узлы, то все их
                TreeViewItem item = new TreeViewItem()         // InnerText соединены
                { 
                    Header= node                                // node.ChildNodes - коллекция внутренних узлов
                    .ChildNodes[1]                              // порядок их следования - как в исхлдном документе
                    .InnerText                                  // [0] - r030        [3] - cc
                };                                              // [1] - txt         [4] - exchangedate
                                                                // [2] - rate                   
                
                    item.Items.Add(new TreeViewItem { Header = "r030: " + node.ChildNodes[0].InnerText });
                    item.Items.Add(new TreeViewItem { Header = "cc: " + node.ChildNodes[3].InnerText });
                    item.Items.Add(new TreeViewItem { Header = "rate: " + node.ChildNodes[2].InnerText });
                    item.Items.Add(new TreeViewItem
                    {
                        Header = String.Format("1 {0} = {1} UAH",
                        node.ChildNodes[3].InnerText,
                        node.ChildNodes[2].InnerText)
                    });
                    item.Items.Add(new TreeViewItem
                    {
                        Header = String.Format("1 UAN = {1:F2} {0}",    // F2 - Float with 2 digit after '.'              
                        node.ChildNodes[3].InnerText,
                        1 / decimal.Parse(node.ChildNodes[2].InnerText, // или  Convert.ToSingle(...)
                        CultureInfo.InvariantCulture.NumberFormat))     // в наших ОС десятичная точка считается
                                                                        // запятой. Для "сброса" єтого используем InvariantCulture
                    });
                     
                    //node.ChildNodes[4].InnerText = EnterDate.Text;
                   // EnterDate.AppendText(node.ChildNodes[4].InnerText);                   
                    item.Items.Add(new TreeViewItem { Header = "date: " + node.ChildNodes[4].InnerText });
                   
                    treeView1.Items.Add(item);               
            }           
        }

        private async void JsonRequestButon_Click(object sender, RoutedEventArgs e)
        {
            HttpClient httpClient = new HttpClient()            // Здесь демонстрируется разделение
            {                                                   // базового адресв: https://bank.gov.ua
                BaseAddress = new Uri("https://bank.gov.ua")    // и запроса: /NBUStatService/v1/statdirectory/exchange?json                                                                   
            };                                                  // Использование await требует
            textBlockResponse.Text = await                      // указать async в сигнатуре метода
                httpClient.GetStringAsync(textBoxJsonUrl.Text); // но разрешает не использовать Dispatcher
                                                                // а  также упрощает блок вместо Dispose
            ParseJsonRates();
        }

        // переводим в JSON из textBlockResponse в treeView
        private void ParseJsonRates()
        {
            var ratesList =
            JsonSerializer.Deserialize<                          // Models.NbuJsonRate - один объект
                List<Models.NbuJsonRate>                         // ответ - коллекция объектов
                >(textBlockResponse.Text);                       // textBlockResponse.Text - JSON (текст) 

            if (ratesList is null) return;
            
            treeView1.Items.Clear();
            
            foreach(Models.NbuJsonRate rate in ratesList)
            {
                if (rate.txt == "Долар США" || rate.txt == "Євро" || rate.txt == "Єна")
                {
                    // создаем узел с сокращенным названием валюты
                    TreeViewItem item = new TreeViewItem()
                    {
                        Header = rate.cc
                    };

                    // заполняем узел под-узлами со всеми данными
                    item.Items.Add(new TreeViewItem { Header = rate.txt });
                    item.Items.Add(new TreeViewItem { Header = "rate^ " + rate.rate });
                    item.Items.Add(new TreeViewItem { Header = "r030^ " + rate.r030 });
                    item.Items.Add(new TreeViewItem { Header = rate.exchangedate });

                    // добавляем узел к "дереву"
                    treeView1.Items.Add(item);
                }
            }
        }
    }
}

/* Добавить в окно элемент для ввода/выбора даты
 * Реализовать отображение курсов валют за выбранную дату
 * https://bank.gov.ua/NBUStatService/v1/statdirectory/exchange?date=20200302
 * Обратить внимание: дата собирается в строку без разделителей
 *  20200302 - 2020-03-02 (02.03.2020)
 *  Детальнее на https://bank.gov.ua/ua/open-data/api-dev
 */
