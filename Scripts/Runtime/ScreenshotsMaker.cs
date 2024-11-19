using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Lift
{
	internal static class ScreenshotsMaker
	{
		private static bool isLoggingEnabled;
		private static NativeArray<byte> buffer;
		
		internal static async void MakeScreenshotAsync(Action<byte[]> resultCallback = null, bool isLogging = false) 
		{
			isLoggingEnabled = isLogging;
			int h = Screen.height;
			int w = Screen.width;

			if (!Application.isPlaying)
				return;

			Logging($"Start {Time.realtimeSinceStartup}");
			var rt = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.Default);
			Logging($"RT created {Time.realtimeSinceStartup}");
			ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);
	
			Logging($"Captured screenshot to RT {Time.realtimeSinceStartup}");
			
			byte[] data;
			if (IsRenderTextureFlipped())
			{
				var flipRt = FlipRenderTexture(rt);
				Object.Destroy(rt);
				data = await ScreenshotsMaker.GetPngDataAsync(flipRt, flipRt.graphicsFormat, w, h);
			}
			else
				data = await ScreenshotsMaker.GetPngDataAsync(rt, rt.graphicsFormat, w, h);
			
			Logging($"Finish {Time.realtimeSinceStartup}");
			resultCallback?.Invoke(data);
		}

		internal static void Dispose()
		{
			if (buffer.IsCreated)
				buffer.Dispose();
		}
		
		private static RenderTexture FlipRenderTexture(RenderTexture source)
		{
			var (scale, offs) = (new Vector2(1, -1), new Vector2(0, 1));
			var (w, h) = (Screen.width, Screen.height);
			var destination = new RenderTexture(source.width, source.height, 0, source.format);
			Graphics.Blit(source, destination, scale, offs);
			return destination;
		}

		private static async Task<byte[]> GetPngDataAsync(RenderTexture rt, GraphicsFormat format, int w, int h)
		{
			buffer = new NativeArray<byte>(Screen.width * Screen.height * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
			byte[] data = Array.Empty<byte>();
			
			try
			{
				Logging($"AsyncGPUReadbackRequest start {Time.realtimeSinceStartup}");
				var tcs = new TaskCompletionSource<bool>();
				AsyncGPUReadbackRequest request = AsyncGPUReadback.RequestIntoNativeArray(ref buffer, rt,0, textureReadback =>
				{
					tcs.SetResult(textureReadback.done);
				});
			
				// Wait for AsyncGPUReadbackRequest to finish
				await tcs.Task;
			
				Logging($"AsyncGPUReadbackRequest finish {Time.realtimeSinceStartup}");
			
				if (request.hasError)
				{
					Debug.LogError("AsyncGPUReadbackRequest has error!");
					return null;
				}
				
				// Create the new thread, and run it in the background!
				Thread encodeImgThread = new Thread(()=>
				{
					data = EncodeImg(ref buffer, format, w, h);
				});
				
				encodeImgThread.IsBackground = true;
			
				Logging($"encodeImgThread start {Time.realtimeSinceStartup}");
				encodeImgThread.Start();

				// This looks goofy but it awaits the image encoder thread
				await Task.Run(() =>
				{
					while (encodeImgThread.IsAlive) ;
				});

				Logging($"encodeImgThread finish {Time.realtimeSinceStartup}");
				
				return data;
			}
			catch (Exception e)
			{
				Debug.LogError($"ScreenShotMaker error: {e}");
				throw;
			}
			finally
			{
				Logging($"dispose start {Time.realtimeSinceStartup}");
				Dispose();
				if (rt != null) 
					Object.Destroy(rt);
				Logging($"dispose finish {Time.realtimeSinceStartup}");
			}
		}

		private static byte[] EncodeImg(ref NativeArray<byte> buffer, GraphicsFormat format, int w, int h)
		{
			try
			{
				var encodedData = ImageConversion.EncodeNativeArrayToJPG(buffer, format, (uint) w, (uint) h).ToArray();
				if (encodedData == null || encodedData.Length <= 0)
					Debug.LogError($"Encoding failed! data was null or length 0!");
				return encodedData;
			}
			catch (Exception e)
			{
				Debug.LogError($"Encoding failed! {e.Message}");
				return null;
			}
		}

		static bool IsRenderTextureFlipped()
		{
			bool isFlipped = true;
			switch (SystemInfo.graphicsDeviceType)
			{
				case GraphicsDeviceType.OpenGLCore:
				case GraphicsDeviceType.OpenGLES2:
				case GraphicsDeviceType.OpenGLES3:
					isFlipped = false;
					break;
			}
			return isFlipped;
		}

		static void Logging(string str)
		{
			if (!isLoggingEnabled)
				return;
			Debug.Log($"Lift ScreenshotMaker: {str}");
		}
	}
}