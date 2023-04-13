using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using ModelsLib;


List<Car> cars = null!;


string path = "Cars.json";

if (File.Exists(path))
    cars = JsonSerializer.Deserialize<List<Car>>(File.ReadAllText(path))!;
cars ??= new();


var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 45678);
listener.Start(10);


while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    var sync = new object();

    Console.WriteLine($"Client {client.Client.RemoteEndPoint} accepted");

    new Task(() =>
    {
        var stream = client.GetStream();
        var bw = new BinaryWriter(stream);
        var br = new BinaryReader(stream);

        while (true)
        {
            var jsonStr = br.ReadString();
            var command = JsonSerializer.Deserialize<Command>(jsonStr);

            if (command is null)
                continue;

            switch (command.Method)
            {
                case MyHttpMethod.GET:
                    {
                        var id = command.Car?.Id;
                        if (id == 0)
                        {
                            var carsJson = JsonSerializer.Serialize(cars);
                            bw.Write(carsJson);
                            break;
                        }

                        Car? car = null;
                        foreach (var c in cars)
                        {
                            if (c.Id == id)
                            {
                                car = c;
                                break;
                            }
                        }

                        var jsonResponse = JsonSerializer.Serialize(car);
                        bw.Write(jsonResponse);
                        break;
                    }
                case MyHttpMethod.POST:
                    {
                        var id = command.Car?.Id;
                        var canBePosted = true;
                        foreach (var c in cars)
                        {
                            if (c.Id == id)
                            {
                                canBePosted = false;
                                break;
                            }
                        }

                        if (canBePosted)
                        {
                            if (command.Car is not null)
                                lock (sync)
                                {
                                    cars.Add(command.Car);
                                }
                        }

                        bw.Write(canBePosted);

                        break;
                    }
                case MyHttpMethod.PUT:
                    {
                        var id = command.Car?.Id;
                        var insertIndex = -1;
                        var canBePuted = false;
                        foreach (var c in cars)
                        {
                            if (c.Id == id)
                            {
                                canBePuted = true;
                                lock (sync)
                                {
                                    insertIndex = cars.IndexOf(c);
                                }
                                cars.Remove(c);
                                break;
                            }
                        }

                        if (canBePuted)
                        {
                            if (command.Car is not null)
                                lock (sync)
                                {
                                    cars.Insert(insertIndex, command.Car);
                                }
                        }

                        bw.Write(canBePuted);

                        break;
                    }

                case MyHttpMethod.DELETE:
                    {
                        var deleted = false;
                        var id = command.Car?.Id;
                        foreach (var c in cars)
                        {
                            if (c.Id == id)
                            {
                                lock (sync)
                                {
                                    cars.Remove(c);
                                }
                                deleted = true;
                                break;
                            }
                        }
                        bw.Write(deleted);
                        break;
                    }
            }

            lock (sync)
            {
                var jsonCars = JsonSerializer.Serialize(cars);
                File.WriteAllTextAsync(path, jsonCars);
            }
        }
    }).Start();
}