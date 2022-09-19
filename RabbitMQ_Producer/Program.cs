using Models;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ_Producer
{
    class Program
    {
        static IMqClient client = MqClient.FromConfig("MqConfig.json");

        static ExchangeModes mode = ExchangeModes.DLX;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Trace.Listeners.Add(new ConsoleTraceListener() { TraceOutputOptions = TraceOptions.None });
            try
            {
                TestRpcAsync().Wait();
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UnhandledException", $"{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            System.IO.File.WriteAllText(path, e.ExceptionObject.ToString());
        }

        static void TestHello()
        {
            using (var producer = client.CreateProducer<string>("hello", mode))
            {
                producer.Publish("hello world");
                producer.Publish("hello consumer1");
                producer.Publish("hello consumer2");
                producer.Publish("hello consumer3");
                producer.Publish("hello consumer4");
                producer.Publish("hello consumer5");
                Console.ReadLine();
            }
        }

        static void TestNumber()
        {
            using (var producer = client.CreateProducer<double>("num", mode))
            {
                producer.Publish(1);
                producer.Publish(2);
                producer.Publish(3);
                producer.Publish(4);
                producer.Publish(5.5);
                producer.Publish(6.7891011121314151617181920);
                Console.ReadLine();
            }
        }

        static void TestEnter()
        {
            using (var producer = client.CreateProducer<EnterApply>("enter", mode))
            {
                producer.Publish(new EnterApply { CarNumber = "粤B123456", EnterTime = DateTime.Today.Add(new TimeSpan(8, 0, 0)) });
                producer.Publish(new EnterApply { CarNumber = "粤B888888", EnterTime = DateTime.Today.Add(new TimeSpan(12, 0, 0)) });
                producer.Publish(new EnterApply { CarNumber = "粤Z999", EnterTime = DateTime.Today.Add(new TimeSpan(21, 0, 0)) });
                Console.ReadLine();
            }
        }

        static void TestExit()
        {
            using (var producer = client.CreateProducer<ExitApply>("exit", mode))
            {
                producer.Publish(new ExitApply { CarNumber = "粤B123456", EnterTime = DateTime.Today.Add(new TimeSpan(8, 0, 0)), ExitTime = DateTime.Today.Add(new TimeSpan(9, 30, 0)) });
                producer.Publish(new ExitApply { CarNumber = "粤B888888", EnterTime = DateTime.Today.Add(new TimeSpan(12, 0, 0)), ExitTime = DateTime.Today.Add(new TimeSpan(16, 0, 0)) });
                producer.Publish(new ExitApply { CarNumber = "粤Z999", EnterTime = DateTime.Today.Add(new TimeSpan(21, 0, 0)), ExitTime = DateTime.Today.Add(new TimeSpan(21, 10, 0)) });
                Console.ReadLine();
            }
        }

        static void TestLoop()
        {
            using (var producer = client.CreateProducer<int>("loop", mode))
            {
                for (int i = 1; i <= 100; i++)
                {
                    producer.Publish(i);
                    Console.WriteLine(i);
                    System.Threading.Thread.Sleep(100);
                }
                Console.ReadLine();
            }
        }

        static void TestParallel()
        {
            using (var producer = client.CreateProducer("parallel", mode))
            {
                for (int i = 1; i <= 100; i++)
                {
                    string message = "parallel_" + i;
                    Console.WriteLine(message);
                    producer.Publish(message);
                }
                Console.ReadLine();
            }
        }

        static void TestRepeat()
        {
            using (var producer = client.CreateProducer<int>("repeat", mode))
            {
                for (int i = 1; i <= 30; i++)
                {
                    producer.Publish(i);
                    Console.WriteLine($"Published:{i}");
                    System.Threading.Thread.Sleep(300);
                }
                Console.ReadLine();
            }
        }

        static async Task TestRpcAsync()
        {
            using (IRemoteProcedure<int, double> rp = client.CreateRemoteProcedure<int, double>("rpc", mode))
            {
                for (int i = 1; i <= 10; i++)
                {
                    Console.WriteLine($"call: {i}");
                    double result = await rp.CallAsync(i);
                    Console.WriteLine($"result: {result}");
                }
            }
        }
    }
}
