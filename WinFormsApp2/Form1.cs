using System.Diagnostics;
using System.Management;

namespace WinFormsApp2
{
    public partial class Form1 : Form
    {
        private static PerformanceCounterCategory category = new("GPU Engine");
        private static PerformanceCounterCategory performanceCounterCategory = new("Network Interface");
        private static string[]? interfaces = performanceCounterCategory.GetInstanceNames();
        private static string[]? counterNames = category.GetInstanceNames();
        private List<PerformanceCounter> dataSentCounters = new();
        private List<PerformanceCounter> dataReceivedCounters = new();
        private PerformanceCounter cpuCounter = new("Processor", "% Processor Time", "_Total");
        private PerformanceCounter ramCounter = new("Memory", "Available MBytes");
        private PerformanceCounter discCounter = new("PhysicalDisk", "% Disk Time", "_Total");
        private static List<PerformanceCounter> gpuCounters = counterNames.Where(counterName => counterName.EndsWith("engtype_3D")).SelectMany(counterName => category.GetCounters(counterName)).Where(counter => counter.CounterName.Equals("Utilization Percentage")).ToList();
        private List<double> cpuCounterAvg = new();
        private List<double> gpuCounterAvg = new();
        private List<double> discCounterAvg = new();
        private List<double> ramCounterAvg = new();
        private List<double> netCounterOutAvg = new();
        private List<double> netCounterInAvg = new();

        public Form1()
        {
            InitializeComponent();
            timer1.Tick += timer1_Tick;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            
            string GetHardwareInfo(string WIN32)
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM " + WIN32);
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["Name"].ToString().Trim();
                }
                return "";
            }
            gpuCounters.ForEach(x => x.NextValue());
            for (int i = 0; i < performanceCounterCategory.GetInstanceNames().Length; i++)
            {
                dataReceivedCounters.Add(new PerformanceCounter("Network Interface", "Bytes Received/sec",
                    interfaces[i]));
                dataSentCounters.Add(new PerformanceCounter("Network Interface", "Bytes Sent/sec", interfaces[i]));
                dataSentCounters[i].NextValue();
                dataReceivedCounters[i].NextValue();
            }

            cpuCounterAvg.Add(cpuCounter.NextValue());
            gpuCounterAvg.Add(gpuCounters.Sum(x => x.NextValue()));
            discCounterAvg.Add(discCounter.NextValue());
            ramCounterAvg.Add(ramCounter.NextValue());
            netCounterInAvg.Add((double)dataReceivedCounters[0].NextValue() / 1024 / 1024);
            netCounterOutAvg.Add((double)dataSentCounters[0].NextValue() / 1024 / 1024);


            foreach (var i in new List<List<double>> { cpuCounterAvg, gpuCounterAvg, discCounterAvg, netCounterOutAvg, netCounterInAvg, ramCounterAvg }.Where(i => i.Count >= 10))
                i.RemoveAt(0);

            List<string> ls = new List<string>
            {
                $"Версия системы: {Environment.OSVersion}",
                $"Текущий пользователь: {Environment.UserName}",
                $"Время работы системы: {Environment.TickCount / 3600000 % 24} часов {Environment.TickCount / 120000 % 60} минут {Environment.TickCount / 1000 % 60} секунд",
                $"Марка и модель пк: {Environment.MachineName}",
                $"Модель cpu: {GetHardwareInfo("Win32_Processor")}",
                $"Модель gpu: {GetHardwareInfo("Win32_VideoController")}",
                $"Загруженость cpu: {Math.Round(cpuCounterAvg.Average(), 2)} %",
                $"Загруженость gpu: {Math.Round(gpuCounterAvg.Average(), 2)}%",
                $"Загруженость диска: {Math.Round(discCounterAvg.Average(), 2)}%",
                $"Доступно ram: {Math.Round(ramCounterAvg.Average(), 2)} MB",
                $"Сеть: In {Math.Round(netCounterInAvg.Average(), 2)}MB/sec | Out {Math.Round(netCounterOutAvg.Average(), 2)}MB/sec"
            };
            ls.AddRange(DriveInfo.GetDrives().Select(i => $"Диск {i.Name} {Math.Round((double)i.AvailableFreeSpace / 1024 / 1024 / 1024)}/{Math.Round((double)i.TotalSize / 1024 / 1024 / 1024)} ГБ доступно"));
            richTextBox1.Lines = ls.ToArray();
        }
    }
}