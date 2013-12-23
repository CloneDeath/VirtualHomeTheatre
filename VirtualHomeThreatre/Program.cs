﻿using System;
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

namespace VirtualHomeThreatre
{
	class Program
	{
		static DxScreenCapture cap = null;
		static int capture_area = 0;

		static void Main(string[] args)
		{
			GraphicsManager.SetResolution(1280, 800);
			GraphicsManager.SetTitle("Capture Test");

			GraphicsManager.Instance.X = Screen.PrimaryScreen.Bounds.Width + 1;
			DisplayDevice.GetDisplay(DisplayIndex.Second).ChangeResolution(1280, 800, 32, 60.0f);
			GraphicsManager.SetWindowState(OpenTK.WindowState.Fullscreen);


			Camera2D cam2d = new Camera2D();
			cam2d.OnRender += Draw2D;

			GraphicsManager.Update += new Action(GraphicsManager_Update);

			GraphicsManager.Start();
			DisplayDevice.GetDisplay(DisplayIndex.Second).RestoreResolution();
		}

		static void GraphicsManager_Update()
		{
			if (KeyboardManager.IsPressed(OpenTK.Input.Key.Escape)) {
				GraphicsManager.Close();
			}
		}

		static void Draw2D()
		{
			if (cap == null) {
				cap = new DxScreenCapture();
			}
			cap.CaptureScreen();
			capture_area = cap.GetGLTex();
			GL.BindTexture(TextureTarget.Texture2D, capture_area);
			int sep = 100;
			GL.Begin(BeginMode.Quads);
			{
				GL.TexCoord2(0, 0); GL.Vertex2(320 - sep, 0);
				GL.TexCoord2(1, 0); GL.Vertex2(640 - sep, 0);
				GL.TexCoord2(1, 1); GL.Vertex2(640 - sep, 800);
				GL.TexCoord2(0, 1); GL.Vertex2(320 - sep, 800);
			}
			GL.Begin(BeginMode.Quads);
			{
				GL.TexCoord2(0, 0); GL.Vertex2(640 + sep, 0);
				GL.TexCoord2(1, 0); GL.Vertex2(1280 - 320 + sep, 0);
				GL.TexCoord2(1, 1); GL.Vertex2(1280 - 320 + sep, 800);
				GL.TexCoord2(0, 1); GL.Vertex2(640 + sep, 800);
			}
			GL.End();
		}

		
	}
}