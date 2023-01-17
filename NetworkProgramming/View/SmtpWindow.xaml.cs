using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
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

namespace NetworkProgramming.View
{
    /// <summary>
    /// Логика взаимодействия для SmtpWindow.xaml
    /// </summary>
    public partial class SmtpWindow : Window
    {
        dynamic email;  // динамические объекты могут менять свой тип в процессе выполнения
                         // программы.
        SqlConnection connection;
        Random random = new Random();

        public SmtpWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Загружаем конфигурацию SMTP
            email =                                         // динамический десериализатор
                JsonSerializer.Deserialize<dynamic>(        // возвращает тип JsonElement
                    File.ReadAllText("emailconfig.json"));  // в котором значения извлекаются
            // цепочками вида
            // email.GetProperty("smtp").GetProperty("host").GetString()
            // email.GetProperty("smtp").GetProperty("port").GetInt32()
            if (email is null)
            {
                MessageBox.Show("Email configuration load error");
                this.Close();
                return;
            }

            // Загружаем конфигурацию БД и подключаеся к ней
            var db = JsonSerializer.Deserialize<dynamic>(
                File.ReadAllText("db.json"));
            if (db is null)
            {
                MessageBox.Show("DB configuration load error");
                this.Close();
                return;
            }
            try
            {
                connection = new SqlConnection(
                    db.GetProperty("EmailDatabase").GetString());
                connection.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show("DB connection error " + ex.Message);
                this.Close();
                return;
            }
        }

        private void SendEmail_Click(object sender, RoutedEventArgs e)
        {
            if (email is null) return;
            JsonElement smtp = email.GetProperty("smtp");
            String host = smtp.GetProperty("host").GetString();
            int port = smtp.GetProperty("port").GetInt32();
            String mailbox = smtp.GetProperty("email").GetString();
            String password = smtp.GetProperty("password").GetString();
            bool ssl = smtp.GetProperty("ssl").GetBoolean();

            #region Д.З. Перед отправкой кода подтверждения убедиться что почта новая
            // и ранее на нее код не отправлялся: выводить соотв. сообщение

            String newemail = mailTo.Text;
            String first;

            var SqlCommand1 = new SqlCommand(
                $"SELECT code FROM email_codes" +
                $" WHERE email = N'{newemail}' ",connection);
            
            try
            {
                first = Convert.ToString(SqlCommand1.ExecuteScalar());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Упс!", ex.Message);
                return;
            }
            if (first == "") 
            {
                MessageBox.Show("Вітаєио з новою реєстрацією");
                return;
            }

            #endregion
            var smtpClient = new SmtpClient(host)
            {
                Port = port,
                EnableSsl = ssl,
                Credentials = new NetworkCredential(mailbox, password)
            };
            // генерируем код подтверждения
            int code = random.Next(100000, 1000000);
            smtpClient.Send(mailbox,
                mailTo.Text,
                mailSubj.Text,
                mailBody.Text + code);  // добавляем код к тексту письма
            // помещаем код в БД
            var sqlCommand = new SqlCommand(
                $"INSERT INTO email_codes(email, code) VALUES( N'{mailTo.Text}', '{code}' ) ",
                connection);
            sqlCommand.ExecuteNonQuery();
            MessageBox.Show("Код підтверждення відправлено на вказану пошту");
        }

        private void ConfirmCode_Click(object sender, RoutedEventArgs e)
        {
            if (connection is null) return;
            String email = confirmEmail.Text;
            // извлекаем код из БД для этой почты
            var sqlCommand = new SqlCommand(
                $"SELECT code FROM email_codes WHERE email = N'{email}' ",
                connection);
            String code;
            try
            {
                code = Convert.ToString(
                    sqlCommand.ExecuteScalar());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            if (code == "")  // нет почты в БД - возврат NULL и в строке ""
            {
                MessageBox.Show("Введенну пошту не зареєстровано");
                return;
            }
            if (code == "000000")  // почта уже подтверждена
            {
                MessageBox.Show("Введенну пошту вже підтверджено");
                return;
            }
            if (code == confirmCode.Text)  // код введен правильно
            {
                // выводим сообщение об успешном подтверждении
                MessageBox.Show("Введенну пошту успішно підтверджено");
                // "сбрасываем" код в БД - устанавливаем его в "000000"
                var sqlCommand2 = new SqlCommand(
                    $"UPDATE email_codes SET code = '000000' WHERE email = N'{email}' ",
                    connection);
                sqlCommand2.ExecuteNonQuery();

            }
            else  // код введен неправильно
            {
                MessageBox.Show("Код введено невірно");
                return;
            }
        }
    }
}
