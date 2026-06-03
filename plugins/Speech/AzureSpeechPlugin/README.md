# Speech recognition plugin

STP is architected to allow for multiple speech recognizers to be employed, either in isolation or
combined to obtain state-of-the-art transcriptions.

This sample plugin implements basic speech transcription services to illustrate the process of 
incorporating a speech recognition service more directly into an app, as an alternative to launching
the STP Speech Recognition Component side-by-side. 

This plugin is built around the [Microsoft Cognitive Services Speech to Text]
(https://azure.microsoft.com/en-us/products/cognitive-services/speech-to-text/) service.
This is primarily a cloud service, so apps using the plugin only operate when internet connectivity is
available (even though it can be run within a container). 

Other recognizers, particularly offline-capable ones may be incorporated in a similar fashion, even if 
usually requiring additional scaffolding (e.g. to manage audio devices).
Implementation of offline-capable plugins is outside the scope of this document.
Contact Hyssos to discuss to discuss how that functionality can be deployed.

For a background and details of the Microsoft Cognitive Services Speech to Text service, see Microsoft's
[overview](https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-to-text) and 
[quickstart](https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/get-started-speech-to-text?tabs=windows%2Cterminal&pivots=programming-language-csharp)

## Pre-requisites

Microsoft Cognitive Services Speech requires a subscription. Applications using this plugin
will be required to provide a valid key in order to use the service.

Visit <https://azure.microsoft.com/en-us/products/cognitive-services/speech-to-text/> 
to obtain a trial account if needed.

Select Create a Resource and pick the Speech service. Proceed with the creation,
selecting a region. Retrieve the key that is required to activate the service.

In the app using the plugin, configure key, region, language. One way is to use application
settings to store the required parameters.
See the [SpeechSample](../../../samples/SpeechSample) for further discussion and examples.

## Main method

A single `RecognizeOnceAsync()` method activates recognition, usually when a pen down sketching event
is detected.

```csharp
/// <summary>
/// Activate the microphone and attempt to recognize speech, listening until an utterance is completed,
/// or Stop() is called to signal the end of the extended listening period 
/// Ideally, the recognition would include 2s of audio _before_ the call, drawing from some buffer
/// </summary>
/// <param name="audioDeviceId">Id of the audio source device or null to use the default</param>
/// <param name="cancellationToken"></param>
/// <returns>Recognized items/hypotheses, or null if nothing was recognized</returns>
Task RecognizeOnceAsync(string audioDeviceId, CancellationToken cancellationToken);
```

## Recognition results

Results are returned asynchronously via the `OnRecognized` event.
Other events provide additional user feedback as described below.

```csharp
/// <summary>
/// Arguments for  complete phrase recognized event
/// </summary>
public delegate void SpeechRecognizedDelegate(SpeechRecoResult sr);

/// <summary>
/// Speech utterance was recognized
/// </summary>
public event SpeechRecognizedDelegate OnRecognized;
```

The results include a list of `SpeechRecoItem` results, as well as timing information.

```csharp
/// <summary>
/// Speech recognition results
/// </summary>
public class SpeechRecoResult
{
    /// <summary>
    /// Identifies the recognizer producing the result (multiple may be used)
    /// </summary>
    public string FromReco { get; set; }

    /// <summary>
    /// Speech recognition hypothesis
    /// </summary>
    public List<SpeechRecoItem> Results { get; set; }

    /// <summary>
    /// Time speech started
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Time speech ended
    /// </summary>
    public DateTime EndTime { get; set; }
}
```

Each recognition alternate contains the transcription, timing and likelihood/confidence
in the interpretation. 


```csharp
/// <summary>
/// Recognition hypotheses / alternates
/// </summary>
public class SpeechRecoItem
{
    /// <summary>
    /// Transcribed speech text
    /// </summary>
    public string Text { get; set; }
    /// <summary>
    /// Likelihood/confidence of the interpretation
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Time speech started
    /// </summary>
    public double? StartSec { get; set; }

    /// <summary>
    /// Time speech ended
    /// </summary>
    public double? EndSec { get; set; }

    /// <summary>
    /// Additional reco-specific information to expose
    /// </summary>
    public string ExtraRecoInfo { get; set; }
}
```

## Additional user feedback

Other events provide additional user feedback as described below.

```csharp
#region Event delegate voids
/// <summary>
/// Arguments for  partial recognition event
/// </summary>
public delegate void SpeechRecognizingDelegate(string phrase);

/// <summary>
/// Arguments for start/end of speech detected events
/// </summary>
public delegate void SpeechStartPauseEndDelegate();

/// <summary>
/// Arguments for recognition error events
/// </summary>
public delegate void SpeechErrorDelegate(string msg);
#endregion

#region Events
/// <summary>
/// Partial recognition available
/// </summary>
public event SpeechRecognizingDelegate OnRecognizing;
/// <summary>
/// Speech segment start detected
/// </summary>
public event SpeechStartPauseEndDelegate OnSpeechStart;
/// <summary>
/// Speech segment end detected
/// </summary>
public event SpeechStartPauseEndDelegate OnSpeechEnd;
/// <summary>
/// Audio collection started/stopped
/// </summary>
public event ListeningStateChangedDelegate OnListeningStateChanged;
/// <summary>
/// Recognition error
/// </summary>
public event SpeechErrorOrCancelDelegate OnError;
/// <summary>
/// Recognition canceled
/// </summary>
public event SpeechErrorOrCancelDelegate OnCanceled;
#endregion
```
