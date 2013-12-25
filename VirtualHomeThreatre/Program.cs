using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;
using GLImp;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using OpenTK;
using RiftSharp;
using System.Threading;

namespace VirtualHomeThreatre
{
	class Program
	{
		static DxScreenCapture cap = null;
		static int capture_area = 0;

		const bool ChangeResolution = false;

		static void Main(string[] args)
		{
			if (ChangeResolution) {
				DisplayDevice.GetDisplay(DisplayIndex.Default).ChangeResolution(640, 480, 32, 60f);
			}
			GraphicsManager.SetResolution(1280, 800);
			GraphicsManager.SetTitle("Capture Test");

			
			GraphicsManager.Instance.X = DisplayDevice.GetDisplay(DisplayIndex.Second).Bounds.X;
			GraphicsManager.Instance.Y = DisplayDevice.GetDisplay(DisplayIndex.Second).Bounds.Y;
			if (ChangeResolution) {
				DisplayDevice.GetDisplay(DisplayIndex.Second).ChangeResolution(1280, 800, 32, 60.0f);
			}
			GraphicsManager.SetWindowState(OpenTK.WindowState.Fullscreen);
			

			Camera2D cam2d = new Camera2D();
			cam2d.OnRender += Draw2D;

			GraphicsManager.Update += new Action(GraphicsManager_Update);

			var x = RiftSharp.RiftHeadsetDevice.FindRiftDevice();
			x.OnMoveHead += MoveHead;
			cap = new DxScreenCapture();
			
			GraphicsManager.Start();
			if (ChangeResolution) {
				DisplayDevice.GetDisplay(DisplayIndex.Second).RestoreResolution();
				DisplayDevice.GetDisplay(DisplayIndex.Default).RestoreResolution();
			}
		}

		static void MoveHead(SensorFusion sensor)
		{
			
			//Console.WriteLine(report.Report.Samples[0].Accel.X);
		}

		static void GraphicsManager_Update()
		{
			if (KeyboardManager.IsPressed(OpenTK.Input.Key.Escape)) {
				GraphicsManager.Close();
			}

			cap.CaptureScreen();
			cap.GetGLTex();
		}

		static void Draw2D()
		{
			capture_area = cap.Texture;
			if (capture_area != -1) {
				GL.Enable(EnableCap.Texture2D);
				GL.Color3(Color.White);
				GL.BindTexture(TextureTarget.Texture2D, capture_area);
				int sep = 100;
				GL.Begin(BeginMode.Quads);
				{
					GL.TexCoord2(0, 0); GL.Vertex2(0 - sep, 0);
					GL.TexCoord2(1, 0); GL.Vertex2(640 - sep, 0);
					GL.TexCoord2(1, 1); GL.Vertex2(640 - sep, 800);
					GL.TexCoord2(0, 1); GL.Vertex2(0 - sep, 800);
				}
				{
					GL.TexCoord2(0, 0); GL.Vertex2(640 + sep, 0);
					GL.TexCoord2(1, 0); GL.Vertex2(1280 + sep, 0);
					GL.TexCoord2(1, 1); GL.Vertex2(1280 + sep, 800);
					GL.TexCoord2(0, 1); GL.Vertex2(640 + sep, 800);
				}
				GL.End();

				GL.Disable(EnableCap.Texture2D);
				
				var MousePos = System.Windows.Forms.Cursor.Position;
				MousePos.X = (int)((MousePos.X * 1.0f / Screen.PrimaryScreen.Bounds.Width) * 640);
				MousePos.Y = (int)((MousePos.Y * 1.0f / Screen.PrimaryScreen.Bounds.Height) * 800);
				
				GL.Color3(Color.White);
				GL.Begin(BeginMode.Triangles);
				{
					GL.Vertex2(640 + sep + MousePos.X, 0 + MousePos.Y);
					GL.Vertex2(640 + sep + MousePos.X + 10, 0 + MousePos.Y);
					GL.Vertex2(640 + sep + MousePos.X, 0 + MousePos.Y + 10);
				}
				GL.End();
				GL.Color3(Color.Black);
				GL.Begin(BeginMode.LineLoop);
				{
					GL.Vertex2(640 + sep + MousePos.X, 0 + MousePos.Y);
					GL.Vertex2(640 + sep + MousePos.X + 10, 0 + MousePos.Y);
					GL.Vertex2(640 + sep + MousePos.X, 0 + MousePos.Y + 10);
				}
				GL.End();
			}
		}

		
	}
}
