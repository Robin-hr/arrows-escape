using System;
using ArrowEscape.Core;
using ArrowEscape.Data;
using ArrowEscape.UI;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
namespace ArrowEscape.Tests
{
    /// <summary>Complete app with three fake stores, optionally including actual menu/shop UI.</summary>
    public sealed class EconomyTestScene : IDisposable
    {
        private readonly GameObject host,camera,ui,ownEventSystem;
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public readonly FakeProgressStore Progress;
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public readonly FakeInventoryStore Inventory;
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public readonly GameBootstrap Bootstrap;
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public readonly GameController Game;
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public readonly BoardView Board;
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public readonly AppFlowController Flow;
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public readonly HudController Hud;
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public EconomyTestScene(bool withUi=false,FakeInventoryStore inventory=null,FakeProgressStore progress=null)
        {
            Inventory=inventory??new FakeInventoryStore();Progress=progress??new FakeProgressStore();
            camera=new GameObject("Economy camera",typeof(Camera));camera.tag="MainCamera";
            host=new GameObject("Economy gameplay");Bootstrap=host.AddComponent<GameBootstrap>();Bootstrap.SetLevelCatalog(OriginalTestCatalog.Paths, OriginalTestCatalog.LegacyPaths);
            Bootstrap.Initialize(Progress,new FakeSettingsStore(),Inventory);Game=host.GetComponent<GameController>();Board=host.GetComponent<BoardView>();
            if(withUi)
            {
                var old=UnityEngine.Object.FindFirstObjectByType<EventSystem>();ui=new GameObject("Economy UI");Hud=ui.AddComponent<HudController>();Flow=ui.AddComponent<AppFlowController>();Flow.Initialize(Bootstrap,Hud);
                if(old==null)ownEventSystem=UnityEngine.Object.FindFirstObjectByType<EventSystem>().gameObject;
            }
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public void Tap(int x,int y){Vector2 world=Board.CellToWorld(x,y);var taps=host.GetComponent<TapController>();taps.PointerDown(1,world);taps.PointerUp(1,world);}
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public void Clear(int wasted=0)
        {
            for(int i=0;i<wasted;i++)Tap(0,0);
            while(!Game.IsCleared)
            {
                Arrow free=null;foreach(Arrow a in Game.Session.Board.Arrows)if(Game.Session.Board.CanExit(a)){free=a;break;}
                Assert.That(free,Is.Not.Null);if(Game.Session.Board.Arrows.Count==1)LogAssert.Expect(LogType.Log,"Level cleared. Mistakes: "+Game.MistakeCount);Tap(free.X,free.Y);
            }
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public void Dispose(){UnityEngine.Object.DestroyImmediate(ui);UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(camera);UnityEngine.Object.DestroyImmediate(ownEventSystem);SpriteFactory.ClearCache();}
    }
}
