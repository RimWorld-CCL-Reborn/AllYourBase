using System.Collections.Generic;
using System.Linq;
using System.Xml;
using RimWorld;
using Verse;

namespace AllYourBase
{
    //using JetBrains.Annotations;

    [StaticConstructorOnStartup]
    //[UsedImplicitly]
    internal static class XmlSharedBaseDetection
    {
        /// <summary>
        /// INTENT: Decrease compatibility issues caused by modders overwriting (abstract) bases, by making it easy to detect the existence of these bad practices.
        /// 
        /// Run once on startup. Creates list of all (abstract) bases in vanilla, then compares with bases in mods. Warns with mod name, base and exact filename for any matches found. 
        /// </summary>
        static XmlSharedBaseDetection()
        {
            //since some people have such wonderfully organised mod lists they hit the 1k limit on error logging:
            Log.ResetMessageCount();

            //get all bases in vanilla.
            List<string> vanillaXmlAttributes = new List<string>();
            //Dictionary<string, string> vanillaXmlAttributes = new Dictionary<string, string>();

            foreach (ModContentPack mod in LoadedModManager.RunningMods.Where(mod => mod.IsCoreMod))
            {
                foreach (LoadableXmlAsset asset in DirectXmlLoader.XmlAssetsInModFolder(mod, "Defs/"))
                {
                    if (asset.xmlDoc?.DocumentElement == null) continue;
                    XmlNodeList childNodes = asset.xmlDoc.DocumentElement.ChildNodes;
                    {
                        for (int i = 0; i < childNodes.Count; i++)
                        {
                            if (childNodes[i].NodeType != XmlNodeType.Element) continue;
                            if (childNodes[i]?.Attributes?["Name"] != null)
                            {
                                vanillaXmlAttributes.Add(childNodes[i].Attributes.GetNamedItem("Name").Value/*, childNodes[i].Name*/);
                            }
                        }
                    }
                }
            }

            if (Prefs.LogVerbose)
                Log.Error("Verbose mode detected. AllYourBase will attempt to fix compatibility errors. If problem persists or gets worse, Verify File Integrity or redownload affected mods.", true);

            //get all bases in mods and compare them to the vanilla list.
            foreach (ModContentPack mod in LoadedModManager.RunningMods.Where(mod => !mod.IsCoreMod))
            {
                foreach (LoadableXmlAsset asset in DirectXmlLoader.XmlAssetsInModFolder(mod, "Defs/"))
                {
                    bool dirty = false;
                    if (asset.xmlDoc?.DocumentElement == null) continue;
                    XmlNodeList childNodes = asset.xmlDoc.DocumentElement.ChildNodes;
                    {
                        for (int i = childNodes.Count -1 ; i >= 0; i--)
                        {
                            if (childNodes[i].NodeType != XmlNodeType.Element) continue;

                            if (childNodes[i]?.Attributes?["Name"] != null &&
                                vanillaXmlAttributes.Contains(childNodes[i].Attributes.GetNamedItem("Name").Value)
                                /*&& vanillaXmlAttributes[childNodes[i].Attributes.GetNamedItem("Name").Value] == childNodes[i].Name*/)
                            {
                                Log.Warning("[" + asset.mod.Name + "]" + " causes compatibility errors by overwriting " +
                                            childNodes[i].Attributes.GetNamedItem("Name").Value + " in file " + asset.FullFilePath, true);

                                if (Prefs.LogVerbose)
                                {
                                    Log.Message($"Attempting fix for {childNodes[i].Attributes.GetNamedItem("Name").Value}", true);
                                    dirty = true;
                                    asset.xmlDoc.DocumentElement.RemoveChild(childNodes[i]);
                                }
                            }
                        }
                    }
                    if (dirty)
                        asset.xmlDoc.Save(asset.FullFilePath);
                }
            }

            foreach (ChemicalDef item in DefDatabase<ChemicalDef>.AllDefsListForReading)
            {
                if (item.addictionHediff?.hediffClass == null)
                    Log.Error($"{item.defName} from mod {item.modContentPack.Name} has no addictionHediff (or missing hediffClass). This will break raids and worldgen. Misconfigured XML or parent.");
            }
        }
    }
}