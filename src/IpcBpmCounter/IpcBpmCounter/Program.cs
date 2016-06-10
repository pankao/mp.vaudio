using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AwesomeSockets.Sockets;
using PowerArgs;
using ABuf = AwesomeSockets.Buffers.Buffer;

namespace IpcBpmCounter
{
    public class PArgs
    {
        [ArgPosition(1)]
        [ArgShortcut("-p")]
        [ArgDefaultValue(4446)]
        public int TcpPort { get; set; }

        [ArgShortcut("-wavelist")]
        [ArgDefaultValue(true)]
        public bool UseWaveList { get; set; }

        [ArgShortcut("-stripes")]
        [ArgDefaultValue(false)]
        public bool UseStripes { get; set; }

        [ArgShortcut("-wc")]
        [ArgDefaultValue(2)]
        [ArgRange(1,16)]
        public int WindowCount { get; set; }
    }
    public class SocketCom
    {
        [ArgActionMethod]
        public void Register(SocketData dat)
        {
            BPMDetect.Instance.Register(dat.Data);
            Console.WriteLine("Registered");
        }

        [ArgActionMethod]
        public void Init()
        {
            var bpmd = BPMDetect.Instance;
            var args = GArgs.Args;
            bpmd.Init();
            bpmd.UseStripes(args.UseStripes);
            bpmd.UseWaveList(args.UseWaveList);
            Console.WriteLine("Initialized");
        }

        [ArgActionMethod]
        public void Reset()
        {
            BPMDetect.Instance.Reset();
            Console.WriteLine("Reset");
        }

        [ArgActionMethod]
        public void AddSamples(SampleData dat)
        {
            using (var mmf = MemoryMappedFile.OpenExisting(dat.Address))
            {
                using (var accessor = mmf.CreateViewAccessor(0, dat.Length))
                {
                    for (long i = 0; i < dat.Length/4; i++)
                    {
                        var ii = i*4;
                        BPMDetect.Instance.AddSample(accessor.ReadSingle(ii));
                    }
                }
            }
        }
    }

    public class SocketData
    {
        [ArgRequired]
        [ArgPosition(1)]
        public string Data { get; set; }
    }
    public class SampleData
    {
        [ArgRequired]
        [ArgPosition(1)]
        public string Address { get; set; }

        [ArgRequired]
        [ArgPosition(2)]
        public long Length { get; set; }
    }

    public class GArgs
    {
        public static PArgs Args;
    }
    class Program
    {
        static void Main(string[] args)
        {
            GArgs.Args = Args.Parse<PArgs>(args);
            var sserver = AweSock.TcpListen(GArgs.Args.TcpPort);
            Console.WriteLine("TCP Available");
            var sclient = AweSock.TcpAccept(sserver);
            var inbuf = ABuf.New(65536);
            var outbuf = ABuf.New(1024);
            var listener = new Task(() =>
            {
                while (true)
                {
                    try
                    {
                        ABuf.ClearBuffer(inbuf);
                        var received = AweSock.ReceiveMessage(sclient, inbuf);
                        ABuf.FinalizeBuffer(inbuf);
                        var resbytes = ABuf.GetBuffer(inbuf).Take(received.Item1).ToArray();
                        if (resbytes.Length > 0)
                        {
                            var resstring = Encoding.Default.GetString(resbytes);
                            Args.InvokeAction<SocketCom>(Args.Convert(resstring));
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                    }
                }
            });
            listener.Start();
            
            Console.WriteLine("TCP Connected");
            var bpmd = BPMDetect.Instance;
            int beat = 0;
            while (true)
            {
                var cbeat = bpmd.BeatLoop;
                if (beat != cbeat)
                {
                    //Console.
                    //Console.WriteLine("{0} {1} {2} {3}", bpmd.BPM, bpmd.Accuracy, bpmd.BeatLoop, bpmd.BeatTime);
                    ABuf.ClearBuffer(outbuf);
                    ABuf.Add(outbuf, Encoding.Default.GetBytes(bpmd.BPM.ToString() + " " + bpmd.Accuracy.ToString() + " " + bpmd.BeatLoop.ToString() + " " + bpmd.BeatTime.ToString()));
                    ABuf.FinalizeBuffer(outbuf);
                    AweSock.SendMessage(sclient, outbuf);
                }
                beat = cbeat;
                /*
                */
            }
        }
    }
}
