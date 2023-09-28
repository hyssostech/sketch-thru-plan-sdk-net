using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using AzureRecognizer = Microsoft.CognitiveServices.Speech.SpeechRecognizer;
using AzureSpeechRecognitionEventArgs = Microsoft.CognitiveServices.Speech.SpeechRecognitionEventArgs;
using AzureRecognitionResult = Microsoft.CognitiveServices.Speech.SpeechRecognitionResult;
using AzureSpeechConfig = Microsoft.CognitiveServices.Speech.SpeechConfig;
using System.Net.Cache;
using System.Net;
using static StpSDK.StpRecognizer;

namespace StpSDK.Speech;

public class AzureSpeechRecognizer : ISpeechRecognizer, IDisposable
{
    #region Events
    /// <summary>
    /// Speech utterance was recognized
    /// </summary>
    public event SpeechRecognizedDelegate OnRecognized;
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

    #region Private properties
    private AzureSpeechConfig _azureConfig;
    private string _azureKey;
    private string _azureRegion;
    private Uri _containertUri;
    private string _azureLanguage;
    private string _azureEndpoint;

    //private TranslationRecognizer _speechRecognizer;
    private AzureRecognizer _speechRecognizer;
    private AudioConfig _audioConfig;
    private bool _isListening;
    private DateTime _recoStartTime;

    private bool _disposedValue;
    #endregion

    #region Construction / teardown
    /// <summary>
    /// Construct recognizer with parameters for MS Cognitive Services Speech running in the cloud
    /// </summary>
    /// <param name="azureKey"></param>
    /// <param name="azureRegion"></param>
    /// <param name="azureLanguage"></param>
    /// <param name="azureEndpoint">Endpoint Id of a custom speech model</param>
    public AzureSpeechRecognizer(
        string azureKey, 
        string azureRegion, 
        string azureLanguage = "en-US",
        string azureEndpoint = null) 
    {
        _azureKey = azureKey;
        _azureRegion = azureRegion;
        _containertUri = null;

        CommonConfig(azureLanguage, azureEndpoint);
    }

    /// <summary>
    /// Construct recognizer with parameters for MS Cognitive Services Speech running in a container
    /// </summary>
    /// <param name="containertUri"></param>
    /// <param name="azureLanguage"></param>
    /// <param name="azureEndpoint">Endpoint Id of a custom speech model</param>
    public AzureSpeechRecognizer(Uri containertUri, string azureLanguage = "en-US", string azureEndpoint = null)
    {
        _containertUri = containertUri;
        // Not used if in container
        _azureKey = _azureRegion = null;

        CommonConfig(azureLanguage, azureEndpoint);
    }

    /// <summary>
    /// Common constructor initialization
    /// </summary>
    /// <param name="azureLanguage"></param>
    /// <param name="azureEndpoint"></param>
    private void CommonConfig(string azureLanguage, string azureEndpoint)
    {
        _azureLanguage = azureLanguage;
        _azureEndpoint = azureEndpoint;

        // Recognizer configuration
        _azureConfig = _containertUri is null
            ? SpeechConfig.FromSubscription(_azureKey, _azureRegion)
            : SpeechConfig.FromHost(_containertUri); // e.g. new Uri("ws://localhost:5000")
        _azureConfig.SpeechRecognitionLanguage = _azureLanguage;
        // Custom language model?
        if (_azureEndpoint != null)
        {
            _azureConfig.EndpointId = _azureEndpoint;
        }
        // Need alternate hypotheses and timing details
        _azureConfig.OutputFormat = OutputFormat.Detailed;

    }

    public virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                if (_speechRecognizer != null)
                {
                    _speechRecognizer.Dispose();
                    _speechRecognizer = null;
                }
                if (_audioConfig != null)
                {
                    _audioConfig.Dispose();
                    _audioConfig = null;
                }
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~AzureSpeechRecognizer()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    /// <summary>
    /// Dispose
    /// </summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion

