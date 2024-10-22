using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BackendApp
{
    public class ShoppingService
    {
        // Lista do przechowywania produktów
        private static List<string> cart = new List<string>();

        // Metoda do dodawania produktów
        public void AddToCart(string message)
        {
            cart.Add(message);
        }

        // Metoda do pobierania wszystkich produktów
        public List<string> GetCart()
        {
            return cart;
        }

        // Metoda do wyczyszczenia wszystkich produktów
        public void ClearCart()
        {
            cart.Clear();
            Console.WriteLine("Wiadomości zostały wyczyszczone.");
        }
    }

    class Program
    {
        static ShoppingService shoppingService = new ShoppingService();


        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost" };

            try
            {
                // Próba nawiązania połączenia z RabbitMQ
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    // Fail-fast: Jeśli połączenie się nie uda, natychmiast wyświetl błąd
                    Console.WriteLine("Połączono z RabbitMQ");

                    try
                    {
                        // Deklaracja kolejki komend
                        channel.QueueDeclare(queue: "commandQueue",
                                             durable: false,
                                             exclusive: false,
                                             autoDelete: false,
                                             arguments: null);

                        // Deklaracja kolejki odpowiedzi
                        channel.QueueDeclare(queue: "responseQueue",
                                             durable: false,
                                             exclusive: false,
                                             autoDelete: false,
                                             arguments: null);

                        var consumer = new EventingBasicConsumer(channel);
                        consumer.Received += (model, ea) =>
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);

                            // Obsługa wiadomości
                            if (message == "checkCart")
                            {
                                // Przygotowanie odpowiedzi
                                var response = string.Join(";", shoppingService.GetCart());
                                var responseBody = Encoding.UTF8.GetBytes(response);

                                // Wysłanie odpowiedzi do responseQueue
                                channel.BasicPublish(exchange: "",
                                                     routingKey: "responseQueue",
                                                     basicProperties: null,
                                                     body: responseBody);

                                Console.WriteLine("Wysłano odpowiedź z listą zakupów");
                            }
                            else if (message == "sendAllProducts")
                            {
                                ForwardToCourierService();
                            }
                            else
                            {
                                // Dodanie nowej wiadomości
                                shoppingService.AddToCart(message);
                                Console.WriteLine("Dodano: {0}", message);
                            }
                        };

                        channel.BasicConsume(queue: "commandQueue",
                                             autoAck: true,
                                             consumer: consumer);

                        Console.WriteLine("BackendApp nasłuchuje na wiadomości...");
                        Console.ReadLine();
                    }
                    catch (Exception ex)
                    {
                        // Fail-fast: Jeśli nie uda się stworzyć kolejki lub obsłużyć wiadomości, rzuć błąd
                        Console.WriteLine("Błąd podczas tworzenia lub używania kolejki: " + ex.Message);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                // Fail-fast: Błąd przy połączeniu z RabbitMQ
                Console.WriteLine("Nie udało się połączyć z RabbitMQ: " + ex.Message);
                throw;
            }
        }

        // Przesyłanie wiadomości do serwisu kurierskiego
        private static void ForwardToCourierService()
        {
            var factory = new ConnectionFactory() { HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost" };
            try
            {
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    // Deklaracja kolejki kurierskiej
                    channel.QueueDeclare(queue: "courierQueue",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    var messages = shoppingService.GetCart();
                    var allProducts = string.Join(";", messages);

                    var body = Encoding.UTF8.GetBytes(allProducts);

                    // Wysłanie wszystkich produktów do serwisu kurierskiego
                    channel.BasicPublish(exchange: "",
                                         routingKey: "courierQueue",
                                         basicProperties: null,
                                         body: body);

                    Console.WriteLine("Wysłano produkty do serwisu kurierskiego.");

                    // Wyczyszczenie listy wiadomości po wysłaniu produktów
                    shoppingService.ClearCart();
                }
            }
            catch (Exception ex)
            {
                // Fail-fast: Błąd przy wysyłaniu wiadomości do serwisu kurierskiego
                Console.WriteLine("Błąd przy wysyłaniu produktów do serwisu kurierskiego: " + ex.Message);
                throw;
            }
        }

    }
}