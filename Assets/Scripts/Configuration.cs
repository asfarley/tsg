using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Configuration : MonoBehaviour
{
    private static Configuration _instance;

    public static Configuration Instance { get { return _instance; } }

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
        OutputFolder = "Test";
        var fullOutputPath = FullOutputPath();
        if (!Directory.Exists(FullOutputPath()))
        {
            Directory.CreateDirectory(fullOutputPath);
        }
    }

    public string FullOutputPath()
    {
		return Path.Combine(RootOutputPath, OutputFolder);
    }
}