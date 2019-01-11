using Frapper;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace DeepSpeech.NET_WER
{
    class Program
    {
        private static ProcessStartInfo pythonProcess;
        private static readonly string TempAudioFile = "temp.wav";
        static void Main(string[] args)
        {
            FFMPEG ffmpeg = new FFMPEG("ffmpeg.exe");
            pythonProcess = new ProcessStartInfo
            {
                FileName = "python.exe",
                CreateNoWindow = true, // No window 
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            var dirs = Directory.GetFileSystemEntries("test-clean/LibriSpeech/test-clean", "*.txt", SearchOption.AllDirectories);
            IDictionary<string, string> dataset = new Dictionary<string, string>();
            foreach (var transcriptionFile in dirs)
            {
                FileInfo fileInf = new FileInfo(transcriptionFile);
                foreach (var sentenceLine in File.ReadAllLines(transcriptionFile))
                {
                    var sentenceSplit = sentenceLine.Split(' ');
                    string audioName = fileInf.FullName.Replace(fileInf.Name, $"{sentenceSplit[0]}.flac");
                    string sentence = string.Join(" ", sentenceSplit.ToList().Skip(1).ToArray()).ToLower();
                    dataset.Add(audioName, sentence);
                }
            }

            const uint N_CEP = 26;
            const uint N_CONTEXT = 9;
            const uint BEAM_WIDTH = 200;
            const float LM_ALPHA = 0.75f;
            const float LM_BETA = 1.85f;

            const string modelVersion = "0.4.1";
            List<Sentence> samples = new List<Sentence>();
            using (var sttClient = new DeepSpeechClient.DeepSpeech())
            {
                var result = 1;
                Console.WriteLine("Loading model...");
                try
                {
                    result = sttClient.CreateModel($"{modelVersion}/output_graph.pbmm",
                        N_CEP, N_CONTEXT,
                        $"{modelVersion}/alphabet.txt",
                        BEAM_WIDTH);
                }
                catch (IOException ex)
                {
                    Console.WriteLine("Error loading lm.");
                    Console.WriteLine(ex.Message);
                }
                if (result == 0)
                {
                    Console.WriteLine("Loadin LM...");
                    try
                    {
                        result = sttClient.EnableDecoderWithLM(
                            $"{modelVersion}/alphabet.txt",
                            $"{modelVersion}/lm.binary",
                            $"{modelVersion}/trie",
                            LM_ALPHA, LM_BETA);
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine("Error loading lm.");
                        Console.WriteLine(ex.Message);
                    }

                    foreach (var sentencePair in dataset)
                    {

                        ConvertFileToWav(sentencePair.Key,ffmpeg);

                        var waveBuffer = new WaveBuffer(File.ReadAllBytes(TempAudioFile));
                        Console.WriteLine("Running inference....");

                        string speechResult = sttClient.SpeechToText(waveBuffer.ShortBuffer, Convert.ToUInt32(waveBuffer.MaxSize / 2), 16000);

                        Sentence sentenceResult = RunPythonWER(sentencePair.Value, speechResult);

                        Console.WriteLine("================================================================================");
                        Console.WriteLine($"Recognized text: {speechResult}");
                        Console.WriteLine($"Correct text: {sentencePair.Value}");
                        Console.WriteLine($"WER {Math.Round(sentenceResult.Wer,2)*100} %");
                        Console.WriteLine("================================================================================");
                        Console.WriteLine();
                        samples.Add(sentenceResult);

                        waveBuffer.Clear();
                    }
                }
                else
                {
                    Console.WriteLine("Error loding the model.");
                }
            }
            double totalLevenshtein = samples.Select(x => x.Levenshtein).Sum();
            int totalLabelLength = samples.Select(x => x.Length).Sum();
            double finalWer = totalLevenshtein / totalLabelLength;
            File.WriteAllText("result.txt", finalWer.ToString(), Encoding.UTF8);
            Console.WriteLine($"Final WER: {finalWer} %");
            Console.ReadKey();
        }

        private static void ConvertFileToWav(string key,FFMPEG ffmpeg)
        {
            if (File.Exists(TempAudioFile))
            {
                File.Delete(TempAudioFile);
            }
            ffmpeg.RunCommand($"-i \"{key}\" -f wav -acodec pcm_s16le -ac 1 -sample_fmt s16 -ar 16000 \"{TempAudioFile}\"");
        }

        private static Sentence RunPythonWER(string original, string result)
        {
            //we need to convert 
            pythonProcess.Arguments = $" text.py -original \"{original}\" -result \"{result}\""; // parameters for the Python script
            using (Process process = Process.Start(pythonProcess))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    var results = reader.ReadLine().Split('|');
                    return new Sentence
                    {
                        Wer = Convert.ToDouble(results[0].Replace(".", ",")),
                        Levenshtein = Convert.ToDouble(results[1].Replace(".", ",")),
                        Length = original.Split(' ').Length
                    };
                }
            }
        }
    }
}