    #region Public methods
    /// <summary>
    /// Recognize the next utterance, raising event with result
    /// </summary>
    /// <remarks>
    /// The call returns after a timeout if nothing is recognized
    /// </remarks>
    /// <param name="audioDeviceId">Id of the audio source device to use; null for the default device</param>
    /// <param name="cancellationToken"></param>
    public async Task RecognizeOnceAsync(string audioDeviceId, CancellationToken cancellationToken=default)
    {
        // Bail out if already listening
        if (_isListening)
        {
            return;
        }
        // Don't bother starting if using the cloud service and it is not reachable
        if (_containertUri is null && !await IsReachableAsync())
        {
            OnError?.Invoke($"Microsoft Cognitive Services Speech is not reachable");
            return;
        }
        // Start recognition proper in a thread
        await Task.Run(async () =>
        {
            SpeechRecoResult sr = null;
            try
            {
                _isListening = true;
                cancellationToken.ThrowIfCancellationRequested();

                // Define the audio source - it might have changed
                _audioConfig = string.IsNullOrWhiteSpace(audioDeviceId)
                    ? AudioConfig.FromDefaultMicrophoneInput()
                    : AudioConfig.FromMicrophoneInput(audioDeviceId);


                // Create a recognizer instance - create fresh to avoid a potentially stale previous connection
                CreateSpeechReco(_audioConfig);

                // Listen for the next utterance, stopping if operation was canceled by the invoker
                AzureRecognitionResult result = null;
                using (cancellationToken.Register(() => _speechRecognizer.StopContinuousRecognitionAsync()))
                {
                    // Timing is provided in terms of deltas over the reco start
                    _recoStartTime = DateTime.Now;
                    result = await _speechRecognizer.RecognizeOnceAsync();
                }
                cancellationToken.ThrowIfCancellationRequested();
                sr = ConvertSpeechResult(result, _recoStartTime);
            }
            catch (OperationCanceledException)
            {
                // Consumer canceled the operation - normal exit possibility
                OnCanceled?.Invoke("Operation canceled by invoking app");
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Failed to recognize: {e.Message}. Is a microphone available and enabled?");
            }
            finally
            {
                _isListening = false;
                OnListeningStateChanged?.Invoke(false);
                ReleaseSpeechReco();
            }
            OnRecognized.Invoke(sr);
        }, cancellationToken);
    }
    #endregion

    #region Private methods
    /// <summary>
    /// Create a recognizer instance
    /// </summary>
    /// <param name="audioConfig"></param>
    private void CreateSpeechReco(AudioConfig audioConfig)
    {
        _speechRecognizer = new SpeechRecognizer(_azureConfig, audioConfig);
        _speechRecognizer.SessionStarted += SpeechRecognizer_SessionStarted;
        _speechRecognizer.SessionStopped += SpeechRecognizer_SessionStopped;
        _speechRecognizer.SpeechStartDetected += SpeechRecognizer_SpeechStartDetected;
        _speechRecognizer.SpeechEndDetected += SpeechRecognizer_SpeechEndDetected;
        _speechRecognizer.Recognizing += SpeechRecognizer_Recognizing;
    }

    /// <summary>
    /// Release / destroy recognizer instance
    /// </summary>
    private void ReleaseSpeechReco()
    {
        try
        {
            if (_speechRecognizer is null)
            {
                return;
            }
            _speechRecognizer.SessionStarted -= SpeechRecognizer_SessionStarted;
            _speechRecognizer.SessionStopped -= SpeechRecognizer_SessionStopped;
            _speechRecognizer.SpeechStartDetected -= SpeechRecognizer_SpeechStartDetected;
            _speechRecognizer.SpeechEndDetected -= SpeechRecognizer_SpeechEndDetected;
            _speechRecognizer.Recognizing -= SpeechRecognizer_Recognizing;
            _speechRecognizer.Dispose();
            _speechRecognizer = null;
        }
        catch
        {
            // DOn't let cleanup glitches slow us down
        }
    }

