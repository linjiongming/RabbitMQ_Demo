using Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ_Consumer
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
                TestRpc();
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
            using (var consumer = client.CreateConsumer<string>("hello", mode))
            {
                consumer.Subscribe(message =>
                {
                    Console.WriteLine(message.Value);
                    System.Threading.Thread.Sleep(300);
                });
                Console.ReadLine();
            }
        }

        static void TestParking03()
        {
            using (var consumer = client.CreateConsumer("parking03", mode, queueType: QueueTypes.Quorum))
            {
                consumer.Subscribe(x => Console.WriteLine(x.Content));
                Console.ReadLine();
            }
        }

        static void TestNumber()
        {
            using (var consumer = client.CreateConsumer<double>("num", mode))
            {
                consumer.Subscribe(message =>
                {
                    Console.WriteLine(message.Value);
                    System.Threading.Thread.Sleep(300);
                });
                Console.ReadLine();
            }
        }

        static void TestEnter()
        {
            using (var consumer = client.CreateConsumer<EnterApply>("enter", mode))
            {
                consumer.Subscribe(message =>
                {
                    Console.WriteLine(message.Value);
                    System.Threading.Thread.Sleep(300);
                });
                Console.ReadLine();
            }
        }

        static void TestExit()
        {
            using (var consumer = client.CreateConsumer<ExitApply>("exit", mode))
            {
                consumer.Subscribe(message =>
                {
                    Console.WriteLine(message.Value);
                    System.Threading.Thread.Sleep(300);
                });
                Console.ReadLine();
            }
        }

        static void TestLoop()
        {
            using (var consumer = client.CreateConsumer<int>("loop", mode))
            {
                consumer.Subscribe(message =>
                {
                    Console.WriteLine(message.Value);
                    System.Threading.Thread.Sleep(100);
                });
                Console.ReadLine();
            }
        }

        static void TestParallel()
        {
            using (var consumer1 = client.CreateConsumer("parallel", mode))
            using (var consumer2 = client.CreateConsumer("parallel", mode))
            using (var consumer3 = client.CreateConsumer("parallel", mode))
            using (var consumer4 = client.CreateConsumer("parallel", mode))
            {
                List<string> list = new List<string>();
                int count1 = 0, count2 = 0, count3 = 0, count4 = 0;
                consumer1.Subscribe(message =>
                {
                    Console.WriteLine($"consumer1:{message.Content}");
                    lock (list) { list.Add(message.Content); }
                    count1++;
                    System.Threading.Thread.Sleep(300);
                });
                consumer2.Subscribe(message =>
                {
                    Console.WriteLine($"consumer2:{message.Content}");
                    lock (list) { list.Add(message.Content); }
                    count2++;
                    System.Threading.Thread.Sleep(300);
                });
                consumer3.Subscribe(message =>
                {
                    Console.WriteLine($"consumer3:{message.Content}");
                    lock (list) { list.Add(message.Content); }
                    count3++;
                    System.Threading.Thread.Sleep(300);
                });
                consumer4.Subscribe(message =>
                {
                    Console.WriteLine($"consumer4:{message.Content}");
                    lock (list) { list.Add(message.Content); }
                    count4++;
                    System.Threading.Thread.Sleep(300);
                });
                Console.ReadLine();
                Console.WriteLine($"count1:{count1}");
                Console.WriteLine($"count2:{count2}");
                Console.WriteLine($"count3:{count3}");
                Console.WriteLine($"count4:{count4}");
                Console.WriteLine($"list count:{list.Count}");
                Console.WriteLine($"list any repeat:{list.Distinct().Count() < list.Count}");
                Console.ReadLine();
            }
        }

        static void TestRepeat()
        {
            using (var consumer1 = client.CreateConsumer<int>("repeat", mode))
            using (var consumer2 = client.CreateConsumer<int>("repeat", mode))
            using (var consumer3 = client.CreateConsumer<int>("repeat", mode))
            {
                consumer1.Subscribe(message =>
                {
                    Console.WriteLine($"consumer1 received:{message.Value}");
                    System.Threading.Thread.Sleep(300);
                    if (message.Value > 10) throw new Exception("模拟消息处理异常");
                });
                consumer2.Subscribe(message =>
                {
                    Console.WriteLine($"consumer2 received:{message.Value}");
                    System.Threading.Thread.Sleep(300);
                    if (message.Value > 10) throw new Exception("模拟消息处理异常");
                });
                consumer3.Subscribe(message =>
                {
                    Console.WriteLine($"consumer3 received:{message.Value}");
                    System.Threading.Thread.Sleep(300);
                    if (message.Value > 10) throw new Exception("模拟消息处理异常");
                });
                Console.ReadLine();
            }
        }

        static void TestDeadLetterQueue(string routingKey)
        {
            try
            {
                using (IMessageQueue<int> queue = client.GetMessageQueue<int>($"{routingKey}.failed"))
                {
                    var one = queue.ElementAt(3);
                    Console.WriteLine(one.Value);
                    foreach (IMessage<int> message in queue)
                    {
                        Console.WriteLine("第一次：" + message.Value);
                    }
                    foreach (IMessage<int> message in queue)
                    {
                        Console.WriteLine("第二次：" + message.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.ReadLine();
        }

        static void TestRpc()
        {
            using (IRemoteProcedure<int, double> rp = client.CreateRemoteProcedure<int, double>("rpc", mode))
            {
                rp.Answer(message =>
                {
                    Console.WriteLine($"receive > id:{message.CorrelationId}, value:{message.Value}");
                    double result = message.Value + 0.1;
                    Console.WriteLine($"calculate > result: {result}");
                    return result;
                });
                Console.ReadKey();
            }
        }
    }
}
