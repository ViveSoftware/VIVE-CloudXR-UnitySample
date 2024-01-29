using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CloudXR;
using System.Text.RegularExpressions;
using System.IO;

public class CxruClientSampleAutoconnect : MonoBehaviour
{
    [SerializeField]
    private string serverAddress = "192.168.0.0";
    [SerializeField]
	private GameObject hintsObj;

	private CloudXRManager cxrManager = null;
    // Start is called before the first frame update
    private bool doConnect = false;
    void Start()
    {
		StartCoroutine(LoadIpAdress());

		cxrManager = Camera.main.GetComponent<CloudXRManager>();
        if (cxrManager == null) {
            Debug.Assert(false,"CloudXRManager is not attached to the main camera");
            return;
        }
        
        // Change this to "true" if there are color space problems on Android.
        cxrManager.config.cxrLibraryConfig.cxrDebugConfig.outputLinearRGBColor = false;

        cxrManager.server_ip = serverAddress;
        cxrManager.statusChanged += OnCxrStatusChange;
        
        // Setup bindings.
        cxrManager.SetBindings(CxruSampleBindings.oculusTouchBindings);

        //doConnect = true;
    }


    // ============================================================================
    // Managing input focus (e.g. dimming during system menu)
    // TDOO: This was used for dimming, and is actually about focus, not being paused
    private bool _isPaused = false;
    public bool isPaused {
        get {
            return _isPaused;
        }
    }     
    private bool appHasFocus = false;
    private void OnApplicationFocus(bool hasFocus) => _isPaused = hasFocus;



    // ============================================================================
    // Application pause means we should disconnect
    private bool appIsPaused = false;
    private void OnApplicationPause(bool isPaused) {
        if (cxrManager==null) { 
            Log.V($"OnApplicationPaused( {isPaused})  -- cxrManager not yet created");
        }
        else {
            Log.V($"OnApplicationPaused( {isPaused})  -- {cxrManager.currentState}");
            if (appIsPaused && (cxrManager.currentState != CloudXRManager.S.dormant)) {
                cxrManager.Disconnect();
            }
        }
        if ((!appIsPaused)) {
            // Schedule a connect.
            //doConnect = true;
        }
        appIsPaused = isPaused;
    } 

    XrPlatform? currentPlatform = null;
    void UpdatePlatform() {
        if (currentPlatform == null) {
            XrPlatform? platformCheck = cxrManager.GetXrPlatform();
            if (platformCheck == null) 
                return;
            XrPlatform runningPlatform = (XrPlatform)platformCheck;
            int actualWidth = runningPlatform.displays[0].width;
            int unityWidth = (int)cxrManager.GetUnityResolutionWidth();
            float scale = (float)actualWidth / (float)unityWidth;
            Log.W($"For platform {runningPlatform.make} {runningPlatform.model}, setting scaling factor to: {scale}");
            cxrManager.SetUnityResolutionScaling(scale);
            currentPlatform = runningPlatform;
        }
    }

    void Update()
    {
        UpdatePlatform();


        if (doConnect && (cxrManager.currentState == CloudXRManager.S.dormant)) {
            Log.W("Connecting");
            cxrManager.Connect();
        }
        if (hintsObj.activeSelf & cxrManager.currentState == CloudXRManager.S.running)
        {
			hintsObj.SetActive(false);
		}
        // TestDisconnect();
    }

    void OnCxrStatusChange(object sender, CloudXRManager.StatusChangeArgs e) {
        Debug.Log($"event fired! {e.new_status}");

        if (e.new_status == CloudXRManager.S.error) {
            Debug.Log($"Oh no! Error!");
            if (e.result != null) {
                Debug.Log($"{e.result.message}, cxr error {e.result.api_cxrError}: '{e.result.api_cxrErrorString}'");
            } 
        }

    }

	IEnumerator LoadIpAdress()
	{
		while (!doConnect)
		{
			yield return new WaitForEndOfFrame();
			string path = Application.persistentDataPath + "/CloudXRLaunchOptions.txt";
			if (File.Exists(path))
			{
				string content = File.ReadAllText(path);

				Match match = Regex.Match(content, @"-s\s+(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})");
				if (match.Success)
				{
					serverAddress = match.Groups[1].Value;
					cxrManager.server_ip = serverAddress;
					doConnect = true;
				}

				match = Regex.Match(content, @"-server\s+(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})");
				if (match.Success)
				{
					serverAddress = match.Groups[1].Value;
					cxrManager.server_ip = serverAddress;
					doConnect = true;
				}
			}
		}
	}
}