    /// <summary>
    /// Convert native reco results to standard format 
    /// </summary>
    /// <param name="r">Native recognizer results</param>
    /// <param name="recoStartTime">Time the recognition started</param>
    /// <returns>Standard recognition results or null if no valid recognition</returns>
    private SpeechRecoResult ConvertSpeechResult(AzureRecognitionResult result, DateTime recoStartTime)
    {
        SpeechRecoResult recoResult = null;
        switch (result.Reason)
        {
            case ResultReason.RecognizedSpeech:
            case ResultReason.TranslatedSpeech:
                try
                {
                    TimeSpan start = TimeSpan.FromTicks(result.OffsetInTicks);
                    TimeSpan end = start + result.Duration;
                    DateTime startTime = recoStartTime + start;
                    DateTime endTime = recoStartTime + end; // startTime + result.Duration;

                    var detailedResults = result.Best();
                    recoResult = new SpeechRecoResult("Azure", startTime, endTime);
                    foreach (var item in detailedResults)
                    {
                        recoResult.AddAlternate(item.LexicalForm, item.Confidence, extraRecoInfo: $"NT: time: {start.TotalSeconds:0.0000} to {end.TotalSeconds:0.0000})");
                        // Add compact representation of spelled out letters, if any are present, e.g.
                        // "L D L C" -> "LDLC",
                        // "suspected I E D" -> "suspected IED"
                        // "R O Z from sixteen hundred" -> "ROZ from sixteen hundred"
                        string compacted = Utility.SpelledLettersToAcronym(item.LexicalForm);
                        if (compacted != null)
                        {
                            // Add hypothesis with lower likelihood
                            recoResult.AddAlternate(compacted, item.Confidence * 0.90, extraRecoInfo: "NT: post-processed acronym");
                        }
                    }
                    // Sort descending by confidence
                    recoResult.Results = recoResult.Results.OrderBy(item => -item.Confidence).ToList();
                }
                catch (Exception e)
                {
                    throw new InvalidDataException($"Error converting speech results: {e.Message}", e);
                }
                break;
            case ResultReason.NoMatch:
                // No speech was recognized
                recoResult = null;
                break;
            case ResultReason.Canceled:
                recoResult = null;
                // Get details and advertise
                var cancellation = CancellationDetails.FromResult(result);
                string cancelDetails = $"CANCELED: Reason={cancellation.Reason}";
                if (cancellation.Reason == CancellationReason.Error)
                {
                    cancelDetails += $" ErrorCode={cancellation.ErrorCode} ErrorDetails={cancellation.ErrorDetails}";
                }
                OnCanceled?.Invoke(cancelDetails);
                break;
        }
        return recoResult;
    }

    /// <summary>
    /// Check if the cognitive services site is reachable
    /// </summary>
    /// <returns></returns>
    private async Task<bool> IsReachableAsync()
    {
        return await Task.Run(() =>
        {
            string url = $"https://{_azureRegion}.api.cognitive.microsoft.com/sts/v1.0/issueToken";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.Headers = new WebHeaderCollection() {
                    { "sec-fecth-mode", "no-cors" },
                    { "Content-type", "application/x-www-form-urlencoded" },
                    { "Content-Length", "0" },
                    { "Ocp-Apim-Subscription-Key", _azureKey},
                };
            request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.Timeout = 2000;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    return true;
                }
            }
            catch (WebException)
            {
                //HttpWebResponse res = (HttpWebResponse)ex.Response;
                return false;
            }
        });
    }
    #endregion

    #region Azure event handlers
    /// <summary>
    /// Recognition started
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SpeechRecognizer_SessionStarted(object? sender, SessionEventArgs e)
    {
        OnListeningStateChanged?.Invoke(true);
    }

    /// <summary>
    /// 
    /// Recognition stopped
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SpeechRecognizer_SessionStopped(object? sender, SessionEventArgs e)
    {
        OnListeningStateChanged?.Invoke(false);
    }

    /// <summary>
    /// Start of speech (segment) detected
    /// </summary>
    /// <remarks>
    /// There is enough of a lag that in this particular recognizer this signal is not of great help.
    /// A more precise signal requires monitoring the audio source directly and applying a separate
    /// classifier like WebRctVad.
    /// </remarks>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SpeechRecognizer_SpeechStartDetected(object? sender, RecognitionEventArgs e)
    {
        OnSpeechStart?.Invoke();
    }

    /// <summary>
    /// End of speech (segment) detected
    /// </summary>
    /// <remarks>
    /// There is enough of a lag that in this particular recognizer this signal is not of great help.
    /// A more precise signal requires monitoring the audio source directly and applying a separate
    /// classifier like WebRctVad.
    /// </remarks>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SpeechRecognizer_SpeechEndDetected(object? sender, RecognitionEventArgs e)
    {
        OnSpeechEnd?.Invoke();
    }

    /// <summary>
    /// Partial recognition result is available
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SpeechRecognizer_Recognizing(object? sender, AzureSpeechRecognitionEventArgs e)
    {
        OnRecognizing.Invoke(e.Result.Text);
    }
    #endregion
}