# Standalone apps with self-contained speech sample Overview


This sample extends the [Scenario sample](../ScenarioSample), embedding  a speech
recognition plugin built around the 
[Microsoft Cognitive Services Speech to Text](https://azure.microsoft.com/en-us/products/cognitive-services/speech-to-text/) 
service.

STP's multimodal approach to natural language planning fuses speech and sketch into combined 
interpretations that make it easy for users to express location  
by "pointing" to the place where they want symbols, and provide the type of symbol via
speech, leveraging natural modes of communication, where speech is used to describe
things that might be pointed to on a map.

Applications require therefore that users' sketches and speech be captured and sent to 
the STP Engine for processing. 
A common way for speech to be collected is via STP's Speech Recognition Component.
This component controls the audio input of a user's machine, and takes advantage of
multiple speech recognizers to provide robust transcriptions that work when the
user is online or offline, including conditions in which connectivity may be 
intermittent. 

When using the Speech Recognition Component, applications simply collect and 
relay the sketches, with no concern for handling speech, which is sent directly to STP
by the SPeech Recognition Component.

In this application, an alternative approach is demonstrated: the embedding of a speech
recognizer within the application itself, producing a self-contained application
that can be deployed on its own, without additional components.

This sample illustrates the approach by employing a plugin that takes advantage
of a cloud service - the Microsoft Cognitive Services Speech to Text service.
The sample is limited to conditions where online connectivity to the internet is
available, a pre-condition for use of cloud-based services. 

**NOTE:** Microsoft Cognitive Services Speech can also be run within a container, 
but as of this writing, an internet connection is still required for billing
purposes, at least for general commercial customers.

For scenarios requiring offline/disconnected recognition, and transparent switching
from online to offline recognizers of the fly, use the STP Speech Recognition Component,
or contact Hyssos to discuss how that functionality can be embedded in an app.

Here just the particular aspects illustrated by the sample are described.
Details shared by all samples are described in the [main samples page](../README.md). 
The workings of the speech plugin itself are described in the  
[Speech plugin documentation](../../plugins/Speech/AzureSpeechPlugin/README.md).

## Pre-requisites

Microsoft Cognitive Services Speech requires a subscription. 
Visit <https://azure.microsoft.com/en-us/products/cognitive-services/speech-to-text/> 
to obtain a trial account if needed.

Select Create a Resource and pick the Speech service. Proceed with the creation,
selecting a region. Retrieve the key that is required to activate the service.

The key, region, language can be configured in a variety of ways:

- Edit appsettings.json and enter the values  into `AzureKey`, `AzureRegion`, `AzureLanguage`
- Or invoke the app with command line parameters, for example `StpApp:AzureKey="<key>"`, 
- Parameters can also be set via environment variables, such as `StpApp__AzureKey`, with the key
as the value

## Connecting to the plugin

An instance of the recognizer plugin is created within the `Connect()` method, alongside
the initialization of the STP recognizer and mapping objects.
This plugin takes the key, region, language, optional custom language model endpoint
as parameters.

Event handling is described in more detail in the rest of this document.

```csharp
// Create the embedded speech recognizer
_speechRecognizer = new AzureSpeechRecognizer(_appParams.AzureKey,
    _appParams.AzureRegion, _appParams.AzureLang, _appParams.AzureEndpoint);
_speechRecognizer.OnRecognized += SpeechRecognizer_OnRecognized;
_speechRecognizer.OnRecognizing += SpeechRecognizer_OnRecognizing;
_speechRecognizer.OnListeningStateChanged += SpeechRecognizer_OnListeningStateChanged;
_speechRecognizer.OnError += SpeechRecognizer_OnError;
```

## Activating recognition from within the application

In this app speech is, as in the previous samples, triggered by sketching.
Audio is activated and transcription is initiated when a pen down sketching event
is detected.
Since the transcription is handled by an embedded plugin, the app needs to include a
call to activate recognition. 

THat is done right after the pen down notification is sent to STP. 
The call to `RecognizeOnceAsync()` initiates an asynchronous operation.
This methods takes the Id of the audio source device. If `null`, the default
microphone is used.
To obtain device Ids, an audio library such as [NAudio](https://github.com/naudio/NAudio) 
can be used. 

```csharp
/// <summary>
/// Notify STP that the user started to sketch
/// </summary>
/// <remarks>
/// STP will propagate this event, which will cause for example speech recognizers to be activated
/// to collect user speech to fuse with this sketched gesture
/// </remarks>
/// <param name="sender"></param>
/// <param name="geoPoint"></param>
private void MapHandler_OnPenDown(object sender, LatLon geoPoint)
{
    // Notify STP of the start of a stroke and activate speech recognition
    _stpRecognizer.SendPenDown(geoPoint, DateTime.Now);
    // Trigger speech recognition (asynchronously), in case it is not already ongoing
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    _speechRecognizer.RecognizeOnceAsync(audioDeviceId:null);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
}
```

Actual interpretations are returned to the app via events, as examined next.

## Handling events

The main event of interest is `OnRecognized`, which returns an object that describes
what was detected by the speech recognition service, if anything.

These results need to be sent over to STP so it can integrate with gestures
that are being sent asynchronously by the app.
That is done via the `SendSpeechRecognition()` method, which takes the list of alternate 
interpretations and time brackets that are returned by the plugin.

The sample also presents the results to users, by invoking the method that handled
the speech results originated by the external STP Speech Recognition Component.

```csharp
/// <summary>
/// Speech utterance recognized
/// </summary>
/// <param name="sr"></param>
private void SpeechRecognizer_OnRecognized(SpeechRecoResult sr)
{
    if (sr is null || sr.Results is null || sr.Results.Count == 0)
    {
        return;
    }
    // Send over to stp for processing
    _stpRecognizer.SendSpeechRecognition(sr.Results, sr.StartTime, sr.EndTime);

    // Extract list of recognized alternates and display locally
    List<string> speechList = sr.Results.Select(a => a.Text).ToList();
    StpRecognizer_OnSpeechRecognized(speechList);
}
```

The plugin also produces incremental recognition strings that can be handled
via `OnRecognizing`. 
In the app, these results are displayed on the speech textbox, just like
the alternates and final recognition results.

For shorter utterances, like the ones STP is designed to use, full transcription
results are produced fast, so unless a longer sentence is being transcribed, the
partial results are quickly replaced by the final ones.

```
/// <summary>
/// Partial speech transcription results
/// </summary>
/// <param name="phrase"></param>
private void SpeechRecognizer_OnRecognizing(string phrase)
{
    if (this.InvokeRequired)
    {   // recurse on GUI thread if necessary
        this.Invoke(new MethodInvoker(() => SpeechRecognizer_OnRecognizing(phrase)));
        return;
    }
    txtSimSpeech.Text = phrase;
    ShowStpMessage(phrase);
}
```


Audio state feedback is provided in the `OnListeningStateChanged` handler.
Once again, the internal speech component's event is just relayed to the same
handler used previously to deal with events provided by the external 
STP Speech Recognition Component.

```csharp
/// <summary>
/// Audio collection started/stopped
/// </summary>
/// <param name="isListening"></param>
private void SpeechRecognizer_OnListeningStateChanged(bool isListening)
{
    StpRecognizer_OnListeningStateChanged(isListening);
}
```

Finally, errors are displayed in the `OnError` handler.

```csharp
/// <summary>
/// Speech recognition error detected
/// </summary>
/// <param name="msg"></param>
/// <exception cref="NotImplementedException"></exception>
private void SpeechRecognizer_OnError(string msg)
{
    ShowStpMessage(msg);
}
```