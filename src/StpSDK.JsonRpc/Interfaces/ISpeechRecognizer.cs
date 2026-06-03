using System.Threading;
using System.Threading.Tasks;

namespace StpSDK;

public delegate void SpeechRecognizedDelegate(SpeechRecoResult sr);
public delegate void SpeechRecognizingDelegate(string phrase);
public delegate void SpeechStartPauseEndDelegate();
public delegate void SpeechErrorOrCancelDelegate(string msg);

public interface ISpeechRecognizer
{
    event SpeechRecognizedDelegate OnRecognized;
    event SpeechRecognizingDelegate OnRecognizing;
    event SpeechStartPauseEndDelegate OnSpeechStart;
    event SpeechStartPauseEndDelegate OnSpeechEnd;
    event ListeningStateChangedDelegate OnListeningStateChanged;
    event SpeechErrorOrCancelDelegate OnError;
    event SpeechErrorOrCancelDelegate OnCanceled;

    Task RecognizeOnceAsync(string audioDeviceId, CancellationToken cancellationToken);
}
