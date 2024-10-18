using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using PlayFi.Web;

namespace PlayFi
{
	public class ScreenshotsCollector : MonoBehaviour
	{
		[SerializeField] private bool isEnabled = true;
		[SerializeField, Range(0.1f,60f)] float screenshotTimeoutInSec = 15f;
		[SerializeField] private string modelId;
		[SerializeField] private bool isDebugMode;
		
		private string payloadJson;
		private ScreenshotWithData uploadingScreenshotWithData = null;
		private ScreenshotWithData lastScreenshotWithData = null;
		private bool isUploading = false;
		private bool isMakingScreenshot = false;
		private float timeAccumulated = 0f;
		private LimitedQueue<ScreenshotWithData> queue;
		private bool isInitialized = false;
		
		private static ScreenshotsCollector instance;
		private const int QueueLimit = 1;
		private Uploader uploader;
		
		public static ScreenshotsCollector Instance
		{
			get
			{
				if (!Application.isPlaying) 
					instance = FindObjectOfType<ScreenshotsCollector>();

				if (instance != null) return instance;
				
				var go = new GameObject
				{
					name = "PlayFiScreenshotsCollector"
				};
				go.AddComponent<ScreenshotsCollector>();
				instance = go.GetComponent<ScreenshotsCollector>();
				
				#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(go);
				#endif
				
				
				return instance;
			}
		}
		
		private void Awake()
		{
			if (instance != null && instance != this)
			{
				Destroy(this);
				return;
			}
			instance = this;
		}
		
		private void Start()
		{
			Initialize();
			DontDestroyOnLoad(gameObject);
			
			StartCoroutine(DoScreenshotLoop());
			StartCoroutine(DoUploadLoop());
		}

		private void Update()
		{
			timeAccumulated += Time.unscaledDeltaTime;
			if (!isEnabled)
				timeAccumulated = float.MaxValue;
		}

		private void OnDisable()
		{
			ScreenshotsMaker.Dispose();
		}

		public Action screenshotProcessingStart;
		public Action screenshotProcessingFinish;
		
		public float ScreenshotTimeoutInSec => screenshotTimeoutInSec;
		public bool IsEnabled
		{
			get => isEnabled;
			set
			{
				if (value && !isEnabled)
					timeAccumulated = float.MaxValue;
				isEnabled = value; 
			}
		}
		
		public string PayloadJson => payloadJson;

		public bool IsMakingScreenshot => isMakingScreenshot;

		public string ModelId => modelId;

		public void ProcessScreenshot()
		{
			Initialize();
			if (!isMakingScreenshot)
				StartCoroutine(DoProcessScreenshot());
		}
		
		public void Enable(bool isEnabled)
		{
			IsEnabled = isEnabled;
		}
		
		public void SetTimeout(float screenshotTimeoutInSec)
		{
			this.screenshotTimeoutInSec = screenshotTimeoutInSec;
		}

		public void SetModelId(string modelId)
		{
			this.modelId = modelId;
		}
		
		public void SetPayloadJson(string payloadJson)
		{
			this.payloadJson = ValidateJson(payloadJson);
		}

		private string ValidateJson(string json)
		{
			if (IsJsonValid(json))
				return json;
			
			Debug.LogWarning("Input string is not valid JSON!");
			return null;
		}

		private IEnumerator DoScreenshotLoop()
		{
			while (true)
			{
				yield return new WaitUntil(() => IsEnabled);
				timeAccumulated = 0;
				ProcessScreenshot();
				yield return new WaitUntil(() => timeAccumulated > screenshotTimeoutInSec);
			}
		}

		private IEnumerator DoUploadLoop()
		{
			while (true)
			{
				yield return new WaitUntil(()=>!isUploading);

				if (uploadingScreenshotWithData == null && queue.Count > 0)
					uploadingScreenshotWithData = queue.Dequeue();

				if (uploadingScreenshotWithData != null)
					UploadScreenshot(uploadingScreenshotWithData);
			}
		}

		private IEnumerator DoProcessScreenshot()
		{
			isMakingScreenshot = true;
			yield return new WaitForEndOfFrame();
			screenshotProcessingStart?.Invoke();
			ScreenshotsMaker.MakeScreenshotAsync(png =>
			{
				if (png !=null && !AreByteArraysEqual(png, lastScreenshotWithData?.Image))
				{
					lastScreenshotWithData = new ScreenshotWithData(png, ModelId, payloadJson);
					queue.Enqueue(lastScreenshotWithData);
				}
				isMakingScreenshot = false;
				screenshotProcessingFinish?.Invoke();
			}, isLogging: isDebugMode);
		}
		
		private void UploadScreenshot(ScreenshotWithData screenshotWithData)
		{
			isUploading = true;
			
			screenshotWithData.Payload = ValidateJson(screenshotWithData.Payload);
			
			StartCoroutine(uploader.DoUpload(screenshotWithData.Image, screenshotWithData.ModelId, screenshotWithData.Payload, isDebugMode, result =>
			{
				if (result)
					uploadingScreenshotWithData = null;
				isUploading = false;
			}));
		}

		private bool AreByteArraysEqual(byte[] a1, byte[] a2)
		{
			if (a1 == null || a2 == null)
				return false;
			return a1.SequenceEqual(a2);
		}
		
		private bool IsJsonValid(string jsonString)
		{
			try
			{
				JsonUtility.FromJsonOverwrite(jsonString, new object());
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
		
		private void Initialize()
		{
			if (isInitialized)
				return;
			
			queue = new LimitedQueue<ScreenshotWithData>(QueueLimit);
			uploadingScreenshotWithData = null;
			lastScreenshotWithData = null;
			uploader = gameObject.AddComponent<Uploader>();
			isInitialized = true;
		}
	}
}