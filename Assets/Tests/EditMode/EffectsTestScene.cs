using System;
using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Runtime-only effects fixture with real gameplay, fake audio and an owned main camera.</summary>
    public sealed class EffectsTestScene : IDisposable
    {
        private readonly Camera[] previousCameras;
        private readonly string[] tags;
        private readonly GameObject cameraHost;
        /// <summary>Existing gameplay helper; never touches real save files.</summary>
        public AudioTestScene Gameplay { get; }
        /// <summary>Independent feedback root outside the board root.</summary>
        public GameObject Host { get; }
        /// <summary>Sprite effect owner.</summary>
        public EffectsController Effects { get; }
        /// <summary>Observing gameplay director.</summary>
        public EffectsDirector Director { get; }
        /// <summary>The test camera.</summary>
        public Camera Camera { get; }
        /// <summary>The current board view.</summary>
        public BoardView View => Gameplay.Host.GetComponent<BoardView>();
        /// <summary>Creates isolated runtime objects and deterministic initial colors.</summary>
        public EffectsTestScene(Board board = null)
        {
            previousCameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);tags=new string[previousCameras.Length];
            for(int i=0;i<tags.Length;i++){tags[i]=previousCameras[i].tag;previousCameras[i].tag="Untagged";}
            cameraHost=new GameObject("Effects camera",typeof(Camera));cameraHost.tag="MainCamera";Camera=cameraHost.GetComponent<Camera>();Camera.backgroundColor=ViewTheme.Background;
            Gameplay=new AudioTestScene(board);Host=new GameObject("Effects feedback");Effects=Host.AddComponent<EffectsController>();Director=Host.AddComponent<EffectsDirector>();Director.Initialize(Gameplay.Game,View,Effects);
        }
        /// <summary>Restores all editor camera tags and removes owned objects.</summary>
        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(Host);Gameplay.Dispose();UnityEngine.Object.DestroyImmediate(cameraHost);
            for(int i=0;i<tags.Length;i++)if(previousCameras[i]!=null)previousCameras[i].tag=tags[i];
        }
    }
}
