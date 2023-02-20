using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StpSDKSample;

public class AppParams : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    #region Properties
    /// <summary>
    /// The STP host value
    /// </summary>
    [CategoryAttribute("STP Settings"), DescriptionAttribute("STP Host"), DisplayName("Host")]
    public string StpHost 
    { 
        get => _stpHost; 
        set 
        { 
            _stpHost = value; 
            OnPropertyChanged();  
        } 
    }

    /// <summary>
    /// The STP port value
    /// </summary>
    [CategoryAttribute("STP Settings"), DescriptionAttribute("STP Port"), DisplayName("Port")]
    public int StpPort
    {
        get => _stpPort;
        set
        {
            _stpPort = value;
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

    private string _stpHost;
    private int _stpPort;
    private string _mapImagePath;
    private double _mapTopLat;
    private double _mapLeftLon;
    private double _mapBottomLat;
    private double _mapRightLon;

    #endregion Properties

    // Create the OnPropertyChanged method to raise the event
    // The calling member's name will be used as the parameter.
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
