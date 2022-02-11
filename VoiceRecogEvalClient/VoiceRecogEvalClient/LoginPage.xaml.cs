using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace VoiceRecogEvalClient
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();

            EntryUsername.Completed += (s, e) => BtnAccept.Focus();
            EntryUsername.Text = Preferences.Get("username", string.Empty);
        }

        /// <summary>
        /// Adds user
        /// </summary>
        private async void Btn_accept_Clicked(object sender, EventArgs e)
        {
            var username = EntryUsername.Text;

            if (string.IsNullOrWhiteSpace(username))
            {
                await DisplayAlert("Anmelden", "Bitte gib einen Namen an", "Ok");
                return;
            }

            Preferences.Set("username", EntryUsername.Text);

            Application.Current.MainPage = new MainPage(username);
        }
    }
}