using UnityEngine;

namespace RCore.Editor
{
	public interface IAssetCatalogPanel
	{
		string Title { get; }
		void OnEnable(AssetCatalogWindow pWindow);
		void OnDisable();
		void OnGUI(Rect pRect);
	}
}
