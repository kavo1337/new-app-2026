using app.CLIENT.Views.Dashboard;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace app.CLIENT.Views.Login
{
    /// <summary>
    /// Логика взаимодействия для LoginView.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        private readonly HttpClient _httpClient = new();
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        public LoginView()
        {
            InitializeComponent();
            _httpClient.BaseAddress = new Uri("http://localhost:7175/");
        }
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailBox.Text?.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                StatusText.Text = "Введите электронную почту и пароль.";
                return;
            }

            LoginButton.IsEnabled = false;
            StatusText.Text = "  ...";

            try
            {
                var payload = new LoginRequest(email, password);
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/authControlers/login", content);
                if (!response.IsSuccessStatusCode)
                {
                    StatusText.Text = "Ошибка входа.";
                    return;
                }

                var body = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<LoginResponse>(body, _jsonOptions);
                if (data is null)
                {
                    StatusText.Text = "Ошибка входа.";
                    return;
                }

                Session.AccessToken = data.AccessToken;
                Session.RefreshToken = data.RefreshToken;
                Session.User = data.User;

                StatusText.Text = "Вы вошли.";

                DashBoardView dashboard = new DashBoardView();
                dashboard.Show();
                Close();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Ошибка: {ex.Message}";
            }
            finally
            {
                LoginButton.IsEnabled = true;
            }
        }
    }
}
