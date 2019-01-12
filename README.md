# DeepSpeech-Windows-WER
WER test for Mozilla's Windows DeepSpeech client

**Versions for 0.4.1 WER test**

 DeepSpeech Model v0.4.1 from [model v0.4.1](https://github.com/mozilla/DeepSpeech/releases/tag/v0.4.1) 

 Windows DeepSpeech native clients from [Windows native clients](https://github.com/carlfm01/deepspeech-tempwinbuilds/releases/tag/0.4.1) 

 NET client wrapper from [NET Framework client](https://github.com/mozilla/DeepSpeech/tree/master/examples/net_framework/CSharpExamples/DeepSpeechClient) 

Used [LibriSpeech clean test corpus ](http://www.openslr.org/12) for the test.
 
**Model 0.4.1 WER result**

Estable RAM usage of **1,7GB**.

The test took about **3h** on a virtual **Intel Xeon Platinum 8168 @ 2.7GHz vcores 16**

WER **8,87%** with LM enabled.
