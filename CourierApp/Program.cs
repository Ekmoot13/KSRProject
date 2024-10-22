using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CourierApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost" };


            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // Deklaracja kolejki kurierskiej
                channel.QueueDeclare(queue: "courierQueue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine("Serwis kurierski otrzymał produkty: {0}", message);
                };

                channel.BasicConsume(queue: "courierQueue",
                                     autoAck: true,
                                     consumer: consumer);

                Console.WriteLine("Serwis kurierski nasłuchuje...");
                Console.ReadLine();
            }
        }
    }
}
