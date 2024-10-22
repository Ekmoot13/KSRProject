using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Web;
using System.Web.Mvc;
using System.Text;
using RabbitMQ.Client;
using System.Net.Http;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace WbApp.Controllers
{
    public class HomeController : Controller
    {

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        } 
        
        public ActionResult Index()
        {
            return View();
        }

        // Metoda do wysyłania wiadomości
        [HttpPost]
        public ActionResult SendProduct(string product)
        {
            // Konfiguracja RabbitMQ
            var factory = new ConnectionFactory() { HostName = "localhost" }; 
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // Deklaracja kolejki
                channel.QueueDeclare(queue: "commandQueue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var body = Encoding.UTF8.GetBytes(product);

                // Publikowanie wiadomości do kolejki
                channel.BasicPublish(exchange: "",
                                     routingKey: "commandQueue",
                                     basicProperties: null,
                                     body: body);

                Console.WriteLine("Wysłano wiadomość: {0}", product);
            }

            ViewBag.Message = "Product został dodany!";
            return View("Index");
        }

        // Metoda do sprawdzenia wysłanych wiadomości (zapytanie do BackendApp)
        [HttpPost]
        public ActionResult CheckCart()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // Deklaracja kolejki dla odpowiedzi
                channel.QueueDeclare(queue: "responseQueue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                // Wysłanie zapytania do kolejki commandQueue o listę wiadomości
                var body = Encoding.UTF8.GetBytes("checkCart");

                channel.BasicPublish(exchange: "",
                                     routingKey: "commandQueue",
                                     basicProperties: null,
                                     body: body);

                Console.WriteLine("Wysłano zapytanie o wiadomości");

                // Nasłuchiwanie na odpowiedzi w responseQueue
                var consumer = new EventingBasicConsumer(channel);
                var receivedCart = "";

                consumer.Received += (model, ea) =>
                {
                    var responseBody = ea.Body.ToArray();
                    var responseProducts = Encoding.UTF8.GetString(responseBody);
                    receivedCart = responseProducts;
                };

                channel.BasicConsume(queue: "responseQueue",
                                     autoAck: true,
                                     consumer: consumer);

                // Czekaj, aby zebrać odpowiedzi 
                Task.Delay(500).Wait();

                ViewBag.SentCart = receivedCart.Split(';');
            }

            return View("Index");
        }

        [HttpPost]
        public ActionResult SendAllProducts()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // Wysłanie komendy do BackendApp o wysyłkę wszystkich produktów
                var body = Encoding.UTF8.GetBytes("sendAllProducts");

                channel.BasicPublish(exchange: "",
                                     routingKey: "commandQueue",
                                     basicProperties: null,
                                     body: body);

                Console.WriteLine("Wysłano komendę o wysłanie wszystkich produktów.");
            }

            ViewBag.Message = "Wysłano komendę do wysyłki wszystkich produktów.";
            return View("Index");
        }

    }
}