using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml.Serialization;
using ClientConsole.Model;

namespace ClientConsole;

class Program
{
    static ConcurrentQueue<Quote> quoteQueue = new();

    static long count = 0;
    static double mean = 0;
    static double m2 = 0;
    static long? lastId = null;
    static long lostPackets = 0;
    static double? mode = null;
    static Dictionary<double, int> frequencyMap = new();

    static PriorityQueue<double, double> minHeap = new();
    static PriorityQueue<double, double> maxHeap = new(Comparer<double>.Create((a, b) => b.CompareTo(a)));

    const long MAX_QUEUE_SIZE = 1_000_000_000_000;//for testing

    private static void Main()
    {
        var config = LoadConfig("client_config.xml");

        var receiverThread = new Thread(() => StartReceiving(config))
        {
            IsBackground = true
        };
        receiverThread.Start();

        StartStatisticsWorker();

        Console.WriteLine("[Client] Running. Press Enter to print stats.");
        while (true)
        {
            Console.ReadLine();
            PrintStatistics();
        }
    }

    static AppConfig LoadConfig(string path)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(AppConfig));
        using var stream = File.OpenRead(path);
        return (AppConfig)serializer.Deserialize(stream);
    }

    static void StartReceiving(AppConfig config)
    {
        try
        {
            using var udpClient = new UdpClient();

            var localEp = new IPEndPoint(IPAddress.Any, config.Multicast.Port);
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(localEp);

            var multicastAddr = IPAddress.Parse(config.Multicast.Address);
            udpClient.JoinMulticastGroup(multicastAddr);

            Console.WriteLine($"[Receiver] Joined {config.Multicast.Address}:{config.Multicast.Port}");

            while (true)
            {
                var result = udpClient.Receive(ref localEp);

                if (result.Length >= 16)
                {
                    long id = BitConverter.ToInt64(result, 0);
                    double value = BitConverter.ToDouble(result, 8);

                    if (quoteQueue.Count < MAX_QUEUE_SIZE)
                    {
                        quoteQueue.Enqueue(new Quote { Id = id, Value = value });
                    }
                    else
                    {
                        Interlocked.Increment(ref lostPackets);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Receiver Error] {ex.Message}");
            Thread.Sleep(1000);
            //that need to restart logic or reconnect
        }
    }

    static void StartStatisticsWorker()
    {
        var statThread = new Thread(() =>
        {
            try
            {
                while (true)
                {
                    while (quoteQueue.TryDequeue(out var quote))
                    {
                        UpdateStatistics(quote);
                    }

                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Statistics Error] {ex.Message}");
            }
        });

        statThread.IsBackground = true;
        statThread.Start();
    }

    static void UpdateStatistics(Quote q)
    {
        if (lastId.HasValue && q.Id > lastId + 1)
            lostPackets += q.Id - lastId.Value - 1;

        lastId = q.Id;
        count++;

        //Welford(mean and StdDev)
        double delta = q.Value - mean;
        mean += delta / count;
        m2 += delta * (q.Value - mean);

        //Mode
        if (frequencyMap.TryGetValue(q.Value, out var current))
            frequencyMap[q.Value] = current + 1;
        else
            frequencyMap[q.Value] = 1;

        if (mode == null || frequencyMap[q.Value] > frequencyMap[mode.Value])
            mode = q.Value;

        if (maxHeap.Count == 0 || q.Value <= maxHeap.Peek())
            maxHeap.Enqueue(q.Value, q.Value);
        else
            minHeap.Enqueue(q.Value, q.Value);

        if (maxHeap.Count > minHeap.Count + 1)
            minHeap.Enqueue(maxHeap.Dequeue(), 0);
        else if (minHeap.Count > maxHeap.Count)
            maxHeap.Enqueue(minHeap.Dequeue(), 0);
    }

    static void PrintStatistics()
    {
        Console.WriteLine("\n--- Statistics ---");
        Console.WriteLine($"Count: {count}");
        Console.WriteLine($"Mean: {mean:F2}");

        double stddev = count > 1 ? Math.Sqrt(m2 / (count - 1)) : 0;
        Console.WriteLine($"StdDev: {stddev:F2}");

        if (maxHeap.Count == minHeap.Count && maxHeap.Count > 0)
            Console.WriteLine($"Median: {(maxHeap.Peek() + minHeap.Peek()) / 2:F2}");
        else if (maxHeap.Count > 0)
            Console.WriteLine($"Median: {maxHeap.Peek():F2}");
        else
            Console.WriteLine("Median: n/a");

        Console.WriteLine($"Mode: {mode?.ToString("F2") ?? "n/a"}");
        Console.WriteLine($"Lost Packets: {lostPackets}");
        Console.WriteLine("------------------\n");
    }
}