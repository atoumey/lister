using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;
using System.Net.Cache;

namespace Lister
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ListerWindow : Window, INotifyPropertyChanged
    {
        delegate void VoidDelegate();

        protected ObservableCollection<Item> FullList { get { return _fullList; } set { _fullList = value; FirePropertyChanged("FullList"); } }
        private ObservableCollection<Item> _fullList = new ObservableCollection<Item>();

        protected ObservableCollection<Item> FilteredList { get { return _filteredList; } set { _filteredList = value; FirePropertyChanged("FilteredList"); } }
        private ObservableCollection<Item> _filteredList = new ObservableCollection<Item>();

        protected string originalFileName;
        protected string originalFilePath;
        protected string originalFileContents;

        public ListerWindow()
        {
            InitializeComponent();
        }

        private void Go_Click(object sender, RoutedEventArgs e)
        {
            Go(urlTextBox.Text);
        }

        protected void Go(string url)
        {
            try
            {
                this.Background = Brushes.White;
                if (string.IsNullOrEmpty(url)) return;

                WebClient wc = new WebClient() { Encoding = Encoding.UTF8, CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore) };
                wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);
                wc.DownloadStringAsync(new Uri(url));
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            string line = null;
            int lineNum = 0;

            try
            {
                if (e.Error != null)
                {
                    ShowException(e.Error);
                    return;
                }

                originalFilePath = urlTextBox.Text;
                originalFileName = originalFilePath.Substring(originalFilePath.LastIndexOf('/') + 1);
                this.Title = "Lister - " + originalFileName;
                originalFileContents = e.Result;
                saveButton.IsEnabled = false;

                FullList.Clear();
                StringReader sr = new StringReader(e.Result);

                line = sr.ReadLine();
                while (line != null)
                {
                    Item newItem = new Item();

                    int ii;
                    for (ii = 0; ii < line.Length; ii++)
                        if (line[ii] == ',')
                        {
                            DateTime dtTemp;
                            DateTime.TryParse(line.Substring(0, ii), out dtTemp);
                            newItem.When = dtTemp;
                            break;
                        }

                    int jj;
                    for (jj = ii + 1; jj < line.Length; jj++)
                        if (line[jj] == ',')
                        {
                            float fTemp;
                            float.TryParse(line.Substring(ii + 1, jj - ii - 1), out fTemp);
                            newItem.Amount = fTemp;
                            break;
                        }

                    if (line.Length > jj + 1)
                    {
                        bool quoted = false;
                        if (line[jj + 1] == '"') quoted = true;
                        if (quoted)
                        {
                            newItem.What = line.Substring(jj + 2, line.Length - jj - 3);
                            newItem.What = newItem.What.Replace("\"\"", "\"");
                        }
                        else
                            newItem.What = line.Substring(jj + 1, line.Length - jj - 1);
                    }
                    else
                        newItem.What = "";

                    FullList.Add(newItem);
                    line = sr.ReadLine();
                    lineNum++;
                }

                listView.ItemsSource = FullList;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error at line " + lineNum + ", line text = " + line);
                ShowException(ex);
            }
        }

        protected void ShowException(Exception ex)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new VoidDelegate(delegate
            {
                this.Background = Brushes.Red;

                StringBuilder sb = new StringBuilder();
                while (ex != null)
                {
                    sb.AppendLine(ex.Message).AppendLine();
                    sb.AppendLine(ex.StackTrace).AppendLine().AppendLine();
                    ex = ex.InnerException;
                }
                MessageBox.Show(sb.ToString());
            }));
        }

        private void urlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Go(urlTextBox.Text);
        }

        private void List_Click(object sender, RoutedEventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        protected class Item : INotifyPropertyChanged
        {
            public DateTime When { get { return _when; } set { _when = value; } }
            protected DateTime _when;

            public float Amount { get { return _amount; } set { _amount = value; } }
            protected float _amount;

            public string What { get { return _what; } set { _what = value; } }
            protected string _what;

            public event PropertyChangedEventHandler PropertyChanged;
            protected void FirePropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                new Thread(new ThreadStart(delegate
                {
                    WebClient wc = new WebClient() { Encoding = Encoding.UTF8, CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore) };
                    wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadLogTypesCompleted);
                    wc.DownloadStringAsync(new Uri(@"http://taiyedbrodels.com/projects/logger/logtypes.txt"));
                })).Start();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        void wc_DownloadLogTypesCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (e.Error != null)
                    ShowException(e.Error);
                else
                {
                    StringReader sr = new StringReader(e.Result);
                    string line = sr.ReadLine();
                    while (line != null)
                    {
                        Dispatcher.Invoke(DispatcherPriority.Normal, new VoidDelegate(delegate
                        {
                            Button button = new Button();
                            button.Content = line;
                            button.Click += new RoutedEventHandler(logTypeButton_Click);
                            button.Margin = new Thickness(3);
                        
                            logTypesPanel.Children.Add(button);
                        }));

                        line = sr.ReadLine();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        void logTypeButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string url = @"http://taiyedbrodels.com/projects/logger/" + (string)button.Content + ".csv";
            urlTextBox.Text = url;
            Go(url);
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            Search(searchTextBox.Text);
        }

        private void Search(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                listView.ItemsSource = FullList;
                return;
            }

            FilteredList.Clear();
            query = query.ToLower();

            var results = from Item item in FullList where item.What.ToLower().Contains(query) select item;

            foreach (var item in results) FilteredList.Add((Item)item);
            listView.ItemsSource = FilteredList;            
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void FirePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Add();
        }

        protected void Add()
        {
            DateTime dtTemp;
            float amount;

            if (string.IsNullOrEmpty(whenTextBox.Text))
                dtTemp = DateTime.Now;
            else
            {
                if (!DateTime.TryParse(whenTextBox.Text, out dtTemp))
                {
                    whenTextBox.Foreground = Brushes.Red;
                    return;
                }
            }

            if (string.IsNullOrEmpty(amountTextBox.Text))
                amount = 1;
            else
            {
                if (!float.TryParse(amountTextBox.Text, out amount))
                {
                    amountTextBox.Foreground = Brushes.Red;
                    return;
                }
            }

            Item newItem = new Item { When = dtTemp, Amount = amount };
            newItem.What = whatTextBox.Text;

            FullList.Add(newItem);
            Item[] items = FullList.ToArray<Item>();
            FullList.Clear();
            foreach (Item item in from Item i in items orderby i.When select i)
                FullList.Add(item);

            Search(searchTextBox.Text);

            whenTextBox.Clear();
            amountTextBox.Clear();
            whatTextBox.Clear();

            saveButton.IsEnabled = true;
        }

        private void whenTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Add();
        }

        private void whatTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Add();
        }

        private void amountTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Add();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void Save()
        {
            try
            {
                // upload backup file with original contents
                string backupName = originalFilePath + ".bak";
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(backupName.Replace("http://", "ftp://"));
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential("u40899192", "headtrip");
                byte[] fileContents = Encoding.UTF8.GetBytes(originalFileContents);
                request.ContentLength = fileContents.Length;
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(fileContents, 0, fileContents.Length);
                requestStream.Close();
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                MessageBox.Show("Upload " + backupName + ", status " + response.StatusDescription);
                response.Close();

                // upload new file
                FtpWebRequest request2 = (FtpWebRequest)WebRequest.Create(originalFilePath.Replace("http://", "ftp://"));
                request2.Method = WebRequestMethods.Ftp.UploadFile;
                request2.Credentials = new NetworkCredential("u40899192", "headtrip");

                // create the string
                StringBuilder sb = new StringBuilder();
                foreach (Item item in FullList)
                {
                    sb.Append(item.When.ToString("yyyy-MM-dd HH:mm:ss"));
                    sb.Append(',');
                    sb.Append(item.Amount);
                    sb.Append(',');
                    string what = item.What;
                    if (what.Contains(',') || what.Contains('"'))
                        what = '"' + what.Replace("\"", "\"\"") + '"';
                    sb.Append(what);
                    sb.AppendLine();
                }

                byte[] fileContents2 = Encoding.UTF8.GetBytes(sb.ToString());
                request2.ContentLength = fileContents2.Length;
                Stream requestStream2 = request2.GetRequestStream();
                requestStream2.Write(fileContents2, 0, fileContents2.Length);
                requestStream2.Close();
                FtpWebResponse response2 = (FtpWebResponse)request2.GetResponse();
                MessageBox.Show("Upload " + originalFilePath + ", status " + response2.StatusDescription);
                response2.Close();
            }
            catch (Exception e)
            {
                ShowException(e);
            }
        }

        private void whenTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            whenTextBox.Foreground = Brushes.Black;
        }

        private void listView_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                int selectedIndex = listView.SelectedIndex;

                List<Item> itemsToRemove = new List<Item>();
                foreach (Item item in listView.SelectedItems)
                    itemsToRemove.Add(item);
                foreach (Item itemToRemove in itemsToRemove)
                    FullList.Remove(itemToRemove);

                if (listView.Items.Count > selectedIndex)
                {
                    listView.SelectedItem = listView.Items.GetItemAt(selectedIndex);
                }
                
                saveButton.IsEnabled = true;
            }
        }

        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Search(searchTextBox.Text);
        }

        private void monthlyChart_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("chart.html");
        }

        private void historyChart_Click(object sender, RoutedEventArgs e)
        {
            string[] labels = new string[FullList.Count];
            float?[] values = new float?[FullList.Count];
            float? max = 0;
            for (int ii = 0; ii < FullList.Count; ii++)
            {
                labels[ii] = FullList[ii].When.ToString();
                values[ii] = FullList[ii].Amount;

                if (values[ii] > max) max = values[ii];
            }

            createJSON(originalFileName, originalFileName, null, max.Value, 1,
                labels,
                values);
            Process.Start("chart.html");
        }

        protected void createJSON(string chartName, string yAxisLabel, string xAxisLabel, 
            float yMax, int yStep,
            string[] xLabels, float?[] values)
        {
            StringBuilder xLabelsString = new StringBuilder();
            foreach (string s in xLabels) xLabelsString.Append('"').Append(s).Append("\",");
            xLabelsString.Length--;

            StringBuilder valuesString = new StringBuilder();
            foreach (float? value in values) valuesString.Append(value.HasValue ? value.Value.ToString() : "null").Append(",");
            valuesString.Length--;

            string json = @"{
  ""title"":{
    ""text"":  """ + chartName + @""",
    ""style"": ""{font-size: 20px; color:#0000ff; font-family: Verdana; text-align: center;}""
  },
 
  ""y_legend"":{
    ""text"": """ + yAxisLabel + @""",
    ""style"": ""{color: #736AFF; font-size: 12px;}""
  },
 
  ""elements"":[
    {
      ""type"":      ""line"",
      ""alpha"":     0.5,
      ""colour"":    ""#9933CC"",
      ""text"":      ""Page views"",
      ""font-size"": 10,
      ""values"" :   [" + valuesString.ToString() +@"]
    }
  ],
 
  ""x_axis"":{
    ""stroke"":1,
    ""tick_height"":10,
    ""colour"":""#d000d0"",
    ""grid_colour"":""#00ff00"",
    ""labels"": {
        ""labels"": [" + xLabelsString.ToString() + @"]
    }
   },
 
  ""y_axis"":{
    ""stroke"":      1,
    ""tick_length"": 1,
	""steps"":	   " + yStep + @",
    ""colour"":      ""#d000d0"",
    ""grid_colour"": ""#00ff00"",
    ""offset"":      0,
    ""max"":         " + yMax + @"
  }
}";

            File.WriteAllText("data.json", json);
        }

        private void amountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            amountTextBox.Foreground = Brushes.Black;
        }
    }
}
