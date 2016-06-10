using System;
using System.Runtime.InteropServices;

namespace IpcBpmCounter
{
    public class BPMDetect
    {
        public enum BPMParam
        {
            BPMFOUNDBPM,
            BPMNROFBEATS,
            BPMBEATLOOP,
            BPMCURRENTBEATTIME,
            BPMRESET,
            BPMMETHOD_WAVELIST,
            BPMMETHOD_STRIPES,
            BPMRANGE_MINBPM,
            BPMRANGE_MAXBPM
        }

        public enum BPMFFTParam
        {
            BPMFFT_WINDOWSIZE,
            BPMFFT_NBWINDOWS,
            BPMFFT_WINDOWTYPE,
            //BPMFFT_SAMPLERATE
        }

        private static BPMDetect _bpmdetect;
        public static BPMDetect Instance => _bpmdetect ?? (_bpmdetect = new BPMDetect());

        private IntPtr bpmi;

        [DllImport("bpmDetect.dll")]
        private static extern IntPtr BPM_Create();
        [DllImport("bpmDetect.dll")]
        private static extern void BPM_Destroy(IntPtr bpm);
        [DllImport("bpmDetect.dll")]
        private static extern void BPM_AddSample(IntPtr bpm, float sample);
        [DllImport("bpmDetect.dll")]
        private static extern double BPM_getParameter(IntPtr bpm, BPMParam param);
        [DllImport("bpmDetect.dll")]
        private static extern void BPM_setParameter(IntPtr bpm, BPMParam param, double value);
        [DllImport("bpmDetect.dll")]
        private static extern double BPM_getFFTParameter(IntPtr bpm, BPMFFTParam param);
        [DllImport("bpmDetect.dll")]
        private static extern void BPM_setFFTParameter(IntPtr bpm, BPMFFTParam param, double value);
        [DllImport("bpmDetect.dll")]
        private static extern void BPM_Register(int[] code);

        public double BPM => bpmi.ToInt32() == 0 ? 0 : BPM_getParameter(bpmi, BPMParam.BPMFOUNDBPM);
        public double Accuracy => bpmi.ToInt32() == 0 ? 0 : BPM_getParameter(bpmi, BPMParam.BPMNROFBEATS);
        public int BeatLoop => bpmi.ToInt32() == 0 ? 0 : (int)BPM_getParameter(bpmi, BPMParam.BPMBEATLOOP);
        public double BeatTime => bpmi.ToInt32() == 0 ? 0 : BPM_getParameter(bpmi, BPMParam.BPMCURRENTBEATTIME);

        public void Init()
        {
            bpmi = BPM_Create();
        }

        public void Register(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            if(bytes.Length != 40) return;
            int[] regcode = new int[10];
            for (int i = 0; i < 10; i++)
            {
                var ii = i*4;
                regcode[i] = BitConverter.ToInt32(bytes, ii);
            }
            BPM_Register(regcode);
        }

        public void Destroy()
        {
            BPM_Destroy(bpmi);
        }

        public void AddSample(float samp)
        {
            BPM_AddSample(bpmi, samp * 32767.0f);
        }

        public void SetWindowCount(int count)
        {
            BPM_setFFTParameter(bpmi, BPMFFTParam.BPMFFT_NBWINDOWS, count);
        }
        public void UseWaveList(bool use)
        {
            BPM_setParameter(bpmi, BPMParam.BPMMETHOD_WAVELIST, use ? 1 : 0);
        }
        public void UseStripes(bool use)
        {
            BPM_setParameter(bpmi, BPMParam.BPMMETHOD_STRIPES, use ? 1 : 0);
        }
        public void Reset()
        {
            BPM_setParameter(bpmi, BPMParam.BPMRESET, 1);
        }
    }
}
