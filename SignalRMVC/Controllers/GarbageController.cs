using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace InternetNow.SampleApp.Controllers
{
    public class GarbageController : Controller
    {
        public static string path = "C:\\garbage.txt";
        private static int maxFileSize = 1024 * 20; // 20 KB
        private static readonly Queue<Task> WaitingTasks = new Queue<Task>();
        private static readonly Dictionary<int, Task> RunningTasks = new Dictionary<int, Task>();
        public static int MaxRunningTasks = 100;
        private static CancellationTokenSource tokenSource = new CancellationTokenSource();

        [HttpPost]
        public JsonResult Start(bool numeric, bool alphanumeric, bool checkedfloat, int fileSize)
        {
            var types = new List<InputType>();
            if (numeric) {
                types.Add(InputType.Numeric);
            }
            if (alphanumeric)
            {
                types.Add(InputType.AplhaNumeric);
            }
            if (checkedfloat)
            {
                types.Add(InputType.Float);
            }

            if (fileSize == 0)
                fileSize = maxFileSize;
            else
                fileSize = fileSize * 1024;

            if (types != null && types.Count > 0)
            {
                // Clean file
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Create(path).Close();
                }

                var token = tokenSource.Token;
                Generate.Done = new Generate.DoneDelegate(WorkDone);
                WaitingTasks.Enqueue(new Task(id => new Generate().doGenerate((int)id,token, path,fileSize,numeric,alphanumeric,checkedfloat,types),1,token));
               
                // keep checking until we're done
                while ((WaitingTasks.Count > 0) || (RunningTasks.Count > 0))
                {
                    // launch tasks when there's room
                    while ((WaitingTasks.Count > 0) && (RunningTasks.Count < MaxRunningTasks))
                    {
                        Task task = WaitingTasks.Dequeue();
                        lock (RunningTasks) RunningTasks.Add((int)task.AsyncState, task);
                        task.Start();
                    }
                    Task.Delay(300); // wait before checking again
                }
            }

            return Json(true);
        }

        public // callback from finished worker
        static void WorkDone(int id)
        {
            lock (RunningTasks) RunningTasks.Remove(id);
        }

        [HttpPost]
        public JsonResult Stop(bool stop) 
        {
            lock (WaitingTasks) WaitingTasks.Clear();
            lock (RunningTasks) RunningTasks.Clear();
            tokenSource.Cancel();
            return Json(true);
        }


        public ActionResult GenerateReport()
        {
            List<InputType> types = new List<InputType>();
            string[] lines = System.IO.File.ReadAllLines(path);
            foreach(var line in lines){
               var txttypes = line.Split('#');
                foreach(var type in txttypes)
                {
                    if (type.StartsWith("Number-")) 
                    {
                        types.Add(InputType.Numeric);                    
                    }
                    if (type.StartsWith("AlphaNumeric-"))
                    {
                        types.Add(InputType.AplhaNumeric);
                    }
                    if (type.StartsWith("Float-"))
                    {
                        types.Add(InputType.Float);
                    }
                }
            }

            ViewBag.Numeric = types.Where(t => t == InputType.Numeric).Count() % 100;
            ViewBag.AlphaNumeric = types.Where(t => t == InputType.AplhaNumeric).Count() % 100;
            ViewBag.Float = types.Where(t => t == InputType.Float).Count() % 100;
            return View();
        }
    }

    internal class Generate
    {
        public delegate void DoneDelegate(int taskId);
        public static DoneDelegate Done { private get; set; }
        private static readonly Random Rnd = new Random();
        private object status = new object();
        public async void doGenerate(object id, CancellationToken token, string path, int fileSize, bool numeric, bool alphanumeric, bool checkedfloat, List<InputType> types){
            StringBuilder sb;
            var context = GlobalHost.ConnectionManager.GetHubContext<SignalRMVCHub>();

            int i, j, k;
            i = 0; j = 0; k = 0;
            while (((new FileInfo(path).Length) < fileSize))
            {
                sb = new StringBuilder();

                Parallel.ForEach(types, t => 
                {
                    if (t == InputType.Numeric)
                    {
                        sb.Append("Number-").Append(Utils.random.Next()).Append("#");
                        i++;
                    }

                    if (t == InputType.AplhaNumeric)
                    {
                        sb.Append("AlphaNumeric-").Append(Utils.RandomString()).Append("#");
                        j++;
                    }

                    if (t == InputType.Float)
                    {
                        sb.Append("Float-").Append(Utils.random.NextDouble()).Append("#");
                        k++;
                    }
                    lock (status)
                    {
                        using (StreamWriter file = System.IO.File.AppendText(path))
                        {
                            file.Write(sb.ToString());
                        }
                        context.Clients.All.updateCount(i, j, k);

                    }
                 });

                if (token.IsCancellationRequested)
                { break; }
                await Task.Delay(100);
            }
            Done((int)id);
    
        }
    }
}