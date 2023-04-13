using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ModelsLib;

namespace Client
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
            requestCommand = new Command();
            Car = new();
            Cars = new();
            client = new TcpClient("127.0.0.1", 45678);

        }



        Command requestCommand;
        public ObservableCollection<Car> Cars { get; set; }
        private TcpClient client;

        public Car Car { get; set; }

        //public Car Car
        //{
        //    get { return (Car)GetValue(carProperty); }
        //    set { SetValue(carProperty, value); }
        //}
        //public static readonly DependencyProperty carProperty =
        //    DependencyProperty.Register("Car", typeof(Car), typeof(MainWindow), new PropertyMetadata(0));




        private void cb_command_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_command.SelectedItem is MyHttpMethod method)
            {
                requestCommand.Method = method;

                switch (method)
                {
                    case MyHttpMethod.GET:
                    case MyHttpMethod.DELETE:
                        foreach (var txt in grid.Children.OfType<TextBox>())
                        {
                            if (txt.Name != "txt_id")
                                txt.Text = string.Empty;
                        }
                        break;
                    case MyHttpMethod.POST:
                    case MyHttpMethod.PUT:
                        break;
                }

            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (cb_command.SelectedItem is null)
            {
                MessageBox.Show("Please select command");
                return;
            }

            if (cb_command.SelectedItem is MyHttpMethod method)
                ExecuteServerCommand(method);

            txt_id.Text = "0";
            txt_make.Text = "";
            txt_model.Text = "";
            txt_vin.Text = "";
            txt_year.Text = "";
            txt_color.Text = "";
        }


        private void Window_Loaded(object sender, RoutedEventArgs e) =>
            cb_command.ItemsSource = Enum.GetValues(typeof(MyHttpMethod)).Cast<MyHttpMethod>();


        private async void ExecuteServerCommand(MyHttpMethod method)
        {

            var stream = client.GetStream();
            var bw = new BinaryWriter(stream);
            var br = new BinaryReader(stream);

            switch (method)
            {
                case MyHttpMethod.GET:
                    {
                        if (Car.Id < 0)
                        {
                            MessageBox.Show($"Id \"{Car.Id}\" is invalid");
                            return;
                        }

                        requestCommand.Car = Car;
                        var jsonStr = JsonSerializer.Serialize(requestCommand);



                        bw.Write(jsonStr);

                        await Task.Delay(50);

                        if (Car.Id == 0)
                        {
                            var jsonCars = br.ReadString();
                            var cars = JsonSerializer.Deserialize<List<Car>>(jsonCars);
                            Cars.Clear();
                            foreach (var c in cars!)
                                Cars.Add(c);

                            return;
                        }

                        var jsonResponse = br.ReadString();
                        var car = JsonSerializer.Deserialize<Car>(jsonResponse);

                        if (car != null)
                        {
                            Cars.Clear();
                            Cars.Add(car);
                        }
                        else
                        {
                            MessageBox.Show($"No car with id: \"{Car.Id}\"");
                            Cars.Clear();
                        }

                        break;
                    }
                case MyHttpMethod.POST:
                    {

                        var sb = new StringBuilder();

                        if (Car.Id <= 0)
                            sb.Append($"Id \"{Car.Id}\" is invalid");
                        if (Car.Year < 1960 || Car.Year > DateTime.Now.Year)
                            sb.Append($"Year \"{Car.Year}\" is invalid");

                        if (string.IsNullOrWhiteSpace(Car.Make)
                            || string.IsNullOrWhiteSpace(Car.Model)
                            || string.IsNullOrWhiteSpace(Car.VIN)
                            || string.IsNullOrEmpty(Car.Color))
                            sb.Append("Please fill in every blank");

                        if (sb.Length > 0)
                        {
                            MessageBox.Show(sb.ToString());
                            return;
                        }

                        requestCommand.Car = Car;
                        var jsonStr = JsonSerializer.Serialize(requestCommand);

                        bw.Write(jsonStr);

                        await Task.Delay(50);

                        var isPosted = br.ReadBoolean();
                        var resultText = string.Empty;

                        if (isPosted)
                            resultText = "Added succesfully";
                        else
                            resultText = $"Car with id \"{Car.Id}\" already exists";

                        MessageBox.Show(resultText);
                        Cars.Clear();

                        break;
                    }
                case MyHttpMethod.PUT:
                    {
                        var sb = new StringBuilder();

                        if (Car.Id <= 0)
                            sb.Append($"Id \"{Car.Id}\" is invalid");
                        if (Car.Year < 1960 || Car.Year > DateTime.Now.Year)
                            sb.Append($"Year \"{Car.Year}\" is invalid");

                        if (string.IsNullOrWhiteSpace(Car.Make)
                            || string.IsNullOrWhiteSpace(Car.Model)
                            || string.IsNullOrWhiteSpace(Car.VIN)
                            || string.IsNullOrEmpty(Car.Color))
                            sb.Append("Please fill in every blank");

                        if (sb.Length > 0)
                        {
                            MessageBox.Show(sb.ToString());
                            return;
                        }

                        requestCommand.Car = Car;
                        var jsonStr = JsonSerializer.Serialize(requestCommand);


                        bw.Write(jsonStr);

                        await Task.Delay(50);

                        var isPosted = br.ReadBoolean();
                        var resultText = string.Empty;

                        if (isPosted)
                            resultText = "Updated succesfully";
                        else
                            resultText = $"Car with id \"{Car.Id}\" doesn't exists";

                        MessageBox.Show(resultText);
                        Cars.Clear();

                        break;
                    }
                case MyHttpMethod.DELETE:
                    {
                        if (Car.Id <= 0)
                        {
                            MessageBox.Show($"Id \"{Car.Id}\" is invalid");
                            return;
                        }

                        requestCommand.Car = Car;
                        var jsonStr = JsonSerializer.Serialize(requestCommand);

                        bw.Write(jsonStr);

                        await Task.Delay(50);

                        var isDeleted = br.ReadBoolean();

                        var resultText = string.Empty;

                        if (isDeleted)
                            resultText = "Deleted succesfully";
                        else
                            resultText = $"Car with id \"{Car.Id}\" not found";

                        MessageBox.Show(resultText);
                        Cars.Clear();
                        break;
                    }
            }
        }

    }
}
