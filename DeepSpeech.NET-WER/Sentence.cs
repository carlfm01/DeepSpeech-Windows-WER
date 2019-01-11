namespace DeepSpeech.NET_WER
{
    class Sentence
    {
        public int Length { get; set; }
        public double Wer { get; set; }
        public double Levenshtein { get; set; }
         
    }
}
