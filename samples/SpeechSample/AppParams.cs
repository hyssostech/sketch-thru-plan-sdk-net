using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StpSDKSample;

public class AppParams : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    #region Properties
    /// <summary>
    /// The STP connection string value
    /// </summary>
    [Category("STP Settings"), Description("STP Connection String"), DisplayName("STP Connection")]
    public string StpConnection
    {
        get => _stpConnection;
        set
        {
            _stpConnection = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Map image file path
    /// </summary>
    [CategoryAttribute("Map Settings"), DescriptionAttribute("Path to map image file"), DisplayName("Map image path")]
    public string MapImagePath
    {
        get => _mapImagePath;
        set
        {
            _mapImagePath = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Map's top latitude    
    /// </summary>
    [CategoryAttribute("Map Settings"), DescriptionAttribute("Top Latitude"), DisplayName("Map's top latitude")]
    public double MapTopLat
    {
        get => _mapTopLat;
        set
        {
            _mapTopLat = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Map's right longitude    
    /// </summary>
    [CategoryAttribute("Map Settings"), DescriptionAttribute("Right Longitude"), DisplayName("Map's right longitude")]
    public double MapRightLon
    {
        get => _mapRightLon;
        set
        {
            _mapRightLon = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Map's top latitude    
    /// </summary>
    [CategoryAttribute("Map Settings"), DescriptionAttribute("Bottom Latitude"), DisplayName("Map's bottom latitude")]
    public double MapBottomLat
    {
        get => _mapBottomLat;
        set
        {
            _mapBottomLat = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Map's left longitude    
    /// </summary>
    [CategoryAttribute("Map Settings"), DescriptionAttribute("Left Longitude"), DisplayName("Map's left longitude")]
    public double MapLeftLon
    {
        get => _mapLeftLon;
        set
        {
            _mapLeftLon = value;
            OnPropertyChanged();
        }
    }

    [CategoryAttribute("Speech Settings"), DescriptionAttribute("Speech Recognizer Key"), DisplayName("Azure Speech Recognizer Subscription Key")]
    public string AzureKey
    {
        get => _azureKey;
        set
        {
            _azureKey = value;
            OnPropertyChanged();
        }
    }

    [CategoryAttribute("Speech Settings"), DescriptionAttribute("Speech Recognizer Region"), DisplayName("Azure Speech Recognizer Region")]
    public string AzureRegion
    {
        get => _azureRegion;
        set
        {
            _azureRegion = value;
            OnPropertyChanged();
        }
    }


    [CategoryAttribute("Speech Settings"), DescriptionAttribute("Speech Recognizer Language"), DisplayName("Azure Speech Recognizer Language")]
    public string AzureLang
    {
        get => _azureLang;
        set
        {
            _azureLang = value;
            OnPropertyChanged();
        }
    }

    [CategoryAttribute("Speech Settings"), DescriptionAttribute("Speech Recognizer Endpoint"), DisplayName("Azure Speech Custom Endpoint")]
    public string AzureEndpoint
    {
        get => _azureEndpoint;
        set
        {
            _azureEndpoint = value;
            OnPropertyChanged();
        }
    }


    private string _stpConnection;

    private string _mapImagePath;
    private double _mapTopLat;
    private double _mapLeftLon;
    private double _mapBottomLat;
    private double _mapRightLon;

    private string _azureKey;
    private string _azureRegion;
    private string _azureLang;
    private string _azureEndpoint;
    #endregion Properties

    // Create the OnPropertyChanged method to raise the event
    // The calling member's name will be used as the parameter.
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
