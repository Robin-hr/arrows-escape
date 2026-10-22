using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.UI;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Live previews are bounded above fully visible names and purchase-state labels.</summary>
    public sealed class ElementalShopTests
    {
        /// <summary>Every arrow card uses a separate masked area and preserves both text rows.</summary>
        [Test] public void EveryCardClipsPreviewAboveLabels()
        {
            using(var scene=new EconomyTestScene(withUi:true))
            {
                scene.Flow.OpenShop();Canvas.ForceUpdateCanvases();var corners=new Vector3[4];
                foreach(var card in scene.Flow.Shop.Cards)
                {
                    var area=(RectTransform)card.Preview.transform.parent;Assert.That(area.name,Is.EqualTo("PreviewArea"));Assert.That(area.GetComponent<UnityEngine.UI.Mask>(),Is.Not.Null);Assert.That(area.anchorMin.y,Is.GreaterThan(.35f));Assert.That(card.Rect.Find("Face/Name"),Is.Not.Null);Assert.That(card.Rect.Find("Face/State"),Is.Not.Null);
                    card.Preview.rectTransform.GetWorldCorners(corners);var box=new Vector3[4];area.GetWorldCorners(box);Assert.That(corners[0].y,Is.GreaterThanOrEqualTo(box[0].y-.01f),card.Item.Id);Assert.That(corners[2].y,Is.LessThanOrEqualTo(box[2].y+.01f),card.Item.Id);
                    if(ArrowSkinRegistry.Find(card.Item.Id)?.IsElemental==true)Assert.That(card.Preview.texture,Is.TypeOf<RenderTexture>());
                }
            }
        }
        /// <summary>Each Elemental has an italic signature description and an isolated dark live board.</summary>
        [Test] public void SignatureDescriptionsAndLiveDetail()
        {
            using(var scene=new EconomyTestScene(withUi:true))
            {
                scene.Flow.OpenShop();for(int i=0;i<scene.Flow.Shop.Cards.Count;i++){var item=scene.Flow.Shop.Cards[i].Item;if(ArrowSkinRegistry.Find(item.Id)?.IsElemental!=true)continue;scene.Flow.Shop.OpenDetail(i);Assert.That(scene.Flow.Shop.Detail.HasLivePreview,Is.True);var text=scene.Flow.Shop.Detail.Root.GetComponentsInChildren<UnityEngine.UI.Text>().Single(x=>x.name=="Signature");Assert.That(text.text,Is.EqualTo(ArrowSkinRegistry.Find(item.Id).Signature));Assert.That(text.fontStyle,Is.EqualTo(FontStyle.Italic));}
            }
        }
        /// <summary>Each recipe's background is temporary, and Classic returns to Paper.</summary>
        [TestCase("frostbite",11,22,32)][TestCase("plasma_bolt",7,20,24)][TestCase("void_whisper",14,12,20)]
        public void DarkBackgroundAndClassic(string id,int r,int g,int b)
        {
            using(var scene=new EconomyTestScene())
            {scene.Bootstrap.ShowLevel(0);scene.Board.SetArrowSkin(id);Assert.That(Camera.main.backgroundColor,Is.EqualTo((Color)new Color32((byte)r,(byte)g,(byte)b,255)));scene.Board.SetArrowSkin("classic");Assert.That(Camera.main.backgroundColor,Is.EqualTo(ViewTheme.Background));}
        }
    }
}
