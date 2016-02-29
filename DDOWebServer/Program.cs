using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DDODatabase;
namespace SimpleWebServer {
    class Program {
        public static ATeamDB db = new ATeamDB();
        static string[] files = new string[]
        {
            "About/index.html",
            "Rules/index.html",
            "Stats/index.html"
        };
        static void Main(string[] args) {
            var prefixes = new string[]
            {
                "http://localhost/"
            };


            if (!HttpListener.IsSupported) {
                Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");
            var listener = new HttpListener();
            foreach (string s in prefixes) {
                listener.Prefixes.Add(s);
            }
            Console.WriteLine("WEB SERVER...");
            new Thread(new ParameterizedThreadStart(About)).Start();
            new Thread(new ParameterizedThreadStart(Rules)).Start();
            new Thread(new ParameterizedThreadStart(Stats)).Start();
        }
        static void About(object arg) {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost/About/");
            while (true) {
                Console.WriteLine("/About listening...");
                listener.Start();
                var context = listener.GetContext();
                Console.WriteLine("New /About visitor: " + context.Request.RemoteEndPoint.Address.ToString());
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                string responseString = File.ReadAllText(files[0]);
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                using (var output = response.OutputStream) {
                    output.Write(buffer, 0, buffer.Length);
                }
                listener.Stop();
            }
        }
        static void Rules(object arg) {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost/Rules/");
            while (true) {
                Console.WriteLine("/Rules listening...");
                listener.Start();
                var context = listener.GetContext();
                Console.WriteLine("New /Rules visitor: " + context.Request.RemoteEndPoint.Address.ToString());
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                string responseString = File.ReadAllText(files[1]);
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                using (var output = response.OutputStream) {
                    output.Write(buffer, 0, buffer.Length);
                }
                listener.Stop();
            }
        }
        static void Stats(object arg) {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost/Stats/");
            while (true) {
                Console.WriteLine("/Stats listening...");
                listener.Start();
                var context = listener.GetContext();
                Console.WriteLine("New /Stats visitor: " + context.Request.RemoteEndPoint.Address.ToString());
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                string responseString = $"<html><body><h2>Stats</h2><ul>";
                foreach (var stat in db.Stats) {
                    responseString += $"<li>{stat.Name}: {stat.Value}</li>";
                }
                responseString += "</ul></body></html>";

                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                using (var output = response.OutputStream) {
                    output.Write(buffer, 0, buffer.Length);
                }
                listener.Stop();
            }
        }
    }
}