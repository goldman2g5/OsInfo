using System.ComponentModel;
using System.Diagnostics;

namespace WinFormsApp2
{
    public partial class Form1 : Form
    {
        private PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        private PerformanceCounter discCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
        private PerformanceCounter netCounterOut = new PerformanceCounter("Network Interface", "Bytes Sent/sec");
        private PerformanceCounter netCounterIn = new PerformanceCounter("Network Interface", "Bytes Received/sec");
        private List<double> cpuCounterAvg = new List<double>();
        private List<double> gpuCounterAvg = new List<double>();
        private List<double> discCounterAvg = new List<double>();
        private List<double> ramCounterAvg = new List<double>();
        private List<double> netCounterOutAvg = new List<double>();
        private List<double> netCounterInAvg = new List<double>();

        public static List<PerformanceCounter> GetGPUCounters()
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var counterNames = category.GetInstanceNames();

            var gpuCounters = counterNames
                                .Where(counterName => counterName.EndsWith("engtype_3D"))
                                .SelectMany(counterName => category.GetCounters(counterName))
                                .Where(counter => counter.CounterName.Equals("Utilization Percentage"))
                                .ToList();

            return gpuCounters;
        }

        public static float GetGPUUsage(List<PerformanceCounter> gpuCounters)
        {
            gpuCounters.ForEach(x => x.NextValue());
            var result = gpuCounters.Sum(x => x.NextValue());

            return result;
        }

        public Form1()
        {
            InitializeComponent();
            timer1.Tick += new EventHandler(timer1_Tick);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var counterNames = category.GetInstanceNames();
            PerformanceCounterCategory performanceCounterCategory = new PerformanceCounterCategory("Network Interface");
            InstanceDataCollectionCollection data = performanceCounterCategory.ReadCategory();
            var interfaces = performanceCounterCategory.GetInstanceNames();
            var dataSentCounters = new List<PerformanceCounter>();
            var dataReceivedCounters = new List<PerformanceCounter>();
            float sendSum = 0;
            float receiveSum = 0;

            var gpuCounters = counterNames
                                .Where(counterName => counterName.EndsWith("engtype_3D"))
                                .SelectMany(counterName => category.GetCounters(counterName))
                                .Where(counter => counter.CounterName.Equals("Utilization Percentage"))
                                .ToList();
            gpuCounters.ForEach(x => x.NextValue());
            for (int i = 0; i < performanceCounterCategory.GetInstanceNames().Length; i++)
            {
                dataReceivedCounters.Add(new PerformanceCounter("Network Interface", "Bytes Received/sec", interfaces[i]));
                dataSentCounters.Add(new PerformanceCounter("Network Interface", "Bytes Sent/sec", interfaces[i]));
                sendSum += dataSentCounters[i].NextValue();
                receiveSum += dataReceivedCounters[i].NextValue();
            }
            cpuCounterAvg.Add(cpuCounter.NextValue());
            gpuCounterAvg.Add(gpuCounters.Sum(x => x.NextValue()));
            discCounterAvg.Add(discCounter.NextValue());
            ramCounterAvg.Add(ramCounter.NextValue());
            netCounterInAvg.Add((double)dataReceivedCounters[0].NextValue() / 1024 / 1024);
            netCounterOutAvg.Add((double)dataSentCounters[0].NextValue() / 1024 / 1024);


            foreach (List<double> i in new List<List<double>> { cpuCounterAvg, gpuCounterAvg, discCounterAvg, netCounterOutAvg, netCounterInAvg, ramCounterAvg })
            {
                if (i.Count >= 10)
                {
                    i.RemoveAt(0);
                }
            }

            List<string> ls = new List<string>
            {
                $"Версия системы: {Environment.OSVersion}",
                $"Текущий пользователь: {Environment.UserName}",
                $"Время работы системы: {Environment.TickCount / 3600000 % 24} часов {Environment.TickCount / 120000 % 60} минут {Environment.TickCount / 1000 % 60} секунд",
                $"Марка и модель пк: {Environment.MachineName}",
                $"Модель cpu: {Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")}",
                $"Загруженость cpu: {Math.Round(cpuCounterAvg.Average(), 2)} %",
                $"Загруженость gpu: {Math.Round(gpuCounterAvg.Average(), 2)}%",
                $"Загруженость диска: {Math.Round(discCounterAvg.Average(), 2)}%",
                $"Доступно ram: {Math.Round(ramCounterAvg.Average(), 2)} MB",
                $"Сеть: In {Math.Round(netCounterInAvg.Average(), 2)}MB/sec | Out {Math.Round(netCounterOutAvg.Average(), 2)}MB/sec  | {netCounterInAvg.Count}"
            };

            

            foreach (DriveInfo i in DriveInfo.GetDrives())
            {
                ls.Add($"Диск {i.Name} {Math.Round((double)i.AvailableFreeSpace / 1024 / 1024 / 1024)}/{Math.Round((double)i.TotalSize / 1024 / 1024 / 1024)} ГБ доступно");
            }
            richTextBox1.Lines = ls.ToArray();
        }
    }
}