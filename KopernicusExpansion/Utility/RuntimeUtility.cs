﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Kopernicus;

namespace KopernicusExpansion.Utility
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class KopE_RuntimeUtil : MonoBehaviour
	{
		static bool buttonAdded = false;

		void Start()
		{
			DontDestroyOnLoad (this);

			foreach (var body in PSystemManager.Instance.localBodies)
			{
				if (body.pqsController != null)
					pqsPlanets.Add (body);
			}

//			string scaledShader = System.IO.File.ReadAllText (KSPUtil.ApplicationRootPath + "GameData/KopernicusExpansion/Compiled-EmissiveScaled.shader");
//			string quadShader = System.IO.File.ReadAllText (KSPUtil.ApplicationRootPath + "GameData/KopernicusExpansion/Compiled-EmissiveQuad.shader");
//
//			var quadMaterial = new Material (quadShader);
//			quadMaterial.SetColor ("_Color", new Color (1.000f, 0.212f, 0.976f));
//			quadMaterial.SetFloat ("_Brightness", 1.4f);
//			quadMaterial.SetFloat ("_Transparency", 0.6f);
//
//			var pqsValklipperOcean = PSystemManager.Instance.localBodies.Find (p => p.name == "Valklipper").pqsController.ChildSpheres[0];
//			var gameobj = new GameObject ("EmissiveOceanFX");
//			var mod = gameobj.AddComponent<PQSMod_EmissiveOceanFX> ();
//			mod.sphere = pqsValklipperOcean;
//			mod.modEnabled = true;
//			mod.order = 1000000;
//			mod.EmissiveMaterial = quadMaterial;
//			gameobj.transform.parent = pqsValklipperOcean.transform;
//			gameobj.layer = Kopernicus.Constants.GameLayers.LocalSpace;
//			pqsValklipperOcean.RebuildSphere ();
//
//			var scaledValklipper = PSystemManager.Instance.scaledBodies.Find (p => p.name == "Valklipper");
//			maskTexture = GameDatabase.Instance.GetTexture ("KopernicusExpansion/ValklipperOceanMask", false);
//			if (scaledValklipper != null)
//			{
//				var mat = scaledValklipper.renderer.sharedMaterial;
//				var emMat = new Material (scaledShader);
//				emMat.SetTexture ("_EmissiveMap", maskTexture);
//				emMat.SetTextureScale ("_EmissiveMap", Vector2.one);
//				emMat.SetTextureOffset ("_EmissiveMap", Vector2.zero);
//				emMat.SetColor ("_Color", new Color (1.000f, 0.212f, 0.976f));
//				emMat.SetFloat ("_Brightness", 1.25f);
//				emMat.SetFloat ("_Transparency", 0.75f);
//				scaledValklipper.renderer.materials = new Material[]{ mat, emMat };
//			}
//			else
//				Debug.LogError ("didn't find Valklipper");

			//note: there are apparently null entries in this list for some reason, hence the check
			//FindObjectOfType<AssetBase>().textures.Where(t => t != null).ToList().ForEach(t => Debug.Log("AssetBase: " + t.name));

			if (!buttonAdded) {
				ApplicationLauncher.Instance.AddModApplication (delegate {
					showWindow = true;
				}, delegate {
					showWindow = false;
				}, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.TRACKSTATION | ApplicationLauncher.AppScenes.SPACECENTER, AssetBase.GetTexture ("scienceicon"));
				buttonAdded = true;
			}
		}

		List<CelestialBody> pqsPlanets = new List<CelestialBody>();
		int[] possibleResolutions = new int[]{512, 1024, 2048};

		GUISkin skin = HighLogic.Skin;

		bool showWindow = false;
		Rect exportRect = new Rect(200f, 200f, 200f, 140f);
		void OnGUI()
		{
			GUI.skin = skin;
			if (showWindow)
				exportRect = GUILayout.Window ("KopernicusExpansion_RuntimeUtility".GetHashCode (), exportRect, ExportWindow, "PQS Maps");
			if (showTextureWindow)
			{
				texWindowRect = GUILayout.Window ("KopernicusExpansion_TextureViewer".GetHashCode (), texWindowRect, TextureWindow, "Texture Viewer");
				if (texture != null)
					GUI.DrawTexture (new Rect (10f, 10f, 512f, 512f), texture);
			}
		}

		int pqsIndex = 0;
		int resIndex = 1;
		void ExportWindow(int id)
		{
			GUILayout.BeginHorizontal (GUILayout.ExpandWidth(true));
			if (GUILayout.Button ("<", GUILayout.Width (30f), GUILayout.Height(30f)))
			{
				pqsIndex--;

				if (pqsIndex < 0)
					pqsIndex = pqsPlanets.Count - 1;
				if (pqsIndex >= pqsPlanets.Count)
					pqsIndex = 0;
			}

			GUILayout.Label (pqsPlanets[pqsIndex].theName, GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(30f));

			if (GUILayout.Button (">", GUILayout.Width (30f), GUILayout.Height(30f)))
			{
				pqsIndex++;

				if (pqsIndex < 0)
					pqsIndex = pqsPlanets.Count - 1;
				if (pqsIndex >= pqsPlanets.Count)
					pqsIndex = 0;
			}
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal (GUILayout.ExpandWidth(true));
			if (GUILayout.Button ("<", GUILayout.Width (30f), GUILayout.Height(30f)))
			{
				resIndex--;

				if (resIndex < 0)
					resIndex = possibleResolutions.Length - 1;
				if (resIndex >= possibleResolutions.Length)
					resIndex = 0;
			}

			GUILayout.Label (possibleResolutions[resIndex].ToString() + " x " + (possibleResolutions[resIndex] / 2).ToString(), GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(30f));

			if (GUILayout.Button (">", GUILayout.Width (30f), GUILayout.Height(30f)))
			{
				resIndex++;

				if (resIndex < 0)
					pqsIndex = possibleResolutions.Length - 1;
				if (resIndex >= possibleResolutions.Length)
					resIndex = 0;
			}
			GUILayout.EndHorizontal ();

			if (GUILayout.Button ("Export Textures"))
			{
				Directory.CreateDirectory (KSPUtil.ApplicationRootPath + "/GameData/Kopernicus/Cache/PluginData/");
				PQS pqs = pqsPlanets [pqsIndex].pqsController;
				var texs = pqs.CreateMaps (possibleResolutions [resIndex], pqs.mapMaxHeight, pqsPlanets [pqsIndex].ocean, pqs.mapOceanHeight, pqs.mapOceanColor);
				var texNorm = Kopernicus.Utility.BumpToNormalMap (texs [1], 9);
				string bodyName = pqsPlanets [pqsIndex].bodyName;

				File.WriteAllBytes (KSPUtil.ApplicationRootPath + "GameData/Kopernicus/Cache/PluginData/" + bodyName + "_Color.png", texs [0].EncodeToPNG ());
				File.WriteAllBytes (KSPUtil.ApplicationRootPath + "GameData/Kopernicus/Cache/PluginData/" + bodyName + "_Height.png", texs [1].EncodeToPNG ());
				File.WriteAllBytes (KSPUtil.ApplicationRootPath + "GameData/Kopernicus/Cache/PluginData/" + bodyName + "_Normal.png", texNorm.EncodeToPNG ());
			}

			GUI.DragWindow ();
		}

		bool showTextureWindow = false;
		Rect texWindowRect = new Rect (200f, 200f, 200f, 110f);
		string textureName = "";
		Texture2D texture;
		void TextureWindow(int id)
		{
			textureName = GUILayout.TextField (textureName);
			if (GUILayout.Button ("Print Texture"))
			{
				texture = UnityEngine.Resources.FindObjectsOfTypeAll<Texture2D> ().Where (t => t.name == textureName).First ();
				if (texture == null)
				{
					ScreenMessages.PostScreenMessage ("<color=red>Error: texture " + textureName + " not found.</color>");
				}
			}

			GUI.DragWindow ();
		}




		//debugging crap
		void Update()
		{
			bool isModDown = GameSettings.MODIFIER_KEY.GetKey ();

			if (isModDown && Input.GetKeyDown (KeyCode.Alpha1))
			{
				foreach (var tex in UnityEngine.Resources.FindObjectsOfTypeAll<Texture2D>())
				{
					Utils.Log ("Texture: " + tex.name);
				}
			}
			if (isModDown && Input.GetKeyDown (KeyCode.Alpha2))
			{
				showTextureWindow = !showTextureWindow;
			}
			if (isModDown && Input.GetKeyDown (KeyCode.Alpha3))
			{
				Utils.Log ("printing PQSLandControls now...");
				foreach (var hcm in PQSMod.FindObjectsOfType<PQSLandControl>())
				{
					Utils.Log ("LandControl: " + hcm.sphere.name + ": altitudeBlend: " + hcm.altitudeBlend + ", latitudeBlend: " + hcm.latitudeBlend + ", longitudeBlend: " + hcm.longitudeBlend);
					foreach (var lc in hcm.landClasses)
					{
						Utils.Log ("==== LC: " + lc.landClassName);
						Utils.Log ("==== LatitudeRange:" + lc.latitudeRange.startStart + " => " + lc.latitudeRange.startEnd + " ===\n"
							+ lc.latitudeRange.endStart + " => " + lc.latitudeRange.endEnd
						);
						Utils.Log ("==== LongitudeRange:" + lc.longitudeRange.startStart + " => " + lc.longitudeRange.startEnd + " ===\n"
							+ lc.longitudeRange.endStart + " => " + lc.longitudeRange.endEnd
						);
						Utils.Log ("==== AltitudeRange:" + lc.altitudeRange.startStart + " => " + lc.altitudeRange.startEnd + " ===\n"
							+ lc.altitudeRange.endStart + " => " + lc.altitudeRange.endEnd
						);
						Utils.Log ("==== coverageBlend: " + lc.coverageBlend);
						Utils.Log ("==== color: " + lc.color);
						Utils.Log ("==== noiseColor: " + lc.noiseColor);
						Utils.Log ("==== noiseFrequency: " + lc.noiseFrequency);
					}
				}
			}
			if (isModDown && Input.GetKeyDown (KeyCode.Alpha4))
			{
				Utils.Log ("printing Meshes now");
				foreach (var mesh in UnityEngine.Resources.FindObjectsOfTypeAll<Mesh>())
				{
					Utils.Log (mesh.name);
				}
			}
			if (isModDown && Input.GetKeyDown (KeyCode.Alpha5))
			{
				Utils.Log (MapView.OrbitLinesMaterial.renderQueue);
				Utils.Log (MapView.DottedLinesMaterial.renderQueue);
			}
		}
	}
}

