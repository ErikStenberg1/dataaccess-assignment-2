using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Assignment2
{
    public class Article
    {
        public string Title { get; set; }
        public string FirstTitle { get; set; }
        public DateTime Date { get; set; }

    }
    public partial class MainWindow : Window
    {
        private Thickness spacing = new Thickness(5);
        private HttpClient http = new HttpClient();
        // We will need these as instance variables to access in event handlers.
        private TextBox addFeedTextBox;
        private Button addFeedButton;
        private ComboBox selectFeedComboBox;
        private Button loadArticlesButton;
        private StackPanel articlePanel;
        private XDocument document;
        private List<Article> articles = new List<Article>();
        private Dictionary<string, string> titleUrl = new Dictionary<string, string>();
        private string firstTitle;
        private string url;


        public MainWindow()
        {
            InitializeComponent();
            Start();
        }

        private void Start()
        {
            // Window options
            Title = "Feed Reader";
            Width = 800;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Scrolling
            var root = new ScrollViewer();
            root.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            Content = root;

            // Main grid
            var grid = new Grid();
            root.Content = grid;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var addFeedLabel = new Label
            {
                Content = "Feed URL:",
                Margin = spacing
            };
            grid.Children.Add(addFeedLabel);

            addFeedTextBox = new TextBox
            {
                Margin = spacing,
                Padding = spacing
            };
            grid.Children.Add(addFeedTextBox);
            Grid.SetColumn(addFeedTextBox, 1);

            addFeedButton = new Button
            {
                Content = "Add Feed",
                Margin = spacing,
                Padding = spacing
            };
            grid.Children.Add(addFeedButton);
            Grid.SetColumn(addFeedButton, 2);
            addFeedButton.Click += AddFeedButton_Click;

            var selectFeedLabel = new Label
            {
                Content = "Select Feed:",
                Margin = spacing
            };
            grid.Children.Add(selectFeedLabel);
            Grid.SetRow(selectFeedLabel, 1);

            selectFeedComboBox = new ComboBox
            {
                Margin = spacing,
                Padding = spacing,
                IsEditable = false
            };
            grid.Children.Add(selectFeedComboBox);
            Grid.SetRow(selectFeedComboBox, 1);
            Grid.SetColumn(selectFeedComboBox, 1);
            selectFeedComboBox.Items.Add("All Feeds");


            loadArticlesButton = new Button
            {
                Content = "Load Articles",
                Margin = spacing,
                Padding = spacing
            };
            grid.Children.Add(loadArticlesButton);
            Grid.SetRow(loadArticlesButton, 1);
            Grid.SetColumn(loadArticlesButton, 2);
            loadArticlesButton.Click += LoadArticlesButton_Click;

            articlePanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = spacing
            };
            grid.Children.Add(articlePanel);
            Grid.SetRow(articlePanel, 2);
            Grid.SetColumnSpan(articlePanel, 3);
            
        }

        private async Task<XDocument> LoadDocumentAsync(string url)
        {
            // This is just to simulate a slow/large data transfer and make testing easier.
            // Remove it if you want to.
            //await Task.Delay(1000);
            var response = await http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync();
            var feed = XDocument.Load(stream);
            return feed;
        }
        private async void AddFeedButton_Click(object sender, RoutedEventArgs e)
        {
            addFeedButton.IsEnabled = false;
            url = addFeedTextBox.Text;
            document = await LoadDocumentAsync(url);
            firstTitle = document.Descendants("title").First().Value;
            selectFeedComboBox.Items.Add(firstTitle);
            //Adds the first title and url to a dictionary
            titleUrl.Add(firstTitle, url);
            addFeedTextBox.Clear();
            addFeedButton.IsEnabled = true;
        }
        private async void LoadArticlesButton_Click(object sender, RoutedEventArgs e)
        {
            articlePanel.Children.Clear();
            articles.Clear();
            loadArticlesButton.IsEnabled = false;
            firstTitle = selectFeedComboBox.SelectedItem.ToString();
            //If "all feeds" is selected
            if (selectFeedComboBox.SelectedIndex == 0)
            {
                var task = new List<string>();
                foreach (var title in titleUrl)
                {
                    task.Add(title.Value);
                }
                var tasks = task.Select(LoadDocumentAsync).ToList();
                var result = await Task.WhenAll(tasks);
                CreateArticles(result);

            }
            
            else
            {
                //If any specific feed is selected
                url = titleUrl[selectFeedComboBox.SelectedItem.ToString()];
                document = await LoadDocumentAsync(url);
                XDocument[] xDocuments = { document };
                CreateArticles(xDocuments);

            }
            loadArticlesButton.IsEnabled = true;
        }
        private void CreateArticles(XDocument[] xDocuments)
        {
            List<Article> articles = new List<Article>();
            foreach (var document in xDocuments)
            {
                for (int i = 0; i < 5; i++) 
                {
                    string firstTitle = document.Descendants("title").First().Value;
                    string[] titles = document.Descendants("title").Skip(2).Select(t => t.Value).ToArray();
                    string[] dates = document.Descendants("pubDate").Select(p => p.Value).ToArray();

                    Article article = new Article
                    {
                        FirstTitle = firstTitle,
                        Title = titles[i],
                        Date = DateTime.ParseExact(dates[i].Substring(0, 25), "ddd, dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                    };
                    articles.Add(article);
                }
                
            }

            foreach (var article in articles.OrderByDescending(a => a.Date))
            {
                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = spacing
                };
                articlePanel.Children.Add(stackPanel);

                var articleTitle = new TextBlock
                {
                    Text = Convert.ToString(article.Date + "-" + article.Title),
                    FontWeight = FontWeights.Bold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                stackPanel.Children.Add(articleTitle);

                var articleWebsite = new TextBlock
                {
                    Text = article.FirstTitle
                };
                stackPanel.Children.Add(articleWebsite);
            }
        }
    }
}