using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Lexikon
{
    /// <summary>
    /// Interaktionslogik für LexikonPage.xaml
    /// </summary>
    public partial class LexikonPage : Page
    {
        public ObservableCollection<LexikonEntry> Entrys { get; set; } = new ObservableCollection<LexikonEntry>();
        public bool OweOfCookie = false;
        private bool LexiconVisible = false;
        private readonly string connectionString =
            @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\user\Documents\Lexikon.mdf;Integrated Security=True;Connect Timeout=30";
        public LexikonPage()
        {
            InitializeComponent();
            DataContext = this;
            LoadEntriesFromDatabase();
        }
        private void LoadEntriesFromDatabase()
        {
            Entrys.Clear();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT Name, Description, Synonyme FROM LexikonEntrys";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        Entrys.Add(new LexikonEntry
                        {
                            Name = reader["Name"].ToString(),
                            Description = reader["Description"].ToString(),
                            Synonyme = reader["Synonyme"].ToString()
                        });
                    }
                    LexiconGrid.ItemsSource = Entrys;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Laden aus der Datenbank:\n" + ex.Message);
            }
        }

        private void Show_Btn_Click(object sender, RoutedEventArgs e)
        {
            if (!LexiconVisible)
            {
                // Nur einmal anzeigen, wenn noch nicht akzeptiert
                if (!OweOfCookie)
                {
                    var result = MessageBox.Show(
                        "Please note: by pressing Yes, you accept our terms and owe me a cookie.",
                        "Important Notice",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information
                    );
                    if (result == MessageBoxResult.No)
                    {
                        // Benutzer stimmt nicht zu > Vorgang abbrechen
                        return;
                    }
                    // Benutzer stimmt zu
                    OweOfCookie = true;
                }

                // Grid anzeigen
                LexiconGrid.Visibility = Visibility.Visible;
                Delete_Btn.IsEnabled = true;
                Show_Btn.Content = "Hide";
                LexiconVisible = true;
            }
            else
            {
                // Grid verstecken
                LexiconGrid.Visibility = Visibility.Hidden;
                Delete_Btn.IsEnabled = false;
                Show_Btn.Content = "Show";
                LexiconVisible = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text) ||
                string.IsNullOrWhiteSpace(DescriptionBox.Text))
            {
                MessageBox.Show("Please fill in all fields before adding it to the Lexikon!",
                                "Missing Information",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(SynonymeBox.Text))
            {
                var result = MessageBox.Show(
                    "Are you sure you don't want to add a synonym?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.No)
                {
                    // Abbruch: Benutzer möchte das Synonym noch eingeben
                    return;
                }
            }

            // Neues Entry hinzufügen
            Entrys.Add(new LexikonEntry
            {
                Name = NameBox.Text,
                Description = DescriptionBox.Text,
                Synonyme = SynonymeBox.Text
            });

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = string.Format(
                "INSERT INTO LexikonEntrys ([Name], [Description], [Synonyme]) " +
                "VALUES ('{0}', '{1}', '{2}')",
                NameBox.Text,
                DescriptionBox.Text,
                SynonymeBox.Text
                );
                SqlCommand command = new SqlCommand(query, connection);
                command.ExecuteNonQuery();
            }

            // Felder leeren
            NameBox.Clear();
            DescriptionBox.Clear();
            SynonymeBox.Clear();
        }

        private void Delete_Btn_Click(object sender, RoutedEventArgs e)
        {
            if (LexiconGrid.SelectedItem is not LexikonEntry selected)
            {
                MessageBox.Show("Please select an entry");
                return;
            }
            var result = MessageBox.Show(
                $"Do you really want to delete '{selected.Name}'?",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
                );

            if (result != MessageBoxResult.Yes)
                return;

            Entrys.Remove(selected);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "DELETE FROM LexikonEntrys WHERE Name = @name";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Parameter hinzufügen, um SQL-Injection zu vermeiden
                    command.Parameters.AddWithValue("@name", selected.Name);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
