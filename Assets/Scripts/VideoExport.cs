using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class VideoExport : MonoBehaviour {

	// Use this for initialization
	void Start () {
		//Create movie file
	}
	
	// Update is called once per frame
	void Update () {
		//Append frame
	}

    private static VideoExport _instance;

    public static VideoExport Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else {
            _instance = this;
        }

        CreateNewOutputFolder();
    }

    public string RootOutputPath = @"C:\TSG";
    public string OutputFolder = @"";

    public void CreateNewOutputFolder()
    {
        //OutputFolder = DateTime.Now.ToString("O");
        OutputFolder = "Test";
        var fullOutputPath = FullOutputPath();
        if (!Directory.Exists(FullOutputPath()))
        {
            Directory.CreateDirectory(fullOutputPath);
        }
    }

    public string FullOutputPath()
    {
        return RootOutputPath + @"\" + OutputFolder;
    }
}
