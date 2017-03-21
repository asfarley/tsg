using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class VideoExport : MonoBehaviour
{
    private const int FrameRate = 25;
    private const string RootOutputPath = @"C:\TSG"; // TODO: get from settings
    private const string AviGeneratorPathname = @"S:\Projects\Traffic\PngToAvi.exe"; // TODO: get from settings
    private string _outputFolder;
    private bool _launched;

    // Use this for initialization
    void Start ()
    {

        _launched = false;
        if (! File.Exists(AviGeneratorPathname))
            throw new ApplicationException("Cannot find AVI generator. Fix the hardcoded path.");

		//Create movie file
        Time.captureFramerate = FrameRate;
        _outputFolder = Path.Combine(RootOutputPath, Guid.NewGuid().ToString("N").ToUpperInvariant());
        Directory.CreateDirectory(_outputFolder);

        _launched = true;
    }
	
	// Update is called once per frame
    void Update () {
		//Append frame

        if (! _launched) return;

        // add zeroes to beginning to simplify sort
        Application.CaptureScreenshot(Path.Combine(_outputFolder, Time.frameCount.ToString("D8") + ".png"));
    }

    void OnDestroy()
    {
        if (!_launched) return;
        var aviName = Path.GetFileName(_outputFolder) + ".avi";

        var processStartInfo = new ProcessStartInfo(AviGeneratorPathname)
        {
            Arguments = string.Format("\"{0}\" \"{1}\" {2}", _outputFolder, Path.Combine(RootOutputPath, aviName), FrameRate),
            UseShellExecute = false,
            LoadUserProfile = false,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        var process = Process.Start(processStartInfo);
    }

    //private static VideoExport _instance;

    //public static VideoExport Instance { get { return _instance; } }

    //public void Awake()
    //{
    //    if (_instance != null && _instance != this)
    //    {
    //        Destroy(this.gameObject);
    //    }
    //    else {
    //        _instance = this;
    //    }

    //    CreateNewOutputFolder();
    //}

    //public void CreateNewOutputFolder()
    //{
    //    _outputFolder = "Test";
    //    var fullOutputPath = FullOutputPath();
    //    if (!Directory.Exists(fullOutputPath))
    //    {
    //        Directory.CreateDirectory(fullOutputPath);
    //    }
    //}

    //public string FullOutputPath()
    //{
    //    return RootOutputPath + @"\" + _outputFolder;
    //}
}
