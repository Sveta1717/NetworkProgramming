using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NetworkProgramming.View
{
    /// <summary>
    /// Логика взаимодействия для ClientWindow.xaml
    /// </summary>
    public partial class ClientWindow : Window
    {
        private Models.NetworkConfig networkConfig;

        public ClientWindow()
        {
            InitializeComponent();
        }       
      
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.Tag is Models.NetworkConfig config)
            {
                networkConfig = config;              // сохраняем полученную конфигурацию
               
            }
            else
            {
                MessageBox.Show("Configuration error");
                Close();
            }
        }
        private void SendButton_Click_1(object sender, RoutedEventArgs e)
        {
            if (networkConfig is null) return;
            // В отличие от сервера клиент не держит постоянное подключение,
            // а подключается по мере необходимости передачи данных
            try
            {
                Socket ClientSocket = new Socket(   // конфигурация 
                    AddressFamily.InterNetwork,     // сокета - клиента
                    SocketType.Stream,              // такая же, как у сервера
                    ProtocolType.Tcp);

                ClientSocket.Connect(                //Сервер должен быть запущен
                    networkConfig.EndPoint);         // и мы к нему подключаемся

                //-- берем данные из поля ввода, преобразуем в байты и отправляем
                // формируем команду для сервера
                Models.ClientReguest request = new Models.ClientReguest()
                {                    
                    Command = "CREATE",
                    Data = ClientMessage.Text
                };
                //ClientSocket.Send(
                //    networkConfig.Encoding.GetBytes(
                //        ClientMessage.Text));

                // переводим объект в текст (JSON) -> в байты -> отправляем команду
                ClientSocket.Send(
                    networkConfig.Encoding.GetBytes(
                        JsonSerializer.Serialize(request)
                        ));
                //сервер получает данные и отвечает нам, принимаем ответ
                byte[] buffer = new byte[2048];
                String str = "";
                int n;
                do
                {
                    n = ClientSocket.Receive(buffer);
                    str += networkConfig.Encoding.GetString(
                        buffer, 0, n);
                } while (ClientSocket.Available > 0);

                ///////////////////////////////////////               
               
                var request1 = JsonSerializer
                        .Deserialize<Models.ServerResponse>(str);

                String response1;
                switch (request1.Status)
                {
                    case "HTTP Status Codes":
                        response1 = "202 Accepted: " + request1._Data;
                        break;
                    default:
                        response1 = "522 Connection Timed Out";
                        break;
                }

                //////////////////////

                // выводим ответ сервера в "лог"
                Dispatcher.Invoke(() => { Log.Text += str + response1 + "\n"; });

                // закрываем соединение с сервером
                ClientSocket.Shutdown(SocketShutdown.Both);
                ClientSocket.Close();               
            }
             catch (Exception ex)
            {
               // MessageBox.Show(ex.Message);
               Dispatcher.Invoke(() => { Log.Text += ex.Message + "\nобмін зупинено\n"; });
            }
        }

    }
}
